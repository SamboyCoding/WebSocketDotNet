using System;
using System.Collections.Generic;

namespace WebSocketDotNet.Protocol;

/// <summary>
/// A WebSocketFrame is made up of one or more WebSocketFraments. It contains an opcode and a payload.
/// </summary>
internal class WebSocketFrame
{
    //Generally speaking, let's try not to use the 64-bit length field.
    private const int MaxFragmentSize = 0xFFFF;

    public WebSocketOpcode Opcode { get; set; }
    public byte[] Payload { get; set; }

    public WebSocketFrame(WebSocketOpcode opcode, byte[] payload)
    {
        Opcode = opcode;
        Payload = payload;
    }

    internal List<WebSocketFragment> ToFragments()
    {
        var fragments = new List<WebSocketFragment>();
        if (Payload.Length < MaxFragmentSize)
        {
            fragments.Add(new(true, Opcode, Payload, true));
        }
        else
        {
            //Chunk the payload into fragments.
            var offset = 0;
            var remaining = Payload.Length;
            while (remaining > 0)
            {
                var fragmentSize = Math.Min(remaining, MaxFragmentSize);

#if SUPPORTS_SPAN
                var payloadSlice = Payload.AsSpan(offset, fragmentSize).ToArray();
#else
                var payloadSlice = new byte[fragmentSize];
                Array.Copy(Payload, offset, payloadSlice, 0, fragmentSize);
#endif

                remaining -= fragmentSize;
                offset += fragmentSize;

                var fragment = new WebSocketFragment(remaining == 0, Opcode, payloadSlice, true);
                fragments.Add(fragment);
            }
        }

        return fragments;
    }
    
    internal static WebSocketFrame FromFragments(List<WebSocketFragment> fragments)
    {
        var payload = new List<byte>();
        foreach (var fragment in fragments)
        {
            payload.AddRange(fragment.Payload);
        }

        return new WebSocketFrame(fragments[0].Opcode, payload.ToArray());
    }
    
    internal static WebSocketFrame FromFragment(WebSocketFragment fragment)
    {
        return new WebSocketFrame(fragment.Opcode, fragment.Payload);
    }
}