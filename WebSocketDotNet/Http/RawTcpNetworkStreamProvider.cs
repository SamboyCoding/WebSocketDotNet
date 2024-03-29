﻿using System.IO;
using System.Net.Sockets;

#if SUPPORTS_ASYNC
using System.Threading.Tasks;
#endif

namespace WebSocketDotNet.Http;

internal class RawTcpNetworkStreamProvider : NetworkStreamProvider
{
    private TcpClient? _client;
    private NetworkStream? _lastStream;
    
    public override bool AnythingToRead => _lastStream?.DataAvailable ?? false;
    public virtual bool IsClosed => false;

    public RawTcpNetworkStreamProvider(string host, int port) : base(host, port)
    {
    }

    private void ResetClient()
    {
        _client?.Close();
        
        _client = new();
        _lastStream = null;
    }

    public override Stream GetStream()
    {
        ResetClient();

        _client!.Connect(Host, Port);

        return _lastStream = _client.GetStream();
    }

#if SUPPORTS_ASYNC
    public override async Task<Stream> GetStreamAsync()
    {
        ResetClient();
        
        await _client!.ConnectAsync(Host, Port);
        
        return _lastStream = _client.GetStream();
    }
#endif
}