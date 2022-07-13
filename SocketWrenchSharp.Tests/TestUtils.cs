using System.Net;
using System.Net.WebSockets;

namespace SocketWrenchSharp.Tests;

public static class TestUtils
{
    public static void SpinUpSocketServerAndWaitForConnect(Action<WebSocketContext> onConnect)
    {
        // ReSharper disable once AsyncVoidLambda
        var t = new Thread(async () =>
        {
            Console.WriteLine("Starting HTTP listener");
            var httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://127.0.0.1:60606/");
            httpListener.Start();

            Console.WriteLine("Waiting for WS connection");
            var context = await httpListener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                Console.WriteLine("Got WS connection");
                var webSocketContext = await context.AcceptWebSocketAsync(null!);
                onConnect(webSocketContext);
                
                Thread.Sleep(1000);
                
                httpListener.Stop();
            }
        })
        {
            Name = "Test socket server thread",
            // IsBackground = true,
        };

        t.Start();
    }
}