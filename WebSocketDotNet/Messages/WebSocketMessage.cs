using System;
using WebSocketDotNet.Protocol;

namespace WebSocketDotNet.Messages;

public abstract class WebSocketMessage
{
    protected abstract WebSocketOpcode OpcodeToSend { get; }

    protected abstract void ReadData(byte[] payload);

    protected abstract byte[] GetPayload();

    internal WebSocketFrame ToFrame() => new(OpcodeToSend, GetPayload());

    internal static WebSocketMessage FromFrame(WebSocketFrame frame)
    {
        WebSocketMessage message;
        switch (frame.Opcode)
        {
            case WebSocketOpcode.Continuation:
                throw new("How did we get here? Received continuation frame?");
            case WebSocketOpcode.Text:
                message = new WebSocketTextMessage();
                break;
            case WebSocketOpcode.Binary:
                message = new WebSocketBinaryMessage();
                break;
            case WebSocketOpcode.Close:
                message = new WebSocketCloseMessage();
                break;
            case WebSocketOpcode.Ping:
                message = new WebSocketPingMessage();
                break;
            case WebSocketOpcode.Pong:
                message = new WebSocketPongMessage();
                break;
            case WebSocketOpcode.ReservedData3:
            case WebSocketOpcode.ReservedData4:
            case WebSocketOpcode.ReservedData5:
            case WebSocketOpcode.ReservedData6:
            case WebSocketOpcode.ReservedData7:
            case WebSocketOpcode.ReservedControlB:
            case WebSocketOpcode.ReservedControlC:
            case WebSocketOpcode.ReservedControlD:
            case WebSocketOpcode.ReservedControlE:
                throw new WebSocketProtocolException($"Received frame with reserved opcode {frame.Opcode}");
            default:
                throw new ArgumentOutOfRangeException(nameof(frame.Opcode), "Unknown opcode");
        }

        message.ReadData(frame.Payload);
        return message;
    }
}