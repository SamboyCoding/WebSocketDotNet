using System.Net.Sockets;
using System.Net.WebSockets;
using Xunit.Abstractions;

namespace WebSocketDotNet.Tests;

public class FailureTests : MakeConsoleWork
{
    public FailureTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async void TryingToConnectToAnUnopenPortFails()
    {
        var ws = new WebSocket("ws://localhost:1", false);

        var closeTask = ws.WaitForClose();

        await Assert.ThrowsAsync<SocketException>(() => ws.ConnectAsync());

        var (code, reason) = await closeTask;
        
        Assert.Equal(WebSocketCloseCode.ProtocolError, code);
        Assert.Equal("Connection refused", reason);
        Assert.Equal(WebSocketState.Closed, ws.State);
    }
}