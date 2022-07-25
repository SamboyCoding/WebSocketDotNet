using System;
using System.Text;
using WebSocketDotNet.Protocol;
using WebSocketDotNet.Utils;

namespace WebSocketDotNet.Messages;

/// <summary>
/// Represents a websocket frame with opcode 0x09 (PING).
/// The payload is optional but if present must be sent back in a PONG frame.
/// As this is a control frame, the length of the payload is limited to 125 bytes.
/// </summary>
public class WebSocketPingMessage : WebSocketMessage
{
    public byte[] PingPayload { get; private set; }

    public WebSocketPingMessage() : this(MiscUtils.EmptyArray<byte>())
    {
    }
    
    public WebSocketPingMessage(string payload) : this(Encoding.UTF8.GetBytes(payload))
    {
        
    }
    
    public WebSocketPingMessage(byte[] payload)
    {
        if(payload.Length > 125)
            throw new ArgumentException("Ping payload must be at most 125 bytes", nameof(payload));
        
        PingPayload = payload;
    }

    protected override WebSocketOpcode OpcodeToSend => WebSocketOpcode.Ping;
    protected override void ReadData(byte[] payload) => PingPayload = payload;
    protected override byte[] GetPayload() => PingPayload;
}