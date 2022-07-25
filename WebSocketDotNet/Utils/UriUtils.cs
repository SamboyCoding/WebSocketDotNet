using System;
using System.Net;

namespace SocketWrenchSharp.Utils;

internal static class UriUtils
{
    /// <summary>
    /// Validates that the given url starts with either http, ws, or their secure variants.
    /// If http(s), it will be changed to ws(s).
    /// If none of the above, throws.
    /// </summary>
    /// <param name="url">The url to validate</param>
    /// <exception cref="WebException">If the protocol of the URL is not http, https, ws, or wss</exception>
    public static void ValidateUrlScheme(ref string url)
    {
        var uri = new Uri(url);

        if (uri.Scheme == "http")
            url = $"ws://{uri.Host}:{uri.Port}{uri.PathAndQuery}";
        else if (uri.Scheme == "https")
            url = $"wss://{uri.Host}:{uri.Port}{uri.PathAndQuery}";
        else if (uri.Scheme is not "ws" and not "wss")
            throw new WebException("Invalid url protocol. Must be one of http, https, ws or wss");
    }
}