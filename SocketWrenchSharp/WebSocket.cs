﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using SocketWrenchSharp.Http;
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

    public WebSocket(string url, bool autoConnect = true)
    {
        UriUtils.ValidateUrlScheme(ref url);

        _httpHandler = new HttpHandler(new Uri(url));

        if (autoConnect)
            Connect();
    }

    public void Connect()
    {
        SendHandshakeRequest();
    }

    private void SendHandshakeRequest()
    {
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

#if SUPPORTS_ASYNC
    public async Task ConnectAsync()
    {
        await SendHandshakeRequestAsync();
    }
    
    private async Task SendHandshakeRequestAsync()
    {
        var headers = BuildHandshakeHeaders();

        var resp = await _httpHandler.SendRequestWithHeadersAsync(headers);

        ValidateResponse(resp, headers["Sec-WebSocket-Key"]);
    }
#endif
}