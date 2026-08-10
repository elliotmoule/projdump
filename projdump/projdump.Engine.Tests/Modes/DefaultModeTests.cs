using projdump.Engine.Core;
using projdump.Engine.Modes;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Modes;

[TestFixture]
public class DefaultModeTests
{
    [Test]
    public void Apply_ReturnsAnalysisUnchanged()
    {
        var analysis = ProjectAnalysisFactory.Create(
            ProjectAnalysisFactory.Entry("Program.cs", FileRole.EntryPoint),
            ProjectAnalysisFactory.Entry("site.css", FileRole.Style));

        var result = new DefaultMode().Apply(analysis);

        Assert.That(result, Is.SameAs(analysis));
        Assert.That(result.AllFiles, Has.Count.EqualTo(2));
    }

    [Test]
    public void ModeKey_IsDefault()
    {
        Assert.That(new DefaultMode().ModeKey, Is.EqualTo("default"));
    }
}