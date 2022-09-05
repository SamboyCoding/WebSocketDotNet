using System.Net.WebSockets;
using Xunit.Abstractions;
using WebSocket = WebSocketDotNet.WebSocket;

namespace WebSocketDotNet.Tests;

[Collection(nameof(WebSocketTestCollection))]
public class ReceiveTests : MakeConsoleWork
{
    public ReceiveTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async void CanReceiveBinary()
    {
        var binaryData = new byte[16];
        new Random().NextBytes(binaryData);
        
        Output.WriteLine($"Generated 16 bytes: [{string.Join(", ", binaryData)}]. Connecting...");
        
        var clientSocket = new WebSocket("http://127.0.0.1:60606/", new() {AutoConnect = false});
        
        var task = TestUtils.SpinUpSocketServerAndWaitForConnect();
        
        var resultTask = clientSocket.WaitForBinaryMessage();
        
        //Not testing connection here, so use async
        await clientSocket.ConnectAsync();

        var (context, listener) = await task;
        
        Assert.NotNull(context);
        
        Output.WriteLine("Server: Sending binary data...");
        await context!.WebSocket.SendAsync(new(binaryData), WebSocketMessageType.Binary, true, CancellationToken.None);
        
        Output.WriteLine("Client: Waiting for binary data...");
        var received = await resultTask;
        
        Output.WriteLine($"Client: Received binary data: [{string.Join(", ", received)}]");
        
        Assert.Equal(received, binaryData);
        
        listener.Close();
        
        // Thread.Sleep(500);
    }

    [Fact]
    public async void CanReceiveLongBinary()
    {
        var binaryData = new byte[256];
        new Random().NextBytes(binaryData);
        
        Output.WriteLine($"Generated 16 bytes: [{string.Join(", ", binaryData)}]. Connecting...");
        
        var clientSocket = new WebSocket("http://127.0.0.1:60606/", new() {AutoConnect = false});
        
        var task = TestUtils.SpinUpSocketServerAndWaitForConnect();
        
        var resultTask = clientSocket.WaitForBinaryMessage();
        
        //Not testing connection here, so use async
        await clientSocket.ConnectAsync();

        var (context, listener) = await task;
        
        Assert.NotNull(context);
        
        Output.WriteLine("Server: Sending binary data...");
        await context!.WebSocket.SendAsync(new(binaryData), WebSocketMessageType.Binary, true, CancellationToken.None);
        
        Output.WriteLine("Client: Waiting for binary data...");
        var received = await resultTask;
        
        Output.WriteLine($"Client: Received binary data: [{string.Join(", ", received)}]");
        
        Assert.Equal(received, binaryData);
        
        listener.Close();
        
        // Thread.Sleep(500);
    }
}