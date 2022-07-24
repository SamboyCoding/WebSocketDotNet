using SocketWrenchSharp.Protocol;

namespace SocketWrenchSharp.Messages;

public abstract class WebSocketMessage
{
    public abstract WebSocketOpcode OpcodeToSend { get; }

    public abstract void ReadData(byte[] payload);
    
    public abstract byte[] GetPayload();

    public WebSocketFrame ToFrame() => new(OpcodeToSend, GetPayload());
}