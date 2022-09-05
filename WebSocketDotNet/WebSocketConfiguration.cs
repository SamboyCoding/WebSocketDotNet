namespace WebSocketDotNet;

public struct WebSocketConfiguration
{
    public WebSocketConfiguration()
    {
        AutoConnect = true;
        UseAutomaticReceiveThread = true;
        MessageChunkingMode = MessageChunkingMode.LimitTo16BitExtendedLength;
    }

    /// <summary>
    /// True to automatically connect to the server when the WebSocket instance is created. False to manually require a call to Connect/ConnectAsync.
    /// Defaults to true.
    /// </summary>
    public bool AutoConnect { get; set; }
    
    /// <summary>
    /// True to start a thread dedicated to calling Receive or ReceiveAsync. False if you want to call Receive or ReceiveAsync manually yourself.
    /// Defaults to true.
    /// </summary>
    public bool UseAutomaticReceiveThread { get; set; }

    /// <summary>
    /// The mode to use when chunking messages. See <see cref="MessageChunkingMode"/> for more information.
    /// Defaults to <see cref="WebSocketDotNet.MessageChunkingMode.LimitTo16BitExtendedLength"/>.
    /// </summary>
    public MessageChunkingMode MessageChunkingMode { get; set; }
}