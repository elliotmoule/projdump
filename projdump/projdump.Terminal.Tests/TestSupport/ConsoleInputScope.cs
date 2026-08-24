namespace projdump.Terminal.Tests.TestSupport;

// Use via `using var input = new ConsoleInputScope("answer1", "answer2", ...);` -
// each line answers one Prompt()/PromptYesNo() call, in order. Restores the
// original Console.In on Dispose.
sealed class ConsoleInputScope : IDisposable
{
    readonly TextReader _originalIn;

    public ConsoleInputScope(params string[] lines)
    {
        _originalIn = Console.In;
        Console.SetIn(new StringReader(string.Join(Environment.NewLine, lines) + Environment.NewLine));
    }

    public void Dispose() => Console.SetIn(_originalIn);
}