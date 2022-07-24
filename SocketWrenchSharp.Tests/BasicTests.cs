using System.Net.WebSockets;
using SocketWrenchSharp.Messages;
using Xunit.Abstractions;

namespace SocketWrenchSharp.Tests;

public class BasicTests : MakeConsoleWork
{
    public BasicTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void WebsocketCanConnect()
    {
        var serverHasWebsocket = false;
        var thread = TestUtils.SpinUpSocketServerAndWaitForConnect((context, listener) =>
        {
            var serverSocket = context.WebSocket;
            serverHasWebsocket = true;
            
            Thread.Sleep(100);
                
            listener.Stop();
        });
        
        var clientSocket = new WebSocket("http://127.0.0.1:60606/", false);
        
        clientSocket.Connect();
        
        thread.Join();
        
        Assert.True(serverHasWebsocket);
    }
    
    [Fact]
    public async void WebsocketCanConnectAsync()
    {
        var serverHasWebsocket = false;
        var thread = TestUtils.SpinUpSocketServerAndWaitForConnect((context, listener) =>
        {
            var serverSocket = context.WebSocket;
            serverHasWebsocket = true;
            
            Thread.Sleep(100);
                
            listener.Stop();
        });
        
        var clientSocket = new WebSocket("http://localhost:60606/", false);
        
        await clientSocket.ConnectAsync();

        thread.Join();
        
        Assert.True(serverHasWebsocket);
    }

    [Fact]
    public void WebsocketCanSendBinaryData()
    {
        var binaryData = new byte[16];
        new Random().NextBytes(binaryData);
        Exception? unhandled = null;
        
        Output.WriteLine($"Generated 16 bytes: [{string.Join(", ", binaryData)}]. Sending...");
        var thread = TestUtils.SpinUpSocketServerAndWaitForConnect((context, listener) =>
        {
            try
            {
                var serverSocket = context.WebSocket;

                var received = new byte[16];
                var segment = new ArraySegment<byte>(received);
                var result = serverSocket.ReceiveAsync(segment, CancellationToken.None).Result;

                Output.WriteLine($"Received {result.Count} bytes, of type {result.MessageType}: [{string.Join(", ", received)}]");

                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
                Assert.Equal(binaryData, received);

                listener.Stop();
            }
            catch (Exception e)
            {
                unhandled = e;
                throw;
            }
        });
        
        var clientSocket = new WebSocket("http://localhost:60606/", false);
        clientSocket.Connect();
        clientSocket.Send(new WebSocketBinaryMessage(binaryData));

        thread.Join();
        
        Assert.Null(unhandled);
    } 
}