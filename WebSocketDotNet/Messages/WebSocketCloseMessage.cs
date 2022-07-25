using System.Text;
using WebSocketDotNet.Protocol;
using WebSocketDotNet.Utils;

namespace WebSocketDotNet.Messages;

/// <summary>
/// Represents a websocket message with opcode 0x8 (close).
/// Closing a connection is a three step process:
/// 1. One of the endpoints sends a close message, optionally with the code and reason for the close.
/// 2. The receiving endpoint receives the close message and responds with a close message of its own.
/// 3. The initial endpoint receives the close message and closes the connection.
/// </summary>
public class WebSocketCloseMessage : WebSocketMessage
{
    public WebSocketCloseCode CloseReason { get; private set; }
    public string? CloseReasonText { get; private set; }

    public WebSocketCloseMessage(WebSocketCloseCode closeReason, string? closeReasonText = null)
    {
        CloseReason = closeReason;
        CloseReasonText = closeReasonText;
    }

    internal WebSocketCloseMessage()
    {
        CloseReason = WebSocketCloseCode.NoStatus;
    }

    protected override WebSocketOpcode OpcodeToSend => WebSocketOpcode.Close;

    protected override void ReadData(byte[] payload)
    {
        if (payload.Length == 0)
            return;

        if (payload.Length < 2)
            throw new WebSocketProtocolException($"Close message payload is too short. Expected at least 2 bytes, got {payload.Length}");

        CloseReason = (WebSocketCloseCode)(payload[0] << 8 | payload[1]);

        if (payload.Length > 2)
            CloseReasonText = Encoding.UTF8.GetString(payload, 2, payload.Length - 2);
    }

    protected override byte[] GetPayload()
    {
        if (CloseReasonText == null)
            return CloseReason == WebSocketCloseCode.Unspecified
                ? MiscUtils.EmptyArray<byte>()
                : new[] { (byte)((int)CloseReason >> 8), (byte)((int)CloseReason & 0xFF) };

        var length = Encoding.UTF8.GetByteCount(CloseReasonText);
        var payload = new byte[length + 2];

        payload[0] = (byte)((int)CloseReason >> 8);
        payload[1] = (byte)((int)CloseReason & 0xFF);

        Encoding.UTF8.GetBytes(CloseReasonText, 0, CloseReasonText.Length, payload, 2);

        return payload;
    }
}