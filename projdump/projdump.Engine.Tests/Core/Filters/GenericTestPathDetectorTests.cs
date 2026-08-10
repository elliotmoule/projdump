using projdump.Engine.Core.Filters;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Core.Filters;

[TestFixture]
public class GenericTestPathDetectorTests
{
    readonly GenericTestPathDetector _detector = new();

    [Test]
    public void IsTestFile_ReturnsTrue_ForTestsFolder()
    {
        var path = FakePaths.Combine("src", "Tests", "Thing.cs");
        Assert.That(_detector.IsTestFile(new FileInfo(path)), Is.True);
    }

    [Test]
    public void IsTestFile_ReturnsTrue_ForTestFolder()
    {
        var path = FakePaths.Combine("src", "Test", "Thing.cs");
        Assert.That(_detector.IsTestFile(new FileInfo(path)), Is.True);
    }

    [Test]
    public void IsTestFile_ReturnsFalse_ForRegularFile()
    {
        var path = FakePaths.Combine("src", "Services", "Thing.cs");
        Assert.That(_detector.IsTestFile(new FileInfo(path)), Is.False);
    }
}