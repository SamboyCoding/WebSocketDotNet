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
        TestUtils.SpinUpSocketServerAndWaitForConnect(context =>
        {
            var serverSocket = context.WebSocket;
            serverHasWebsocket = true;
        });
        
        var clientSocket = new WebSocket("http://127.0.0.1:60606/", false);
        
        clientSocket.Connect();
        
        Assert.True(serverHasWebsocket);
    }
    
    [Fact]
    public async void WebsocketCanConnectAsync()
    {
        var serverHasWebsocket = false;
        TestUtils.SpinUpSocketServerAndWaitForConnect(context =>
        {
            var serverSocket = context.WebSocket;
            serverHasWebsocket = true;
        });
        
        var clientSocket = new WebSocket("http://localhost:60606/", false);
        
        await clientSocket.ConnectAsync();
        
        Assert.True(serverHasWebsocket);
    }
}