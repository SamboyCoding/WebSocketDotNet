using System;
using System.Collections.Generic;
using System.IO;
using SocketWrenchSharp.Http;

#if !NET35
using System.Threading.Tasks;
#endif

namespace SocketWrenchSharp;

public static class Extensions
{
    public static byte[] ReadToEnd(this Stream s, NetworkStreamProvider provider)
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

    public static void Write(this Stream s, byte[] bytes)
        => s.Write(bytes, 0, bytes.Length);

#if !NET35
    public static async Task WriteAsync(this Stream s, byte[] bytes)
        => await s.WriteAsync(bytes, 0, bytes.Length);
#endif
}