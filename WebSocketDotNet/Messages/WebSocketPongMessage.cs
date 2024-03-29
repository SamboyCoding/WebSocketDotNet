﻿using WebSocketDotNet.Protocol;
using WebSocketDotNet.Utils;

namespace WebSocketDotNet.Messages;

/// <summary>
/// Represents a websocket frame with the opcode 0x0A (PONG).
/// Can be sent without a ping frame requesting it, in which case it serves as a one-way ping to
/// inform the receiving end that the connection is still alive and a response is neither required nor expected.
/// The payload, if present, must be the same as the payload of the corresponding PING frame.
/// </summary>
public class WebSocketPongMessage : WebSocketMessage
{
    public byte[] PongPayload { get; private set; }
    
    public WebSocketPongMessage(byte[] pongPayload)
    {
        PongPayload = pongPayload;
    }

    internal WebSocketPongMessage()
    {
        PongPayload = MiscUtils.EmptyArray<byte>();
    }

    protected override WebSocketOpcode OpcodeToSend => WebSocketOpcode.Pong;
    protected override void ReadData(byte[] payload) => PongPayload = payload;
    protected override byte[] GetPayload() => PongPayload;
}