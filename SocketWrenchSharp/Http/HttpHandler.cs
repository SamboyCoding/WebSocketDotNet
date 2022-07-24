using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

#if SUPPORTS_ASYNC
using System.Threading.Tasks;
#endif

namespace SocketWrenchSharp.Http;

public class HttpHandler
{
    private Uri _uri;
    private NetworkStreamProvider _underlyingClient;
    private Stream? _stream;

    public Stream GetOrOpenStream()
        => _stream ??= _underlyingClient.GetStream();

    public void CloseAnyExistingStream()
    {
        _stream?.Close();
        _stream = null;
    }

    public HttpHandler(Uri uri)
    {
        _uri = uri;

        _underlyingClient = _uri.Scheme == "wss"
            ? new EncryptedNetworkStreamProvider(uri.DnsSafeHost, uri.Port)
            : new RawTcpNetworkStreamProvider(uri.DnsSafeHost, uri.Port);
    }

    public HttpResponse SendRequestWithHeaders(Dictionary<string, string> headers)
    {
        AddRequiredHeaders(headers);

        var s = GetOrOpenStream();
        s.Write(GetRequestBytes(headers));

        _underlyingClient.WaitForData();

        return HttpResponse.Parse(s.ReadToEnd(_underlyingClient));
    }
    
#if SUPPORTS_ASYNC
    public async Task<Stream> GetOrOpenStreamAsync()
    {
        if (_stream != null)
            return _stream;
        
        _stream = await _underlyingClient.GetStreamAsync();
        return _stream;
    }
    
    public async Task<HttpResponse> SendRequestWithHeadersAsync(Dictionary<string, string> headers)
    {
        AddRequiredHeaders(headers);

        var s = await GetOrOpenStreamAsync();
        await s.WriteAsync(GetRequestBytes(headers));

        await _underlyingClient.WaitForDataAsync();

        return HttpResponse.Parse(await s.ReadToEndAsync(_underlyingClient));
    }
#endif

    private byte[] GetRequestBytes(Dictionary<string, string> headers)
    {
        var builder = new StringBuilder();

        builder.Append(BuildProtocolLine()).Append("\r\n");
        foreach (var header in headers)
            builder.Append(BuildHeaderLine(header)).Append("\r\n");

        builder.Append("\r\n");

        var request = builder.ToString();
        var bytes = Encoding.UTF8.GetBytes(request);
        return bytes;
    }

    private void AddRequiredHeaders(Dictionary<string, string> headers)
    {
        if (!headers.ContainsKey("User-Agent"))
            headers.Add("User-Agent", $"{AssemblyInfo.Name}/{AssemblyInfo.Version}");

        headers["Host"] = _uri.Host;
    }

    private string BuildProtocolLine() => $"GET {_uri.PathAndQuery} HTTP/1.1";

    private string BuildHeaderLine(KeyValuePair<string, string> header)
        => header.Key.Contains(":") ? throw new Exception($"Invalid HTTP Header {header.Key}") : $"{header.Key}: {header.Value}";
}