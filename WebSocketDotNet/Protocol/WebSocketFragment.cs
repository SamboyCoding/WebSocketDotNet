using System;
using System.IO;
using SocketWrenchSharp.Utils;

#if SUPPORTS_ASYNC
using System.Threading.Tasks;
#endif

namespace SocketWrenchSharp.Protocol;

public class WebSocketFragment
{
    public const byte MaxSingleFragmentPayloadSize = 125;
    private const byte ShortLengthExtended16Bit = 126;
    private const byte ShortLengthExtended64Bit = 127;

    private static readonly Random MaskGenerator = new();
    
    public bool IsFinal;
    public bool Reserved1;
    public bool Reserved2;
    public bool Reserved3;
    public WebSocketOpcode Opcode; //4 bits
    public bool IsMasked;
    
    private byte _shortPayloadLength; //7 bits
    private ulong _extendedPayloadLength; //Either 16 or 64 bits

    public byte[] Mask; //32-bit masking key
    
    private byte[] _rawPayload;

    public ulong PayloadLength => UsesExtendedPayloadLength ? _extendedPayloadLength : _shortPayloadLength;

    public byte[] Payload => _rawPayload;
    
    private bool UsesExtendedPayloadLength => _extendedPayloadLength != ulong.MaxValue;

    private WebSocketFragment()
    {
        Mask = new byte[4];
        _rawPayload = MiscUtils.EmptyArray<byte>();
    }
    
    public WebSocketFragment(bool final, WebSocketOpcode opcode, byte[] payload, bool mask) : this()
    {
        IsFinal = final;
        Opcode = opcode;
        // _rawPayload = payload;
        _rawPayload = (byte[])payload.Clone(); //Clone to avoid modifying the original array
        
        ComputeOutgoingLength();
        
        if(mask)
            MaskPayload();
    }

    /// <summary>
    /// Applies the mask to the payload in-place.
    /// </summary>
    private void XorPayloadWithMask()
    {
        for (var i = 0; i < _rawPayload.Length; i++)
        {
            var maskingByteIdx = i % 4;
            var maskingByte = Mask[maskingByteIdx];

            _rawPayload[i] = (byte)(_rawPayload[i] ^ maskingByte);
        }
    }

    /// <summary>
    /// Unmask in-place and clear the masked flag, then return payload
    /// </summary>
    private void UnmaskPayload()
    {
        XorPayloadWithMask();

        IsMasked = false;
        Array.Clear(Mask, 0, 4);
    }

    /// <summary>
    /// Generates a new random masking key and applies it to the payload, then sets the masked flag.
    /// </summary>
    private void MaskPayload()
    {
        MaskGenerator.NextBytes(Mask);
        
        XorPayloadWithMask();

        IsMasked = true;
    }

    /// <summary>
    /// Handles reading the payload length for this fragment. If the initial 7-byte length in the frame is less than 126, it is stored in _shortPayloadLength.
    /// Otherwise extra bytes are read from the stream and stored in _extendedPayloadLength.
    /// </summary>
    /// <param name="initialHeader">The initial 2-byte header of this fragment to read the short length from</param>
    /// <param name="stream">The stream to read any extended length data from if needed</param>
    /// <exception cref="IOException">If an error occurs when reading from the stream</exception>
    private void ReadLength(byte[] initialHeader, Stream stream)
    {
        var shortLength = initialHeader[1].Bits(0, 6);
        if (shortLength == ShortLengthExtended16Bit)
        {
            //We don't need to access the existing buffered data so we can re-use it for the 16-bit length
            if (stream.Read(initialHeader, 0, 2) != 2)
                throw new IOException("Failed to read 2-byte extended length from stream");
            
            //Swap bytes - network order is big-endian, BitConverter is little-endian
            //This is more optimised than Array.Reverse for a single swap
            (initialHeader[0], initialHeader[1]) = (initialHeader[1], initialHeader[0]);
            
            _extendedPayloadLength = BitConverter.ToUInt16(initialHeader, 0);
        } else if (shortLength == ShortLengthExtended64Bit)
        {
            //Need an 8-byte buffer
            initialHeader = new byte[8];
            if (stream.Read(initialHeader, 0, 8) != 8)
                throw new IOException("Failed to read 8-byte extended length from stream");
            
            //Swap bytes - network order is big-endian, BitConverter is little-endian
            Array.Reverse(initialHeader);
            
            _extendedPayloadLength = BitConverter.ToUInt64(initialHeader, 0);
            
            if(_extendedPayloadLength >> 63 != 0)
                throw new IOException("64-bit extended payload length has most significant bit set, which is not allowed");
        }
        else
        {
            _shortPayloadLength = shortLength;
            _extendedPayloadLength = ulong.MaxValue;
        }
    }

