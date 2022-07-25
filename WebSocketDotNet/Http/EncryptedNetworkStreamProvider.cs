using System.IO;
using System.Net.Security;

#if SUPPORTS_ASYNC
using System.Threading.Tasks;
#endif

namespace WebSocketDotNet.Http;

public class EncryptedNetworkStreamProvider : RawTcpNetworkStreamProvider
{
    public EncryptedNetworkStreamProvider(string host, int port) : base(host, port)
    {
    }

    public override Stream GetStream()
    {
        var tcpStream = base.GetStream();

        var sslStream = new SslStream(tcpStream, false);

        sslStream.AuthenticateAsClient(Host);

        return sslStream;
    }

#if SUPPORTS_ASYNC
    public override async Task<Stream> GetStreamAsync()
    {
        var tcpStream = await base.GetStreamAsync();

        var sslStream = new SslStream(tcpStream, false);

        await sslStream.AuthenticateAsClientAsync(Host);

        return sslStream;
    }
#endif
}