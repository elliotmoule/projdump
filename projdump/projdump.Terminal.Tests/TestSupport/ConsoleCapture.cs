namespace projdump.Terminal.Tests.TestSupport;

sealed class ConsoleCapture : IDisposable
{
    readonly TextWriter _originalOut;
    readonly StringWriter _capturedOut = new();

    public ConsoleCapture()
    {
        _originalOut = Console.Out;
        Console.SetOut(_capturedOut);
    }

    public string Output => _capturedOut.ToString();

    public void Dispose() => Console.SetOut(_originalOut);
}