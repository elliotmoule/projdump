namespace projdump.Terminal.Tests.TestSupport;

// Use via `using var temp = new TempProjectDirectory();` - Dispose runs even
// if the test throws mid-assertion, so cleanup always happens.
sealed class TempProjectDirectory : IDisposable
{
    public string RootPath { get; }
    public DirectoryInfo RootDirectoryInfo => new(RootPath);

    public TempProjectDirectory()
    {
        RootPath = Path.Combine(Path.GetTempPath(), "projdump-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
    }

    // Writes a file at a path relative to the root, creating subdirectories as needed. Returns the full path.
    public string WriteFile(string relativePath, string content = "")
    {
        string fullPath = Path.Combine(RootPath, relativePath);
        string? dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    public string GetFullPath(string relativePath) => Path.Combine(RootPath, relativePath);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(RootPath))
                Directory.Delete(RootPath, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup - a locked file shouldn't fail the test run.
        }
    }
}