    private void ComputeOutgoingLength()
    {
        if (_rawPayload.Length < MaxSingleFragmentPayloadSize)
        {
            _shortPayloadLength = (byte)_rawPayload.Length;
            _extendedPayloadLength = ulong.MaxValue;
            return;
        }

        if (_rawPayload.Length < ushort.MaxValue)
            _shortPayloadLength = ShortLengthExtended16Bit;
        else
            _shortPayloadLength = ShortLengthExtended64Bit;
        
        _extendedPayloadLength = (ulong)_rawPayload.Length;
    }

    /// <summary>
    /// Read a WebSocketFragment from a stream.
    /// <br/>
    /// Does *not* do any validation other than that required to actually read the fragment (length etc).
    /// <br/>
    /// Does unmask the payload if it is masked.
    /// </summary>
    /// <param name="from">The stream to read from</param>
    /// <exception cref="IOException">If an error occurs reading from the stream.</exception>
    public static WebSocketFragment Read(Stream from)
    {
        var buf = new byte[2];
        if (from.Read(buf, 0, 2) != 2)
            throw new IOException("Failed to read 2-byte header from stream");
        
        //Read initial data
        var ret = ParseTwoByteHeader(buf);
        
        //Length handling
        ret.ReadLength(buf, from);

        //Mask handling
        if (ret.IsMasked)
        {
            if (from.Read(ret.Mask, 0, 4) != 4)
                throw new IOException("Failed to read 4-byte mask from stream");
        }
        
        //Read length of payload
        if(ret.PayloadLength > int.MaxValue)
            throw new IOException($"Cannot read >2GiB payload (length in header was {ret.PayloadLength} bytes)");
        
        ret._rawPayload = new byte[(int)ret.PayloadLength];
        if (from.Read(ret._rawPayload, 0, (int)ret.PayloadLength) != (int)ret.PayloadLength)
            throw new IOException("Failed to read payload from stream");
        
        if(ret.IsMasked)
            ret.UnmaskPayload();
        
        return ret;
    }

    private static WebSocketFragment ParseTwoByteHeader(byte[] buf) =>
        new()
        {
            IsFinal = buf[0].Bit(7),
            Reserved1 = buf[0].Bit(6),
            Reserved2 = buf[0].Bit(5),
            Reserved3 = buf[0].Bit(4),
            Opcode = (WebSocketOpcode)buf[0].Bits(0, 3),
            IsMasked = buf[1].Bit(7),
        };

    public byte[] Serialize()
    {
        byte[] toWrite;
        if (!UsesExtendedPayloadLength && !IsMasked)
        {
            //Simple write of 2 byte header then payload
            toWrite = new byte[2 + _rawPayload.Length];
            WriteTwoByteHeader(toWrite);

            Array.Copy(_rawPayload, 0, toWrite, 2, _rawPayload.Length);
        } else if (!UsesExtendedPayloadLength && IsMasked)
        {
            //Two byte header, 4 byte mask, payload
            toWrite = new byte[2 + 4 + _rawPayload.Length];
            WriteTwoByteHeader(toWrite);
            Array.Copy(Mask, 0, toWrite, 2, 4);
            Array.Copy(_rawPayload, 0, toWrite, 6, _rawPayload.Length);
        } else
        {
            //Two byte header, extended payload length, optional mask, payload
            var payloadLengthLength = _shortPayloadLength == ShortLengthExtended16Bit ? 2 : 8;
            var maskLength = IsMasked ? 4 : 0;
            
            toWrite = new byte[2 + payloadLengthLength + maskLength + _rawPayload.Length];
            WriteTwoByteHeader(toWrite);

            if (payloadLengthLength == 2)
            {
                //Big-endian 16-bit length
                toWrite[2] = (byte)(_extendedPayloadLength >> 8);
                toWrite[3] = (byte)_extendedPayloadLength;
            } else
            {
                //Big-endian 64-bit length
                var extendedPayloadLengthBytes = BitConverter.GetBytes(_extendedPayloadLength);
                Array.Reverse(extendedPayloadLengthBytes);
                Array.Copy(extendedPayloadLengthBytes, 0, toWrite, 2, 8);
            }

            if (IsMasked)
            {
                Array.Copy(Mask, 0, toWrite, 2 + payloadLengthLength, 4);
            }

            Array.Copy(_rawPayload, 0, toWrite, 2 + payloadLengthLength + maskLength, _rawPayload.Length);
        }

        return toWrite;
    }

