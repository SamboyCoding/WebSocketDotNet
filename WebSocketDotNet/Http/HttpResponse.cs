using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace WebSocketDotNet.Http;

public class HttpResponse
{
    public HttpStatusCode StatusCode { get; }
    public string StatusDescription { get; }
    public Dictionary<string, string> Headers { get; }

    private HttpResponse(HttpStatusCode statusCode, string statusDescription, Dictionary<string, string> headers)
    {
        StatusCode = statusCode;
        StatusDescription = statusDescription;
        Headers = headers;
    }

    public static HttpResponse Parse(byte[] resultBytes)
    {
        var resultString = Encoding.UTF8.GetString(resultBytes);

        if (!resultString.StartsWith("HTTP/1.1"))
            throw new Exception("Invalid response from server - not a HTTP/1.1 response");

        var lines = resultString.Split(new[] { "\r\n" }, StringSplitOptions.None);
        var statusLine = lines[0];
        statusLine = statusLine[9..]; // Remove "HTTP/1.1 " from the start

        var firstSpace = statusLine.IndexOf(' ');
        var statusCode = int.Parse(statusLine[..firstSpace]);
        var statusDesc = statusLine[(firstSpace + 1)..];

        var headersDict = new Dictionary<string, string>();
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Length == 0)
                break;
            var colon = line.IndexOf(':');
            var key = line[..colon];
            var value = line[(colon + 2)..];
            headersDict.Add(key, value);
        }

        return new HttpResponse((HttpStatusCode)statusCode, statusDesc, headersDict);
    }
}