using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("SocketWrenchSharp.Tests")]

namespace SocketWrenchSharp;

public class AssemblyInfo
{
    public static string Name = Assembly.GetExecutingAssembly().GetName().Name!;
    public static Version Version = Assembly.GetExecutingAssembly().GetName().Version!;
}