    private void WriteTwoByteHeader(byte[] toWrite)
    {
        //Right to left: 4-bit opcode, reserved3, reserved2, reserved1, is final
        toWrite[0] = (byte)((byte)Opcode | (byte)(IsFinal ? 0x80 : 0) | (byte)(Reserved1 ? 0x40 : 0) | (byte)(Reserved2 ? 0x20 : 0) | (byte)(Reserved3 ? 0x10 : 0));
        
        //Right to left: 7 bit payload length, 1 bit mask flag
        toWrite[1] = (byte)(_shortPayloadLength | (byte)(IsMasked ? 0x80 : 0));
    }
    
    #if SUPPORTS_ASYNC
    
    /// <summary>
    /// Handles reading the payload length for this fragment. If the initial 7-byte length in the frame is less than 126, it is stored in _shortPayloadLength.
    /// Otherwise extra bytes are read from the stream and stored in _extendedPayloadLength.
    /// </summary>
    /// <param name="initialHeader">The initial 2-byte header of this fragment to read the short length from</param>
    /// <param name="stream">The stream to read any extended length data from if needed</param>
    /// <exception cref="IOException">If an error occurs when reading from the stream</exception>
    private async Task ReadLengthAsync(byte[] initialHeader, Stream stream)
    {
        var shortLength = initialHeader[1].Bits(0, 6);
        if (shortLength == ShortLengthExtended16Bit)
        {
            //We don't need to access the existing buffered data so we can re-use it for the 16-bit length
            if (await stream.ReadAsync(initialHeader, 0, 2) != 2)
                throw new IOException("Failed to read 2-byte extended length from stream");
            
            //Swap bytes - network order is big-endian, BitConverter is little-endian
            //This is more optimised than Array.Reverse for a single swap
            (initialHeader[0], initialHeader[1]) = (initialHeader[1], initialHeader[0]);
            
            _extendedPayloadLength = BitConverter.ToUInt16(initialHeader, 0);
        } else if (shortLength == ShortLengthExtended64Bit)
        {
            //Need an 8-byte buffer
            initialHeader = new byte[8];
            if (await stream.ReadAsync(initialHeader, 0, 8) != 8)
                throw new IOException("Failed to read 8-byte extended length from stream");
            
            //Swap bytes - network order is big-endian, BitConverter is little-endian
            Array.Reverse(initialHeader);
            
            _extendedPayloadLength = BitConverter.ToUInt64(initialHeader, 0);
            
            if(_extendedPayloadLength >> 63 != 0)
                throw new IOException("64-bit extended payload length has most significant bit set, which is not allowed");
        }
        else
        {
            _shortPayloadLength = shortLength;
            _extendedPayloadLength = ulong.MaxValue;
        }
    }
    
    public static async Task<WebSocketFragment> ReadAsync(Stream from)
    {
        var buf = new byte[2];
        if (await from.ReadAsync(buf, 0, 2) != 2)
            throw new IOException("Failed to read 2-byte header from stream");
        
        //Read initial data
        var ret = ParseTwoByteHeader(buf);
        
        //Length handling
        await ret.ReadLengthAsync(buf, from);

        //Mask handling
        if (ret.IsMasked)
        {
            if (await from.ReadAsync(ret.Mask, 0, 4) != 4)
                throw new IOException("Failed to read 4-byte mask from stream");
        }
        
        //Read length of payload
        if(ret.PayloadLength > int.MaxValue)
            throw new IOException($"Cannot read >2GiB payload (length in header was {ret.PayloadLength} bytes)");
        
        ret._rawPayload = new byte[(int)ret.PayloadLength];
        if (await from.ReadAsync(ret._rawPayload, 0, (int)ret.PayloadLength) != (int)ret.PayloadLength)
            throw new IOException("Failed to read payload from stream");
        
        if(ret.IsMasked)
            ret.UnmaskPayload();
        
        return ret;
    }
    
    #endif
}