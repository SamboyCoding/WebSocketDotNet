using System.Net.WebSockets;
using WebSocketDotNet.Messages;
using Xunit.Abstractions;
using WebSocket = WebSocketDotNet.WebSocket;

namespace WebSocketDotNet.Tests;

[Collection(nameof(WebSocketTestCollection))]
public class BasicTests : MakeConsoleWork
{
    public BasicTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async void WebsocketCanConnect()
    {
        var task = TestUtils.SpinUpSocketServerAndWaitForConnect();
        
        var clientSocket = new WebSocket("http://127.0.0.1:60606/", false);
        
        //Specifically do NOT want the async version here
        clientSocket.Connect();

        var (context, listener) = await task;

        Assert.NotNull(context?.WebSocket);

        listener.Stop();
        
        // Thread.Sleep(500);
    }
    
    [Fact]
    public async void WebsocketCanConnectAsync()
    {
        var task = TestUtils.SpinUpSocketServerAndWaitForConnect();
        
        var clientSocket = new WebSocket("http://127.0.0.1:60606/", false);
        
        await clientSocket.ConnectAsync();

        var (context, listener) = await task;

        Assert.NotNull(context?.WebSocket);
        
        Output.WriteLine("Stopping listener...");
        listener.Stop();
        Output.WriteLine("Listener stopped");
        
        // Thread.Sleep(500);
    }

    [Fact]
    public async void WebsocketCanSendBinaryData()
    {
        var binaryData = new byte[16];
        new Random().NextBytes(binaryData);
        
        Output.WriteLine($"Generated 16 bytes: [{string.Join(", ", binaryData)}]. Sending...");

        var task = TestUtils.StartServerAndWaitForFirstWsMessage(16);

        var clientSocket = new WebSocket("http://127.0.0.1:60606/", false);
        
        Output.WriteLine("Client: Connecting...");
        //Again, specifically do NOT want the async version here
        clientSocket.Connect();
        
        Output.WriteLine("Client: Sending...");
        clientSocket.Send(new WebSocketBinaryMessage(binaryData));

        Output.WriteLine("Server: Waiting for data...");
        var (received, listener) = await task;
        
        Output.WriteLine($"Received {received.Buffer.Length} bytes: [{string.Join(", ", received.Buffer)}]");
        
        Assert.Equal(WebSocketMessageType.Binary, received.Result.MessageType);
        Assert.Equal(binaryData, received.Buffer);
        
        Output.WriteLine("Stopping listener...");
        listener.Stop();
        Output.WriteLine("Listener stopped");
        
        // Thread.Sleep(500);
    } 
    
    [Fact]
    public async void WebsocketCanSendBinaryDataAsynchronously()
    {
        var binaryData = new byte[16];
        new Random().NextBytes(binaryData);
        
        Output.WriteLine($"Generated 16 bytes: [{string.Join(", ", binaryData)}]. Sending...");

        var task = TestUtils.StartServerAndWaitForFirstWsMessage(16);

        var clientSocket = new WebSocket("http://127.0.0.1:60606/", false);
        Output.WriteLine("Client: Connecting...");
        //Again, specifically do NOT want the async version here
        await clientSocket.ConnectAsync();
        
        Output.WriteLine("Client: Sending...");
        await clientSocket.SendAsync(new WebSocketBinaryMessage(binaryData));

        Output.WriteLine("Server: Waiting for data...");
        var (received, listener) = await task;
        
        Output.WriteLine($"Received {received.Buffer.Length} bytes: [{string.Join(", ", received.Buffer)}]");
        
        Assert.Equal(WebSocketMessageType.Binary, received.Result.MessageType);
        Assert.Equal(binaryData, received.Buffer);
        
        Output.WriteLine("Stopping listener...");
        listener.Stop();
        Output.WriteLine("Listener stopped");
        
        // Thread.Sleep(500);
    } 
}