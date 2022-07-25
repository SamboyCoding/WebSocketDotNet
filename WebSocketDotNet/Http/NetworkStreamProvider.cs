using System;
using System.IO;
using System.Threading;

#if SUPPORTS_ASYNC
using System.Threading.Tasks;
#endif

namespace WebSocketDotNet.Http;

public abstract class NetworkStreamProvider
{
    private const int WaitIntervalMs = 10;

    protected string Host { get; }
    protected int Port { get; }
    
    public abstract bool AnythingToRead { get; }

    protected NetworkStreamProvider(string host, int port)
    {
        Host = host;
        Port = port;
    }

    public abstract Stream GetStream();
    
    public void WaitForData(int timeout = 5000)
    {
        var waited = 0;
        while (!AnythingToRead)
        {
            if ((waited += WaitIntervalMs) > timeout)
                throw new Exception("Timeout waiting for response to initial handshake");

            Thread.Sleep(WaitIntervalMs);
        }
    }
    
#if SUPPORTS_ASYNC
    public async Task WaitForDataAsync(int timeout = 5000)
    {
        var waited = 0;
        while (!AnythingToRead)
        {
            if ((waited += WaitIntervalMs) > timeout)
                throw new Exception("Timeout waiting for response to initial handshake");

            await Task.Delay(WaitIntervalMs);
        }
    }

    public abstract Task<Stream> GetStreamAsync();
#endif
}