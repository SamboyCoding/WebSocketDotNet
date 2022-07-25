using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using WebSocketDotNet.Http;
using WebSocketDotNet.Messages;
using WebSocketDotNet.Protocol;
using WebSocketDotNet.Utils;

#if SUPPORTS_ASYNC
using System.Threading.Tasks;
#endif

namespace WebSocketDotNet;

public class WebSocket
{
    private static readonly Guid WebsocketKeyGuid = new("258EAFA5-E914-47DA-95CA-C5AB0DC85B11");

    private readonly Random _random = new();
    private readonly SHA1 _sha1 = SHA1.Create();
    private readonly HttpHandler _httpHandler;
    private readonly List<WebSocketFragment> _currentPartialFragments = new();
    private readonly bool _useReceiveThread;

#if SUPPORTS_ASYNC
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly SemaphoreSlim _receiveLock = new(1, 1);
#else
    private readonly object _sendLock = new();
    private readonly object _receiveLock = new();
#endif

    private Thread? _receiveThread;
    private WebSocketCloseMessage? _closeMessage;

    public WebSocketState State { get; private set; }

    public event Action Opened = () => { };
    public event Action<WebSocketCloseCode, string?> Closing = (_, _) => { };
    public event Action<WebSocketCloseCode, string?> Closed = (_, _) => { };
    public event Action<byte[]> PongReceived = _ => { };
    public event Action<byte[]> BinaryReceived = _ => { };
    public event Action<string> TextReceived = _ => { };
    public event Action<WebSocketMessage> MessageReceived = _ => { };

    /// <summary>
    /// Instantiates a new WebSocket object.
    /// </summary>
    /// <param name="url">The URL to connect to. You can use either http(s) or ws(s) as the protocol.</param>
    /// <param name="autoConnect">True (default) to connect immediately, false if you want to manually call Connect/ConnectAsync</param>
    /// <param name="useReceiveThread">True to start a thread to poll for incoming messages, false to not do so (you will have to manually call <see cref="ReceiveAllAvailable"/> periodically)</param>
    public WebSocket(string url, bool autoConnect = true, bool useReceiveThread = true)
    {
        UriUtils.ValidateUrlScheme(ref url);

        _useReceiveThread = useReceiveThread;
        _httpHandler = new(new(url));

        State = WebSocketState.Closed;

        if (autoConnect)
            Connect();
    }

