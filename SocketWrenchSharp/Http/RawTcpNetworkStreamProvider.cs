using System.IO;
using System.Net.Sockets;

#if !NET35
using System.Threading.Tasks;
#endif

namespace SocketWrenchSharp.Http;

public class RawTcpNetworkStreamProvider : NetworkStreamProvider
{
    private TcpClient _client;
    private NetworkStream? _lastStream;

    public RawTcpNetworkStreamProvider(string host, int port) : base(host, port)
    {
        _client = new TcpClient();
    }

    public override Stream GetStream()
    {
        _client.Connect(Host, Port);

        return _lastStream = _client.GetStream();
    }

#if !NET35
    public override async Task<Stream> GetStreamAsync()
    {
        await _client.ConnectAsync(Host, Port);
        
        return _lastStream = _client.GetStream();
    }
#endif

    public override bool AnythingToRead => _lastStream?.DataAvailable ?? false;
}