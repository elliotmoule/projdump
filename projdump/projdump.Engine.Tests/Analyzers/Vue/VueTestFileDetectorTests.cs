using projdump.Engine.Analyzers.Vue;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Analyzers.Vue;

[TestFixture]
public class VueTestFileDetectorTests
{
    readonly VueTestFileDetector _detector = new();

    [TestCase("OrderList.spec.ts")]
    [TestCase("useOrders.test.js")]
    public void IsTestFile_ReturnsTrue_ForSpecAndTestNamingConventions(string fileName)
    {
        Assert.That(_detector.IsTestFile(new FileInfo(fileName)), Is.True);
    }

    [TestCase("__tests__")]
    [TestCase("e2e")]
    public void IsTestFile_ReturnsTrue_ForTestFolders(string folder)
    {
        var path = FakePaths.Combine("src", folder, "thing.ts");
        Assert.That(_detector.IsTestFile(new FileInfo(path)), Is.True);
    }

    [Test]
    public void IsTestFile_ReturnsFalse_ForRegularSourceFile()
    {
        var path = FakePaths.Combine("src", "OrderList.vue");
        Assert.That(_detector.IsTestFile(new FileInfo(path)), Is.False);
    }
}