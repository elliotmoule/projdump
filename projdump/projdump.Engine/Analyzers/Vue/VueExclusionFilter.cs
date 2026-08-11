using projdump.Engine.Core;

namespace projdump.Engine.Analyzers.Vue;

sealed class VueExclusionFilter : IFileExclusionFilter
{
    static readonly HashSet<string> ExcludedFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "package-lock.json",
        "yarn.lock",
        "pnpm-lock.yaml",
    };

    static readonly string[] ExcludedPathSegments =
    [
        $"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}dist{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}.vite{Path.DirectorySeparatorChar}",
    ];

    public bool IsExcluded(FileInfo f) =>
        ExcludedFileNames.Contains(f.Name) ||
        ExcludedPathSegments.Any(seg => f.FullName.Contains(seg));
}