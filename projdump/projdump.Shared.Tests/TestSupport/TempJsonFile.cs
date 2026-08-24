namespace projdump.Shared.Tests.TestSupport;

// A file path under a unique temp directory that doesn't exist yet - lets
// tests exercise CommandHistoryStore.Save's "create the directory" behaviour.
sealed class TempJsonFile : IDisposable
{
    public string FilePath { get; }

    public TempJsonFile()
    {
        string dir = Path.Combine(Path.GetTempPath(), "projdump-shared-tests-" + Guid.NewGuid().ToString("N"));
        FilePath = Path.Combine(dir, "command-history.json");
    }

    public void Dispose()
    {
        try
        {
            string? dir = Path.GetDirectoryName(FilePath);
            if (dir != null && Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup - a locked file shouldn't fail the test run.
        }
    }
}