using System.Net.WebSockets;
using WebSocketDotNet.Messages;
using Xunit.Abstractions;

namespace WebSocketDotNet.Tests;

public class LargeBodyTests : MakeConsoleWork
{
    public LargeBodyTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async void PacketSplittingWorksWhenSendingLargePacket()
    {
        var binaryData = new byte[65536];
        new Random().NextBytes(binaryData);
        
        Output.WriteLine($"Generated 65535 bytes. Sending...");

        var task = TestUtils.StartServerAndWaitForFirstWsMessage(65536);

        var clientSocket = new WebSocket("http://127.0.0.1:60606/", new() {AutoConnect = false});
        
        Output.WriteLine("Client: Connecting...");
        await clientSocket.ConnectAsync();
        
        Output.WriteLine("Client: Sending...");
        await clientSocket.SendAsync(new WebSocketBinaryMessage(binaryData));

        Output.WriteLine("Server: Waiting for data...");
        var (received, listener) = await task;
        
        Output.WriteLine($"Received {received.Buffer.Length} bytes. With result {received.Result.Count}/{received.Result.EndOfMessage}/{received.Result.CloseStatus}");
        
        Assert.Equal(WebSocketMessageType.Binary, received.Result.MessageType);
        Assert.Equal(binaryData, received.Buffer);
        
        Output.WriteLine("Stopping listener...");
        listener.Stop();
        Output.WriteLine("Listener stopped");
    }
    
    [Fact]
    public async void FullExtendedLengthFieldWorks()
    {
        var binaryData = new byte[65536];
        new Random().NextBytes(binaryData);
        
        Output.WriteLine($"Generated 65535 bytes. Sending...");

        var task = TestUtils.StartServerAndWaitForFirstWsMessage(65536);

        var clientSocket = new WebSocket("http://127.0.0.1:60606/", new() {AutoConnect = false, MessageChunkingMode = MessageChunkingMode.AlwaysUseExtendedLength});
        
        Output.WriteLine("Client: Connecting...");
        await clientSocket.ConnectAsync();
        
        Output.WriteLine("Client: Sending...");
        await clientSocket.SendAsync(new WebSocketBinaryMessage(binaryData));

        Output.WriteLine("Server: Waiting for data...");
        var (received, listener) = await task;
        
        Output.WriteLine($"Received {received.Buffer.Length} bytes.");
        
        Assert.Equal(WebSocketMessageType.Binary, received.Result.MessageType);
        Assert.Equal(binaryData, received.Buffer);
        
        Output.WriteLine("Stopping listener...");
        listener.Stop();
        Output.WriteLine("Listener stopped");
    }
    
    [Fact]
    public async void SplitPacketsCanBeReassembled()
    {
        var binaryData = new byte[65536];
        new Random().NextBytes(binaryData);
        
        Output.WriteLine($"Generated 65536 bytes. Connecting...");
        
        var clientSocket = new WebSocket("http://127.0.0.1:60606/", new() {AutoConnect = false});
        
        var task = TestUtils.SpinUpSocketServerAndWaitForConnect();
        
        var resultTask = clientSocket.WaitForBinaryMessage();
        
        //Not testing connection here, so use async
        await clientSocket.ConnectAsync();

        var (context, listener) = await task;
        
        Assert.NotNull(context);
        
        Output.WriteLine("Server: Sending binary data in 2 frames...");
        await context!.WebSocket.SendAsync(new(binaryData.AsSpan(0, 65535).ToArray()), WebSocketMessageType.Binary, false, CancellationToken.None);
        await context!.WebSocket.SendAsync(new(binaryData.AsSpan(65535, 1).ToArray()), WebSocketMessageType.Binary, true, CancellationToken.None);
        
        Output.WriteLine("Client: Waiting for binary data...");
        var received = await resultTask;
        
        Output.WriteLine($"Client: Received binary data of length {received.Length}");
        
        Assert.Equal(received, binaryData);
        
        listener.Close();
    }
}