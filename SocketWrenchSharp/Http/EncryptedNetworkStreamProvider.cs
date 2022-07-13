using System.IO;
using System.Net.Security;

#if !NET35
using System.Threading.Tasks;
#endif

namespace SocketWrenchSharp.Http;

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

#if !NET35
    public override async Task<Stream> GetStreamAsync()
    {
        var tcpStream = await base.GetStreamAsync();

        var sslStream = new SslStream(tcpStream, false);

        await sslStream.AuthenticateAsClientAsync(Host);

        return sslStream;
    }
#endif
}