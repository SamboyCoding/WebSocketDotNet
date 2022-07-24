using System;

namespace SocketWrenchSharp;

/// <summary>
/// If thrown anywhere, indicates that the socket should be closed with <see cref="WebSocketCloseCode.ProtocolError"/>
/// </summary>
public class WebSocketProtocolException : Exception
{
    public WebSocketProtocolException(string message) : base(message)
    {
    }
}