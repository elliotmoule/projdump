using projdump.Engine.Core;

namespace projdump.Engine.Analyzers.CSharp;

sealed class CSharpExclusionFilter : IFileExclusionFilter
{
    static readonly HashSet<string> ExcludedFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "AssemblyInfo.cs",
        "GlobalUsings.cs",
        "GlobalUsings.g.cs",
    };

    static readonly string[] ExcludedPathSegments =
    [
        $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}.vs{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}",
    ];

    static readonly string[] ExcludedFileSuffixes = [".designer.cs", ".g.cs", ".g.i.cs"];

    public bool IsExcluded(FileInfo f) =>
        ExcludedFileNames.Contains(f.Name) ||
        ExcludedPathSegments.Any(seg => f.FullName.Contains(seg)) ||
        ExcludedFileSuffixes.Any(suffix => f.Name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
}