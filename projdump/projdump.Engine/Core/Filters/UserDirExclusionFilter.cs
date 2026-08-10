namespace projdump.Engine.Core.Filters;

// Built from --exclude-dir at Analyze-time, not a static per-analyzer field
// like the other filters, since the directory names come from the user's
// options rather than being fixed for the stack.
sealed class UserDirExclusionFilter : IFileExclusionFilter
{
    readonly string[] _segments;

    public UserDirExclusionFilter(IReadOnlyList<string> dirNames)
    {
        _segments = [.. dirNames.Select(name => $"{Path.DirectorySeparatorChar}{name.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)}{Path.DirectorySeparatorChar}")];
    }

    public bool IsExcluded(FileInfo f) => _segments.Any(seg => f.FullName.Contains(seg, StringComparison.OrdinalIgnoreCase));
}