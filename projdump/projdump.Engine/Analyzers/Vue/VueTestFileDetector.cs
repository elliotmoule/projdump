using projdump.Engine.Core;

namespace projdump.Engine.Analyzers.Vue;

sealed class VueTestFileDetector : ITestFileDetector
{
    static readonly string[] TestPathSegments =
    [
        $"{Path.DirectorySeparatorChar}__tests__{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}e2e{Path.DirectorySeparatorChar}",
    ];

    public bool IsTestFile(FileInfo f) =>
        TestPathSegments.Any(seg => f.FullName.Contains(seg)) ||
        f.Name.Contains(".spec.", StringComparison.OrdinalIgnoreCase) ||
        f.Name.Contains(".test.", StringComparison.OrdinalIgnoreCase);
}