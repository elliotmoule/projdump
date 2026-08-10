namespace projdump.Engine.Core.Filters;

// Only truly stack-agnostic rules belong here - stack-specific ones go in that stack's own filter.
sealed class VcsAndToolingExclusionFilter : IFileExclusionFilter
{
    static readonly string[] ExcludedPathSegments =
    [
        $"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}.vscode{Path.DirectorySeparatorChar}",
    ];

    public bool IsExcluded(FileInfo f) => ExcludedPathSegments.Any(seg => f.FullName.Contains(seg));
}