using projdump.Engine.Analyzers.CSharp;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Analyzers.CSharp;

[TestFixture]
public class CSharpTestFileDetectorTests
{
    readonly CSharpTestFileDetector _detector = new();

    [TestCase("OrderServiceTests.cs")]
    [TestCase("OrderServiceTest.cs")]
    [TestCase("OrderServiceSpec.cs")]
    [TestCase("OrderServiceSpecs.cs")]
    public void IsTestFile_ReturnsTrue_ForTestNamingConventions(string fileName)
    {
        Assert.That(_detector.IsTestFile(new FileInfo(fileName)), Is.True);
    }

    [TestCase("UnitTests")]
    [TestCase("IntegrationTests")]
    [TestCase("Specs")]
    public void IsTestFile_ReturnsTrue_ForTestFolders(string folder)
    {
        var path = FakePaths.Combine("src", folder, "Thing.cs");
        Assert.That(_detector.IsTestFile(new FileInfo(path)), Is.True);
    }

    [Test]
    public void IsTestFile_ReturnsTrue_ForTestCsproj()
    {
        Assert.That(_detector.IsTestFile(new FileInfo("MyApp.Tests.csproj")), Is.True);
    }

    [Test]
    public void IsTestFile_ReturnsFalse_ForRegularCsproj()
    {
        Assert.That(_detector.IsTestFile(new FileInfo("MyApp.csproj")), Is.False);
    }

    [Test]
    public void IsTestFile_ReturnsFalse_ForRegularSourceFile()
    {
        Assert.That(_detector.IsTestFile(new FileInfo("OrderService.cs")), Is.False);
    }
}