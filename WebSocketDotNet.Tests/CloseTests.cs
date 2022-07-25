using System.Net.WebSockets;
using SocketWrenchSharp;
using Xunit.Abstractions;
using WebSocket = SocketWrenchSharp.WebSocket;

namespace WebSocketDotNet.Tests;

[Collection(nameof(WebSocketTestCollection))]
public class CloseTests : MakeConsoleWork
{
    public CloseTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async void CanHandleCloseFromServer()
    {
        var clientSocket = new WebSocket("http://localhost:60606/", false);
        
        var task = TestUtils.SpinUpSocketServerAndWaitForConnect();

        var closeTask = clientSocket.WaitForClose();

        clientSocket.Closing += (closeCode, r) =>
        {
            Output.WriteLine("Client: Socket closing with code " + closeCode + " and reason " + r);
        };

        Output.WriteLine("Client: Connecting...");
        
        //Not testing connection here, so use async
        await clientSocket.ConnectAsync();

        var (context, listener) = await task;
        
        Output.WriteLine("Connected.");
        
        Assert.NotNull(context);
        
        Output.WriteLine("Server: Sending close request");
        await context!.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing from test", CancellationToken.None);
        
        var (code, reason) = await closeTask;
        
        Output.WriteLine($"Client: Socket closed with code {code} and reason {reason}");
        
        Assert.Equal(WebSocketCloseCode.ClosedOk, code);
        Assert.Equal("Closing from test", reason);
        
        listener.Close();
        
        // Thread.Sleep(500);
    }
}