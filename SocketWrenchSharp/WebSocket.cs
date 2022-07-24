using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using SocketWrenchSharp.Http;
using SocketWrenchSharp.Messages;
using SocketWrenchSharp.Utils;

#if SUPPORTS_ASYNC
using System.Threading.Tasks;
#endif

namespace SocketWrenchSharp;

public class WebSocket
{
    private static readonly Guid WebsocketKeyGuid = new("258EAFA5-E914-47DA-95CA-C5AB0DC85B11");

    private readonly Random _random = new();
    private readonly SHA1 _sha1 = SHA1.Create();
    private readonly HttpHandler _httpHandler;

    public WebSocketState State { get; private set; }

    public WebSocket(string url, bool autoConnect = true)
    {
        UriUtils.ValidateUrlScheme(ref url);

        _httpHandler = new(new(url));

        State = WebSocketState.Closed;

        if (autoConnect)
            Connect();
    }

    public void Connect()
    {
        SendHandshakeRequest();

        State = WebSocketState.Open;
    }

    private void SendHandshakeRequest()
    {
        State = WebSocketState.Connecting;
        var headers = BuildHandshakeHeaders();

        var resp = _httpHandler.SendRequestWithHeaders(headers);

        ValidateResponse(resp, headers["Sec-WebSocket-Key"]);
    }

    private Dictionary<string, string> BuildHandshakeHeaders()
    {
        //Key is a random 16-byte string, base64 encoded
        var keyBytes = new byte[16];
        _random.NextBytes(keyBytes);
        var key = Convert.ToBase64String(keyBytes);

        return new()
        {
            { "Upgrade", "websocket" },
            { "Connection", "Upgrade" },
            { "Sec-WebSocket-Key", key },
            { "Sec-WebSocket-Version", "13" }
        };
    }

    private void ValidateResponse(HttpResponse resp, string key)
    {
        //Expected response is the base64 key string with the magic guid in uppercase appended, hashed as sha1, then base64 encoded
        var expectedAccept = Convert.ToBase64String(_sha1.ComputeHash(Encoding.UTF8.GetBytes(key + WebsocketKeyGuid.ToString().ToUpperInvariant())));
        
        if (resp.StatusCode != HttpStatusCode.SwitchingProtocols)
            throw new WebException($"Expecting HTTP 101/SwitchingProtocols, got {(int)resp.StatusCode}/{resp.StatusCode}");

        if (!resp.Headers.TryGetValue("Upgrade", out var upgrade) || upgrade != "websocket")
            throw new WebException($"Expecting Upgrade: websocket, got \"{upgrade}\"");

        if (!resp.Headers.TryGetValue("Sec-WebSocket-Accept", out var accept) || accept != expectedAccept)
            throw new WebException($"Invalid or no Sec-WebSocket-Accept header in response (got \"{accept}\", expected \"{expectedAccept}\")");
    }

    public void Send(WebSocketMessage message)
    {
        if (State != WebSocketState.Open)
            throw new Exception("WebSocket is not open");

        var frame = message.ToFrame();
        var fragments = frame.ToFragments();

        var s = _httpHandler.GetOrOpenStream();
        foreach (var fragment in fragments)
        {
            var bytes = fragment.Serialize();
            s.Write(bytes, 0, bytes.Length);
        }
    }

#if SUPPORTS_ASYNC
    public async Task ConnectAsync()
    {
        await SendHandshakeRequestAsync();
        
        State = WebSocketState.Open;
    }
    
    private async Task SendHandshakeRequestAsync()
    {
        State = WebSocketState.Connecting;
        
        var headers = BuildHandshakeHeaders();

        var resp = await _httpHandler.SendRequestWithHeadersAsync(headers);

        ValidateResponse(resp, headers["Sec-WebSocket-Key"]);
    }
    
    public async Task SendAsync(WebSocketMessage message)
    {
        if (State != WebSocketState.Open)
            throw new Exception("WebSocket is not open");

        var frame = message.ToFrame();
        var fragments = frame.ToFragments();

        var s = await _httpHandler.GetOrOpenStreamAsync();
        foreach (var fragment in fragments)
        {
            var bytes = fragment.Serialize();
            await s.WriteAsync(bytes, 0, bytes.Length);
        }
    }
#endif
}