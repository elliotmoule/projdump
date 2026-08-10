using projdump.Engine.Analyzers.Vue;

namespace projdump.Engine.Tests.Analyzers.Vue;

[TestFixture]
public class VueProjectAnalyzerTests
{
    readonly VueProjectAnalyzer _analyzer = new();

    [Test]
    public void CanHandle_ReturnsFalse_ForNonExistentPath()
    {
        // No fixture needed - Directory.Exists/File.Exists just return false
        // for a path that isn't there, so this stays fast and isolated.
        Assert.That(_analyzer.CanHandle(Path.Combine("definitely", "not", "a", "real", "path")), Is.False);
    }

    [Test]
    public void TypeKey_IsVue()
    {
        Assert.That(_analyzer.TypeKey, Is.EqualTo("vue"));
    }

    [Test]
    public void SupportedModes_IsDefaultOnly()
    {
        Assert.That(_analyzer.SupportedModes, Is.EquivalentTo(["default"]));
    }

    // CanHandle against a real package.json, and Analyze() itself, both need
    // real files on disk - deliberately not covered here.
}