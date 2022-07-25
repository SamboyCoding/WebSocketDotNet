using System;

namespace WebSocketDotNet.Utils;

public static class MiscUtils
{
    public static T[] EmptyArray<T>()
    {
#if NET35 || NET45
        return new T[0];
#else
        return Array.Empty<T>();
#endif
    }
}