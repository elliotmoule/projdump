using projdump.Engine.Analyzers.CSharp;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Analyzers.CSharp;

[TestFixture]
public class CSharpExclusionFilterTests
{
    readonly CSharpExclusionFilter _filter = new();

    [Test]
    public void IsExcluded_ReturnsTrue_ForBinFolder()
    {
        var path = FakePaths.Combine("MyApp", "bin", "Debug", "MyApp.dll");
        Assert.That(_filter.IsExcluded(new FileInfo(path)), Is.True);
    }

    [Test]
    public void IsExcluded_ReturnsTrue_ForObjFolder()
    {
        var path = FakePaths.Combine("MyApp", "obj", "Debug", "MyApp.dll");
        Assert.That(_filter.IsExcluded(new FileInfo(path)), Is.True);
    }

    [Test]
    public void IsExcluded_ReturnsTrue_ForVsFolder()
    {
        var path = FakePaths.Combine("MyApp", ".vs", "config");
        Assert.That(_filter.IsExcluded(new FileInfo(path)), Is.True);
    }

    [Test]
    public void IsExcluded_ReturnsTrue_ForMigrationsFolder()
    {
        var path = FakePaths.Combine("MyApp", "Migrations", "20240101_Init.cs");
        Assert.That(_filter.IsExcluded(new FileInfo(path)), Is.True);
    }

    [TestCase("AssemblyInfo.cs")]
    [TestCase("GlobalUsings.cs")]
    [TestCase("GlobalUsings.g.cs")]
    public void IsExcluded_ReturnsTrue_ForBoilerplateFileNames(string fileName)
    {
        Assert.That(_filter.IsExcluded(new FileInfo(fileName)), Is.True);
    }

    [TestCase("MainWindow.designer.cs")]
    [TestCase("Reference.g.cs")]
    [TestCase("Reference.g.i.cs")]
    public void IsExcluded_ReturnsTrue_ForGeneratedFileSuffixes(string fileName)
    {
        Assert.That(_filter.IsExcluded(new FileInfo(fileName)), Is.True);
    }

    [Test]
    public void IsExcluded_ReturnsFalse_ForRegularSourceFile()
    {
        var path = FakePaths.Combine("src", "OrderService.cs");
        Assert.That(_filter.IsExcluded(new FileInfo(path)), Is.False);
    }
}