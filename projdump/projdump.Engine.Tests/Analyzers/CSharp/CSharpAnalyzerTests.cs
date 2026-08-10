using projdump.Engine.Analyzers.CSharp;

namespace projdump.Engine.Tests.Analyzers.CSharp;

[TestFixture]
public class CSharpAnalyzerTests
{
    readonly CSharpAnalyzer _analyzer = new();

    [TestCase("MyApp.sln", true)]
    [TestCase("MyApp.slnx", true)]
    [TestCase("MyApp.csproj", true)]
    [TestCase("MyApp.vbproj", false)]
    [TestCase("package.json", false)]
    [TestCase("MyApp", false)]
    public void CanHandle_MatchesOnlyKnownExtensions(string inputPath, bool expected)
    {
        Assert.That(_analyzer.CanHandle(inputPath), Is.EqualTo(expected));
    }

    [Test]
    public void TypeKey_IsCsharp()
    {
        Assert.That(_analyzer.TypeKey, Is.EqualTo("csharp"));
    }

    [Test]
    public void SupportedModes_IncludesDefaultAndWebApi()
    {
        Assert.That(_analyzer.SupportedModes, Is.EquivalentTo(["default", "webapi"]));
    }

    // Analyze() itself is deliberately not covered here - it walks a real
    // directory tree, which makes it an integration test, not a unit test.
}