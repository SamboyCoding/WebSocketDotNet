﻿using System.Net;
using System.Net.WebSockets;
using SocketWrenchSharp;
using WebSocket = System.Net.WebSockets.WebSocket;

namespace WebSocketDotNet.Tests;

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
            Console.WriteLine("Server: Got WebSocket request.");
            return (await context.AcceptWebSocketAsync(null!), httpListener);
        }

        return (null, httpListener);
    }

    internal static async Task<(ReceivedWebsocketPacket, HttpListener)> StartServerAndWaitForFirstWsMessage(int length)
    {
        var (webSocketContext, httpListener) = await SpinUpSocketServerAndWaitForConnect();

        var ws = webSocketContext!.WebSocket;

        var ret = new byte[length];
        var result = await ws.ReceiveAsync(new(ret), CancellationToken.None);

        return (new(result, ret), httpListener);
    }

    internal static Task<byte[]> WaitForBinaryMessage(this SocketWrenchSharp.WebSocket socket)
    {
        var tcs = new TaskCompletionSource<byte[]>();

        void CloseHandler(WebSocketCloseCode code, string? reason)
        {
            Console.WriteLine($"Socket closed: {code} {reason}");
            tcs.SetException(new Exception($"WebSocket closed with code {code}: {reason}"));
        }

        void DataHandler(byte[] data)
        {
            tcs.SetResult(data);
            socket.BinaryReceived -= DataHandler;
            socket.Closed -= CloseHandler;
        }

        socket.BinaryReceived += DataHandler;
        socket.Closing += CloseHandler;
        
        return tcs.Task;
    }
    
    internal static Task<(WebSocketCloseCode, string?)> WaitForClose(this SocketWrenchSharp.WebSocket socket)
    {
        var tcs = new TaskCompletionSource<(WebSocketCloseCode, string?)>();

        void CloseHandler(WebSocketCloseCode code, string? reason)
        {
            Console.WriteLine($"Socket closed: {code} {reason}");
            tcs.SetResult((code, reason));
            socket.Closed -= CloseHandler;
        }
        
        socket.Closed += CloseHandler;
        
        return tcs.Task;
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