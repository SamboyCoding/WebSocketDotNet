using System;
using System.Collections.Generic;
using System.IO;
using WebSocketDotNet.Http;
using WebSocketDotNet.Protocol;

#if SUPPORTS_ASYNC
using System.Threading.Tasks;
#endif

namespace WebSocketDotNet.Utils;

internal static class Extensions
{
    internal static byte[] ReadToEnd(this Stream s, NetworkStreamProvider provider)
    {
        //This is gross but i'm just gonna use a list for it because we have no way of knowing how much to read
        var bytes = new List<byte>();

        var buffer = new byte[1024];
        int read;
        while (provider.AnythingToRead && (read = s.Read(buffer, 0, buffer.Length)) > 0)
        {
            var temp = new byte[read];
            Array.Copy(buffer, 0, temp, 0, read);
            bytes.AddRange(temp);
        }

        return bytes.ToArray();
    }

    internal static void Write(this Stream s, byte[] bytes)
        => s.Write(bytes, 0, bytes.Length);

    internal static bool Bit(this byte b, int bit) => (b & (1 << bit)) != 0;

    internal static byte Bits(this byte b, int start, int end)
    {
        var mask = 0xFF >> (8 - (end - start + 1));
        return (byte)((b >> start) & mask);
    }

    public static bool IsControlOpcode(this WebSocketOpcode opcode) 
        => opcode is WebSocketOpcode.Close or WebSocketOpcode.Ping or WebSocketOpcode.Pong;

#if SUPPORTS_ASYNC
    internal static async Task WriteAsync(this Stream s, byte[] bytes)
        => await s.WriteAsync(bytes, 0, bytes.Length);
    
    internal static async Task<byte[]> ReadToEndAsync(this Stream s, NetworkStreamProvider provider)
    {
        var bytes = new List<byte>();

        var buffer = new byte[1024];
        int read;
        while (provider.AnythingToRead && (read = await s.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            var temp = new byte[read];
            Array.Copy(buffer, 0, temp, 0, read);
            bytes.AddRange(temp);
        }

        return bytes.ToArray();
    }
#endif
}