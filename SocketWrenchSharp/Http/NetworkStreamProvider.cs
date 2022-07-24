using System;
using System.IO;
using System.Threading;

#if SUPPORTS_ASYNC
using System.Threading.Tasks;
#endif

namespace SocketWrenchSharp.Http;

public abstract class NetworkStreamProvider
{
    public string Host { get; }

    public int Port { get; }
    public abstract bool AnythingToRead { get; }

    public NetworkStreamProvider(string host, int port)
    {
        this.Host = host;
        this.Port = port;
    }

    public abstract Stream GetStream();
    
    public void WaitForData(int timeout = 5000)
    {
        var waited = 0;
        while (!AnythingToRead)
        {
            if ((waited += 10) > timeout)
                throw new Exception("Timeout waiting for response to initial handshake");

            Thread.Sleep(10);
        }
    }
    
#if SUPPORTS_ASYNC
    public async Task WaitForDataAsync(int timeout = 5000)
    {
        var waited = 0;
        while (!AnythingToRead)
        {
            if ((waited += 10) > timeout)
                throw new Exception("Timeout waiting for response to initial handshake");

            await Task.Delay(10);
        }
    }

    public abstract Task<Stream> GetStreamAsync();
#endif
}