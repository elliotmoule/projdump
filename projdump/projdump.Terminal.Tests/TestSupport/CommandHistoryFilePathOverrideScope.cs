namespace projdump.Terminal.Tests.TestSupport;

sealed class CommandHistoryFilePathOverrideScope : IDisposable
{
    public CommandHistoryFilePathOverrideScope(string filePath)
    {
        Program.CommandHistoryFilePathOverride = filePath;
    }

    public void Dispose() => Program.CommandHistoryFilePathOverride = null;
}