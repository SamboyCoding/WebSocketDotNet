using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("WebSocketDotNet.Tests")]

namespace WebSocketDotNet;

internal static class AssemblyInfo
{
    public static readonly string Name = Assembly.GetExecutingAssembly().GetName().Name!;
    public static readonly Version Version = Assembly.GetExecutingAssembly().GetName().Version!;
}