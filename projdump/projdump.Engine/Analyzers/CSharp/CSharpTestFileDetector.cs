using projdump.Engine.Core;

namespace projdump.Engine.Analyzers.CSharp;

sealed class CSharpTestFileDetector : ITestFileDetector
{
    static readonly string[] TestPathSegments =
    [
        $"{Path.DirectorySeparatorChar}Specs{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}UnitTests{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}IntegrationTests{Path.DirectorySeparatorChar}",
    ];

    public bool IsTestFile(FileInfo f) =>
        TestPathSegments.Any(seg => f.FullName.Contains(seg)) ||
        f.Name.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase) ||
        f.Name.EndsWith("Test.cs", StringComparison.OrdinalIgnoreCase) ||
        f.Name.EndsWith("Spec.cs", StringComparison.OrdinalIgnoreCase) ||
        f.Name.EndsWith("Specs.cs", StringComparison.OrdinalIgnoreCase) ||
        (f.Extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase) && (
            f.Name.Contains("Test", StringComparison.OrdinalIgnoreCase) ||
            f.Name.Contains("Spec", StringComparison.OrdinalIgnoreCase)
        ));
}