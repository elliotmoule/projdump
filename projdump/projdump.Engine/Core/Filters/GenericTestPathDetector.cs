namespace projdump.Engine.Core.Filters;

// Only conventions common enough across ecosystems to be non-stack-specific belong here.
sealed class GenericTestPathDetector : ITestFileDetector
{
    static readonly string[] TestPathSegments =
    [
        $"{Path.DirectorySeparatorChar}Tests{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}Test{Path.DirectorySeparatorChar}",
    ];

    public bool IsTestFile(FileInfo f) => TestPathSegments.Any(seg => f.FullName.Contains(seg));
}