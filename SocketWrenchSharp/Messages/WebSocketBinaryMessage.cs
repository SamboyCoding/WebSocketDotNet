using SocketWrenchSharp.Protocol;

namespace SocketWrenchSharp.Messages;

/// <summary>
/// Represents a websocket message with opcode 0x2 (binary).
/// Contains a byte array of data which it is up to the application to interpret.
/// </summary>
public class WebSocketBinaryMessage : WebSocketMessage 
{
    public byte[] Data { get; private set; }
    
    public WebSocketBinaryMessage(byte[] data)
    {
        Data = data;
    }
    
    public override WebSocketOpcode OpcodeToSend => WebSocketOpcode.Binary;
    public override void ReadData(byte[] payload) => Data = payload;
    public override byte[] GetPayload() => Data;
}