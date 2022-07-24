using System.Net;
using System.Net.WebSockets;

namespace SocketWrenchSharp.Tests;

internal static class TestUtils
{
    
    internal static async Task<(WebSocketContext?, HttpListener)> SpinUpSocketServerAndWaitForConnect()
    {
        Console.WriteLine("Starting test server...");
        var httpListener = new HttpListener();
        try
        {
            httpListener.Prefixes.Add("http://127.0.0.1:60606/");
            httpListener.Start();
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to start test server: " + e.Message);
            return (null, httpListener);
        }

        Console.WriteLine("Server is running.");

        var context = await httpListener.GetContextAsync();
        if (context.Request.IsWebSocketRequest)
        {
            return (await context.AcceptWebSocketAsync(null), httpListener);
        }

        return (null, httpListener);
    }

    internal static async Task<ReceivedWebsocketPacket> StartServerAndWaitForFirstWsMessage(int length)
    {
        var (webSocketContext, httpListener) = await SpinUpSocketServerAndWaitForConnect();

        var ws = webSocketContext!.WebSocket;

        var ret = new byte[length];
        var result = await ws.ReceiveAsync(new(ret), CancellationToken.None);
        
        httpListener.Stop();

        return new(result, ret);
    }

    internal struct ReceivedWebsocketPacket
    {
        public WebSocketReceiveResult Result;
        public byte[] Buffer;

        public ReceivedWebsocketPacket(WebSocketReceiveResult result, byte[] buffer)
        {
            Result = result;
            Buffer = buffer;
        }
    }
}