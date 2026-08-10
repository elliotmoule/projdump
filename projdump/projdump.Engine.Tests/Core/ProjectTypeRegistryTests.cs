using projdump.Engine.Core;
using projdump.Engine.Tests.Core.TestDoubles;

namespace projdump.Engine.Tests.Core;

[TestFixture]
public class ProjectTypeRegistryTests
{
    [Test]
    public void Resolve_WithExplicitType_ReturnsMatchingAnalyzer_CaseInsensitive()
    {
        var csharpAnalyzer = new FakeProjectAnalyzer("csharp", _ => false, "default");
        var registry = new ProjectTypeRegistry([csharpAnalyzer]);

        var resolved = registry.Resolve("anything", "CSHARP");

        Assert.That(resolved, Is.SameAs(csharpAnalyzer));
    }

    [Test]
    public void Resolve_WithUnknownExplicitType_Throws()
    {
        var registry = new ProjectTypeRegistry([new FakeProjectAnalyzer("csharp", _ => false, "default")]);

        Assert.Throws<ProjectAnalysisException>(() => registry.Resolve("anything", "python"));
    }

    [Test]
    public void Resolve_WithNoExplicitType_UsesFirstAnalyzerThatCanHandle()
    {
        var csharpAnalyzer = new FakeProjectAnalyzer("csharp", path => path.EndsWith(".csproj"), "default");
        var vueAnalyzer = new FakeProjectAnalyzer("vue", _ => true, "default");
        var registry = new ProjectTypeRegistry([csharpAnalyzer, vueAnalyzer]);

        var resolved = registry.Resolve("MyApp.csproj", null);

        Assert.That(resolved, Is.SameAs(csharpAnalyzer));
    }

    [Test]
    public void Resolve_WithNoAnalyzerMatching_Throws()
    {
        var registry = new ProjectTypeRegistry([new FakeProjectAnalyzer("csharp", _ => false, "default")]);

        Assert.Throws<ProjectAnalysisException>(() => registry.Resolve("anything", null));
    }

    [Test]
    public void ValidateMode_DoesNotThrow_WhenModeIsSupported()
    {
        var analyzer = new FakeProjectAnalyzer("csharp", _ => true, "default", "webapi");
        var registry = new ProjectTypeRegistry([analyzer]);

        Assert.DoesNotThrow(() => registry.ValidateMode(analyzer, "webapi"));
    }

    [Test]
    public void ValidateMode_Throws_WhenModeIsNotSupported()
    {
        var analyzer = new FakeProjectAnalyzer("vue", _ => true, "default");
        var registry = new ProjectTypeRegistry([analyzer]);

        Assert.Throws<ProjectAnalysisException>(() => registry.ValidateMode(analyzer, "webapi"));
    }
}