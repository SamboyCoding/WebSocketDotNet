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
    public async void WebsocketCanConnect()
    {
        //Wrap in task.run to run in bg thread
        var task = TestUtils.SpinUpSocketServerAndWaitForConnect();
        
        var clientSocket = new WebSocket("http://127.0.0.1:60606/", false);
        
        //Specifically do NOT want the async version here
        clientSocket.Connect();

        var (context, listener) = await task;

        Assert.NotNull(context?.WebSocket);

        listener.Stop();
    }
    
    [Fact]
    public async void WebsocketCanConnectAsync()
    {
        var task = TestUtils.SpinUpSocketServerAndWaitForConnect();
        
        var clientSocket = new WebSocket("http://127.0.0.1:60606/", false);
        
        await clientSocket.ConnectAsync();

        var (context, listener) = await task;

        Assert.NotNull(context?.WebSocket);
        
        listener.Stop();
    }

    [Fact]
    public async void WebsocketCanSendBinaryData()
    {
        var binaryData = new byte[16];
        new Random().NextBytes(binaryData);
        
        Output.WriteLine($"Generated 16 bytes: [{string.Join(", ", binaryData)}]. Sending...");

        var task = TestUtils.StartServerAndWaitForFirstWsMessage(16);

        var clientSocket = new WebSocket("http://localhost:60606/", false);
        
        //Again, specifically do NOT want the async version here
        clientSocket.Connect();
        clientSocket.Send(new WebSocketBinaryMessage(binaryData));

        var received = await task;
        
        Output.WriteLine($"Received {received.Buffer.Length} bytes: [{string.Join(", ", received.Buffer)}]");
        
        Assert.Equal(WebSocketMessageType.Binary, received.Result.MessageType);
        Assert.Equal(binaryData, received.Buffer);
    } 
    
    [Fact]
    public async void WebsocketCanSendBinaryDataAsynchronously()
    {
        var binaryData = new byte[16];
        new Random().NextBytes(binaryData);
        
        Output.WriteLine($"Generated 16 bytes: [{string.Join(", ", binaryData)}]. Sending...");

        var task = TestUtils.StartServerAndWaitForFirstWsMessage(16);

        var clientSocket = new WebSocket("http://localhost:60606/", false);
        await clientSocket.ConnectAsync();
        await clientSocket.SendAsync(new WebSocketBinaryMessage(binaryData));

        var received = await task;
        
        Output.WriteLine($"Received {received.Buffer.Length} bytes: [{string.Join(", ", received.Buffer)}]");
        
        Assert.Equal(WebSocketMessageType.Binary, received.Result.MessageType);
        Assert.Equal(binaryData, received.Buffer);
    } 
}