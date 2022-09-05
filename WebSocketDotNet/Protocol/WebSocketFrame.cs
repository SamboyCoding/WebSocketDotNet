using System;
using System.Collections.Generic;

namespace WebSocketDotNet.Protocol;

/// <summary>
/// A WebSocketFrame is made up of one or more WebSocketFraments. It contains an opcode and a payload.
/// </summary>
internal class WebSocketFrame
{

    public WebSocketOpcode Opcode { get; set; }
    public byte[] Payload { get; set; }

    public WebSocketFrame(WebSocketOpcode opcode, byte[] payload)
    {
        Opcode = opcode;
        Payload = payload;
    }

    internal List<WebSocketFragment> ToFragments(MessageChunkingMode configurationMessageChunkingMode)
    {
        var fragments = new List<WebSocketFragment>();
        
        var maxFragmentSize = configurationMessageChunkingMode switch
        {
            MessageChunkingMode.AlwaysUseExtendedLength => int.MaxValue, //Max length of an array. While, per spec, this could be 2^63 - 1, the max length of an array is 2^31 - 1
            MessageChunkingMode.NeverUseExtendedLength => 0x7F,
            MessageChunkingMode.LimitTo16BitExtendedLength => ushort.MaxValue,
            _ => throw new ArgumentOutOfRangeException(nameof(configurationMessageChunkingMode), configurationMessageChunkingMode, null)
        };
        
        if (Payload.Length < maxFragmentSize)
        {
            fragments.Add(new(true, Opcode, Payload, true));
        }
        else
        {
            //Chunk the payload into fragments.
            var offset = 0;
            var remaining = Payload.Length;
            var opcodeToSend = Opcode;
            while (remaining > 0)
            {
                var fragmentSize = Math.Min(remaining, maxFragmentSize);

#if SUPPORTS_SPAN
                var payloadSlice = Payload.AsSpan(offset, fragmentSize).ToArray();
#else
                var payloadSlice = new byte[fragmentSize];
                Array.Copy(Payload, offset, payloadSlice, 0, fragmentSize);
#endif

                remaining -= fragmentSize;
                offset += fragmentSize;

                var fragment = new WebSocketFragment(remaining == 0, opcodeToSend, payloadSlice, true);
                fragments.Add(fragment);

                opcodeToSend = WebSocketOpcode.Continuation; //Only the first fragment should have the opcode.
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