using SocketWrenchSharp.Protocol;
using SocketWrenchSharp.Utils;

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

    internal WebSocketBinaryMessage()
    {
        Data = MiscUtils.EmptyArray<byte>();
    }

    protected override WebSocketOpcode OpcodeToSend => WebSocketOpcode.Binary;
    protected override void ReadData(byte[] payload) => Data = payload;
    protected override byte[] GetPayload() => Data;
}