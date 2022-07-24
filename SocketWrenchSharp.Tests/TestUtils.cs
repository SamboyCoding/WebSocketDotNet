﻿using System.Net;
using System.Net.WebSockets;

namespace SocketWrenchSharp.Tests;

public static class TestUtils
{
    public static Thread SpinUpSocketServerAndWaitForConnect(Action<WebSocketContext, HttpListener> onConnect)
    {
        // ReSharper disable once AsyncVoidLambda
        var t = new Thread(() =>
        {
            // Console.WriteLine("Starting HTTP listener");
            var httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://127.0.0.1:60606/");
            httpListener.Start();

            // Console.WriteLine("Waiting for WS connection");
            var context = httpListener.GetContextAsync().Result;
            if (context.Request.IsWebSocketRequest)
            {
                // Console.WriteLine("Got WS connection");
                var webSocketContext = context.AcceptWebSocketAsync(null!).Result;

                onConnect(webSocketContext, httpListener);

                // Console.WriteLine("Thread terminating.");
            }
        })
        {
            Name = "Test socket server thread",
            IsBackground = true,
        };

        t.Start();

        return t;
    }
}