using System.Globalization;
using System.Text;
using SocketWrenchSharp.Utils;
using Xunit.Abstractions;

namespace WebSocketDotNet.Tests;

public class MakeConsoleWork : IDisposable
{
    protected readonly ITestOutputHelper Output;
    private readonly TextWriter _originalOut;
    private readonly TextWriter _textWriter;

    public MakeConsoleWork(ITestOutputHelper output)
    {
        Output = output;
        _originalOut = Console.Out;
        _textWriter = new TestOutputTextWriter(output);
        Console.SetOut(_textWriter);
    }

    public void Dispose()
    {
        // Output.WriteLine(_textWriter.ToString());
        Console.SetOut(_originalOut);
    }

    [NoCoverage]
    private class TestOutputTextWriter : TextWriter
    {
        private ITestOutputHelper Output;
        
        public override Encoding Encoding => Encoding.UTF8;

        public TestOutputTextWriter(ITestOutputHelper output)
        {
            Output = output;
        }


        public override void Write(char[] buffer, int index, int count)
        {
            Output.WriteLine(new(buffer, index, count));
        }
        
        public override void Write(string? value)
        {
            Output.WriteLine(value);
        }
        
        public override void WriteLine(string? value)
        {
            Output.WriteLine(value);
        }
        
        public override void WriteLine()
        {
            Output.WriteLine("");
        }
        
        public override void WriteLine(char[] buffer, int index, int count)
        {
            Output.WriteLine(new(buffer, index, count));
        }
        
        public override void WriteLine(char value)
        {
            Output.WriteLine(value.ToString());
        }
        
        public override void WriteLine(char[]? buffer)
        {
            Output.WriteLine(new(buffer));
        }
        
        public override void WriteLine(bool value)
        {
            Output.WriteLine(value.ToString());
        }
        
        public override void WriteLine(int value)
        {
            Output.WriteLine(value.ToString());
        }
        
        public override void WriteLine(uint value)
        {
            Output.WriteLine(value.ToString());
        }
        
        public override void WriteLine(long value)
        {
            Output.WriteLine(value.ToString());
        }
        
        public override void WriteLine(ulong value)
        {
            Output.WriteLine(value.ToString());
        }
        
        public override void WriteLine(float value)
        {
            Output.WriteLine(value.ToString(CultureInfo.InvariantCulture));
        }
        
        public override void WriteLine(double value)
        {
            Output.WriteLine(value.ToString(CultureInfo.InvariantCulture));
        }
        
        public override void WriteLine(decimal value)
        {
            Output.WriteLine(value.ToString(CultureInfo.InvariantCulture));
        }
        
        public override void WriteLine(object? value)
        {
            Output.WriteLine(value?.ToString());
        }
        
        public override void WriteLine(string format, object? arg0)
        {
            Output.WriteLine(format, arg0);
        }
        
        public override void WriteLine(string format, object? arg0, object? arg1)
        {
            Output.WriteLine(format, arg0, arg1);
        }
        
        public override void WriteLine(string format, object? arg0, object? arg1, object? arg2)
        {
            Output.WriteLine(format, arg0, arg1, arg2);
        }
        
        public override void WriteLine(string format, params object?[] arg)
        {
            Output.WriteLine(format, arg);
        }
    }
}