    /// <summary>
    /// Connect to the server synchronously.
    /// </summary>
    public void Connect()
    {
        if (State != WebSocketState.Closed)
            throw new InvalidOperationException("Cannot connect while in state " + State);

        SendHandshakeRequest();

        OnOpen();
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

    /// <summary>
    /// Send a message to the server. This method is thread-safe.
    /// </summary>
    /// <param name="message">The message to send</param>
    /// <exception cref="InvalidOperationException">If the socket is not in the Open state.</exception>
    public void Send(WebSocketMessage message)
    {
        if (State != WebSocketState.Open)
            throw new InvalidOperationException("WebSocket is not open");

        var frame = message.ToFrame();
        var fragments = frame.ToFragments();

        var s = _httpHandler.GetOrOpenStream();

#if SUPPORTS_ASYNC
        _sendLock.Wait();
#else
        Monitor.Enter(_sendLock);
#endif

        foreach (var fragment in fragments)
        {
            var bytes = fragment.Serialize();
            s.Write(bytes);
        }

#if SUPPORTS_ASYNC
        _sendLock.Release();
#else
        Monitor.Exit(_sendLock);
#endif
    }

    /// <summary>
    /// Send a message to the server requesting a close. This method is thread-safe.
    /// </summary>
    /// <param name="code">The close code to send. Use <see cref="WebSocketCloseCode.Unspecified"/> if you don't want to send any code.</param>
    /// <param name="reason">The human-readable reason to send with the close code. Will not be sent if a close code is not sent.</param>
    /// <exception cref="InvalidOperationException">If the socket is currently connecting.</exception>
    public void SendClose(WebSocketCloseCode code = WebSocketCloseCode.ClosedOk, string? reason = null)
    {
        if (State is WebSocketState.Closed)
            return;

        if (State is WebSocketState.Closing)
        {
            if (code == WebSocketCloseCode.InternalError)
                //If we're closing again due to an internal error, dispatch to closing event even if we're already closing
                Closing(code, reason);

            return;
        }

        if (State == WebSocketState.Connecting)
            throw new InvalidOperationException("Cannot send close message while connecting");

        if (code == WebSocketCloseCode.Reserved)
            throw new ArgumentException("Cannot use reserved close codes", nameof(code));
        
        State = WebSocketState.Closing;
        _closeMessage = new(code, reason);
        Closing(code, reason);

        try
        {
            Send(_closeMessage);
        }
        catch (Exception e)
        {
            //Just close immediately if we can't send the close message
            var wrapped = new IOException("Error sending close message", e);
            _closeMessage = new(WebSocketCloseCode.InternalError, wrapped.ToString());
            OnClose();
        }
    }

    /// <summary>
    /// Receive all available messages from the server, invoking the handler for each message in received order. This method is thread-safe.
    /// </summary>
    public void ReceiveAllAvailable()
    {
        if (State is not WebSocketState.Open and not WebSocketState.Closing)
            return;

        List<WebSocketFragment> receivedFragments = new();

#if SUPPORTS_ASYNC
        _receiveLock.Wait();
#else
        Monitor.Enter(_receiveLock);
#endif

        do
        {
            try
            {
                var fragment = ReceiveOneFragment();

                receivedFragments.Add(fragment);
            }
            catch (WebSocketProtocolException e)
            {
                SendClose(WebSocketCloseCode.ProtocolError, e.Message);
            }
            catch (IOException ioe)
            {
                if (State == WebSocketState.Closing)
                {
                    //This io exception probably means the server closed the connection, so we'll just treat it as a normal close
                    OnClose();
                    return;
                }

                SendClose(WebSocketCloseCode.InternalError, ioe.Message);
            }
            catch (Exception e)
            {
                SendClose(WebSocketCloseCode.InternalError, e.Message);
            }
        } while (_httpHandler.AnyDataAvailable);

#if SUPPORTS_ASYNC
        _receiveLock.Release();
#else
        Monitor.Exit(_receiveLock);
#endif

        try
        {
            receivedFragments.ForEach(ProcessFragment);
        }
        catch (WebSocketProtocolException e)
        {
            SendClose(WebSocketCloseCode.ProtocolError, e.Message);
        }
    }

    private void ReceiveLoop()
    {
        while (State is WebSocketState.Open or WebSocketState.Closing)
        {
            try
            {
                ReceiveAllAvailable();
            }
            catch (IOException e)
            {
                if (e.InnerException is SocketException se)
                {
                    if (se.SocketErrorCode == SocketError.ConnectionReset)
                    {
                        OnClose();
                        continue;
                    }
                }

                SendClose(WebSocketCloseCode.InternalError, e.Message);
            }
            catch (Exception e)
            {
                SendClose(WebSocketCloseCode.InternalError, e.Message);
            }

            Thread.Sleep(10);
        }
    }

    private WebSocketFragment ReceiveOneFragment()
    {
        if (State is not WebSocketState.Open and not WebSocketState.Closing)
            throw new InvalidOperationException("WebSocket is not open");

        // if (!_httpHandler.AnyDataAvailable)
        //Nothing to read
        // return null;

        var s = _httpHandler.GetOrOpenStream();
        return WebSocketFragment.Read(s);
    }

    private void ProcessFragment(WebSocketFragment fragment)
    {
        if (fragment.Reserved1 || fragment.Reserved2 || fragment.Reserved3)
            throw new WebSocketProtocolException("Reserved bits set in fragment");

        if (fragment.Opcode == WebSocketOpcode.Continuation)
        {
            //Continuation of previous message

            //Make sure we actually *have* a previous message 
            if (_currentPartialFragments.Count == 0)
                throw new WebSocketProtocolException("Received unexpected continuation fragment with no partial fragments");

            _currentPartialFragments.Add(fragment);

            if (!fragment.IsFinal)
                return;

            //Frame is complete - reassemble
            var frame = WebSocketFrame.FromFragments(_currentPartialFragments);
            _currentPartialFragments.Clear();
            ProcessFrame(frame);

            return;
        }

        if (_currentPartialFragments.Count > 0)
        {
            //We already know it's not a continuation, so if it's not a control frame we need to throw
            if (!fragment.Opcode.IsControlOpcode())
                throw new WebSocketProtocolException("Received non-continuation, non-control fragment with incomplete frame in buffer");
        }

        if (fragment.IsFinal)
        {
            //Either single-fragment frame or interleaved control frame
            ProcessFrame(WebSocketFrame.FromFragment(fragment));
            return;
        }

        //Non-final fragment - check it's not a control frame
        if (fragment.Opcode.IsControlOpcode())
            throw new WebSocketProtocolException($"Received fragmented control frame! (opcode: {fragment.Opcode})");

        //Add to current partial fragments
        _currentPartialFragments.Add(fragment);
    }

    private void ProcessFrame(WebSocketFrame frame)
        => ProcessMessage(WebSocketMessage.FromFrame(frame));

    private void ProcessMessage(WebSocketMessage message)
    {
        switch (message)
        {
            case WebSocketPingMessage ping:
                //Send a pong and match their data
                Send(new WebSocketPongMessage(ping.PingPayload));
                break;
            case WebSocketPongMessage pong:
                //Dispatch to application
                PongReceived(pong.PongPayload);
                break;
            case WebSocketCloseMessage close when State == WebSocketState.Closing:
                //We already sent a close, close connection
                _closeMessage = close; //Update reason with what they sent back
                OnClose();
                break;
            case WebSocketCloseMessage close:
                //They requested close (we didn't send one yet). Echo it back to them (this also sets our state to closing)
                SendClose(close.CloseReason, close.CloseReasonText);
                break;
            case WebSocketBinaryMessage binary:
                BinaryReceived(binary.Data);
                break;
            case WebSocketTextMessage text:
                TextReceived(text.Text);
                break;
        }

        MessageReceived(message);
    }

    private void OnClose()
    {
        if (State != WebSocketState.Closing || _closeMessage == null)
        {
            //Unexpected close
            _closeMessage = new(WebSocketCloseCode.AbnormalClosure, "Unexpected close");
        }

        _httpHandler.CloseAnyExistingStream();
        State = WebSocketState.Closed;

        Closed(_closeMessage.CloseReason, _closeMessage.CloseReasonText);
    }

    private void OnOpen()
    {
        _closeMessage = null;
        _currentPartialFragments.Clear();

        Opened();

        if (_receiveThread != null)
        {
            if (_receiveThread.IsAlive)
                Console.WriteLine("Warning - receive thread still running!");
            _receiveThread = null;
        }

        State = WebSocketState.Open;

        if (_useReceiveThread)
        {
            _receiveThread = new(ReceiveLoop)
            {
                Name = "WebSocket Receive Thread",
                IsBackground = true
            };
            _receiveThread.Start();
        }
    }

#if SUPPORTS_ASYNC
    /// <summary>
    /// Send a message to the server asynchronously. This method is thread-safe.
    /// </summary>
    /// <param name="message">The message to send</param>
    /// <exception cref="InvalidOperationException">If the socket is not in the Open state.</exception>
    public async Task SendAsync(WebSocketMessage message)
    {
        if (State != WebSocketState.Open)
            throw new InvalidOperationException("WebSocket is not open");

        var frame = message.ToFrame();
        var fragments = frame.ToFragments();

        var s = await _httpHandler.GetOrOpenStreamAsync();

        await _sendLock.WaitAsync();

        foreach (var fragment in fragments)
        {
            var bytes = fragment.Serialize();
            await s.WriteAsync(bytes);
        }

        _sendLock.Release();
    }

    /// <summary>
    /// Receive all available messages from the server, invoking the handler for each message in received order. This method is thread-safe.
    /// </summary>
    public async Task ReceiveAllAvailableAsync()
    {
        if (State is not WebSocketState.Open and not WebSocketState.Closing)
            return;

        List<WebSocketFragment> receivedFragments = new();

        await _receiveLock.WaitAsync();

        do
        {
            try
            {
                var fragment = await ReceiveOneFragmentAsync();

                receivedFragments.Add(fragment);
            }
            catch (WebSocketProtocolException e)
            {
                SendClose(WebSocketCloseCode.ProtocolError, e.Message);
            }
            catch (IOException ioe)
            {
                if (State == WebSocketState.Closing)
                {
                    //This io exception probably means the server closed the connection, so we'll just treat it as a normal close
                    OnClose();
                    return;
                }

                SendClose(WebSocketCloseCode.InternalError, ioe.Message);
            }
            catch (Exception e)
            {
                SendClose(WebSocketCloseCode.InternalError, e.Message);
            }
        } while (_httpHandler.AnyDataAvailable);

        _receiveLock.Release();

        try
        {
            receivedFragments.ForEach(ProcessFragment);
        }
        catch (WebSocketProtocolException e)
        {
            SendClose(WebSocketCloseCode.ProtocolError, e.Message);
        }
    }

    /// <summary>
    /// Connect to the server asynchronously.
    /// </summary>
    public async Task ConnectAsync()
    {
        await SendHandshakeRequestAsync();

        OnOpen();
    }

    private async Task SendHandshakeRequestAsync()
    {
        State = WebSocketState.Connecting;

        var headers = BuildHandshakeHeaders();

        var resp = await _httpHandler.SendRequestWithHeadersAsync(headers);

        ValidateResponse(resp, headers["Sec-WebSocket-Key"]);
    }

    private async Task<WebSocketFragment> ReceiveOneFragmentAsync()
    {
        if (State is not WebSocketState.Open and not WebSocketState.Closing)
            throw new InvalidOperationException("WebSocket is not open");

        // if (!_httpHandler.AnyDataAvailable)
            //Nothing to read
            // return null;

        var s = await _httpHandler.GetOrOpenStreamAsync();
        return await WebSocketFragment.ReadAsync(s);
    }
#endif
}