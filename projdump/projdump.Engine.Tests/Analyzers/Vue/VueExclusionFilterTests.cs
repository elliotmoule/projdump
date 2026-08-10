using projdump.Engine.Analyzers.Vue;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Analyzers.Vue;

[TestFixture]
public class VueExclusionFilterTests
{
    readonly VueExclusionFilter _filter = new();

    [Test]
    public void IsExcluded_ReturnsTrue_ForNodeModulesFolder()
    {
        var path = FakePaths.Combine("MyApp", "node_modules", "vue", "index.js");
        Assert.That(_filter.IsExcluded(new FileInfo(path)), Is.True);
    }

    [Test]
    public void IsExcluded_ReturnsTrue_ForDistFolder()
    {
        var path = FakePaths.Combine("MyApp", "dist", "index.html");
        Assert.That(_filter.IsExcluded(new FileInfo(path)), Is.True);
    }

    [TestCase("package-lock.json")]
    [TestCase("yarn.lock")]
    [TestCase("pnpm-lock.yaml")]
    public void IsExcluded_ReturnsTrue_ForLockfiles(string fileName)
    {
        Assert.That(_filter.IsExcluded(new FileInfo(fileName)), Is.True);
    }

    [Test]
    public void IsExcluded_ReturnsFalse_ForRegularSourceFile()
    {
        var path = FakePaths.Combine("src", "OrderList.vue");
        Assert.That(_filter.IsExcluded(new FileInfo(path)), Is.False);
    }
}