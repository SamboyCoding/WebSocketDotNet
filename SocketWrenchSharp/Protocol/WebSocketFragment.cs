using System;
using System.IO;

namespace SocketWrenchSharp.Protocol;

public struct WebSocketFragment
{
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

    public ulong PayloadLength => _extendedPayloadLength == ulong.MaxValue ? _shortPayloadLength : _extendedPayloadLength;

    public byte[] Payload => _rawPayload;

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
        var shortLength = initialHeader[1].Bits(1, 7);
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
        var ret = new WebSocketFragment
        {
            IsFinal = buf[0].Bit(0),
            Reserved1 = buf[0].Bit(1),
            Reserved2 = buf[0].Bit(2),
            Reserved3 = buf[0].Bit(3),
            Opcode = (WebSocketOpcode)buf[0].Bits(4, 7),
            IsMasked = buf[1].Bit(0),
        };
        
        //Length handling
        ret.ReadLength(buf, from);

        //Mask handling
        if (ret.IsMasked)
        {
            ret.Mask = new byte[4];
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
}