namespace projdump.Terminal.Tests.TestSupport;

sealed class DefaultOutputDirectoryOverrideScope : IDisposable
{
    public DefaultOutputDirectoryOverrideScope(string directory)
    {
        Program.DefaultOutputDirectoryOverride = directory;
    }

    public void Dispose() => Program.DefaultOutputDirectoryOverride = null;
}