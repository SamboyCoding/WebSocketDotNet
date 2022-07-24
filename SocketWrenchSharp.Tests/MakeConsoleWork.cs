using Xunit.Abstractions;

namespace SocketWrenchSharp.Tests;

public class MakeConsoleWork : IDisposable
{
    protected readonly ITestOutputHelper Output;
    private readonly TextWriter _originalOut;
    private readonly TextWriter _textWriter;

    public MakeConsoleWork(ITestOutputHelper output)
    {
        Output = output;
        _originalOut = Console.Out;
        _textWriter = new StringWriter();
        Console.SetOut(_textWriter);
    }

    public void Dispose()
    {
        Output.WriteLine(_textWriter.ToString());
        Console.SetOut(_originalOut);
    }
}