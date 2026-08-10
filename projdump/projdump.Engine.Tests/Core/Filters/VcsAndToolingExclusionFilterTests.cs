using projdump.Engine.Core.Filters;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Core.Filters;

[TestFixture]
public class VcsAndToolingExclusionFilterTests
{
    readonly VcsAndToolingExclusionFilter _filter = new();

    [Test]
    public void IsExcluded_ReturnsTrue_ForGitFolder()
    {
        var path = FakePaths.Combine("repo", ".git", "HEAD");
        Assert.That(_filter.IsExcluded(new FileInfo(path)), Is.True);
    }

    [Test]
    public void IsExcluded_ReturnsTrue_ForVscodeFolder()
    {
        var path = FakePaths.Combine("repo", ".vscode", "settings.json");
        Assert.That(_filter.IsExcluded(new FileInfo(path)), Is.True);
    }

    [Test]
    public void IsExcluded_ReturnsFalse_ForRegularFile()
    {
        var path = FakePaths.Combine("repo", "src", "Program.cs");
        Assert.That(_filter.IsExcluded(new FileInfo(path)), Is.False);
    }

    [Test]
    public void IsExcluded_ReturnsFalse_ForVsFolder()
    {
        // .vs is C#-specific - belongs to CSharpExclusionFilter, not here.
        var path = FakePaths.Combine("repo", ".vs", "config");
        Assert.That(_filter.IsExcluded(new FileInfo(path)), Is.False);
    }
}