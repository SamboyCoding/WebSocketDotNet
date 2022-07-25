using System;
using WebSocketDotNet.Utils;

namespace WebSocketDotNet;

/// <summary>
/// If thrown anywhere, indicates that the socket should be closed with <see cref="WebSocketCloseCode.ProtocolError"/>
/// </summary>
[NoCoverage]
public class WebSocketProtocolException : Exception
{
    public WebSocketProtocolException(string message) : base(message)
    {
    }
}