using projdump.Engine.Core;
using projdump.Engine.Tests.Core.TestDoubles;

namespace projdump.Engine.Tests.Core;

[TestFixture]
public class CompositeTestFileDetectorTests
{
    [Test]
    public void IsTestFile_ReturnsFalse_WhenNoDetectorsMatch()
    {
        var composite = new CompositeTestFileDetector(new FakeTestFileDetector(false), new FakeTestFileDetector(false));

        Assert.That(composite.IsTestFile(new FileInfo("anything.cs")), Is.False);
    }

    [Test]
    public void IsTestFile_ReturnsTrue_WhenAnyDetectorMatches()
    {
        var composite = new CompositeTestFileDetector(new FakeTestFileDetector(false), new FakeTestFileDetector(true));

        Assert.That(composite.IsTestFile(new FileInfo("anything.cs")), Is.True);
    }
}