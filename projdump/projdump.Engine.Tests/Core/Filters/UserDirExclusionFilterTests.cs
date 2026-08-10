using projdump.Engine.Core.Filters;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Core.Filters;

[TestFixture]
public class UserDirExclusionFilterTests
{
    [Test]
    public void IsExcluded_ReturnsTrue_ForNamedDirectory()
    {
        var filter = new UserDirExclusionFilter(["wwwroot"]);
        var path = FakePaths.Combine("MyApp", "wwwroot", "site.js");

        Assert.That(filter.IsExcluded(new FileInfo(path)), Is.True);
    }

    [Test]
    public void IsExcluded_ReturnsFalse_ForUnrelatedDirectory()
    {
        var filter = new UserDirExclusionFilter(["wwwroot"]);
        var path = FakePaths.Combine("MyApp", "Controllers", "OrdersController.cs");

        Assert.That(filter.IsExcluded(new FileInfo(path)), Is.False);
    }

    [Test]
    public void IsExcluded_SupportsMultipleDirectories()
    {
        var filter = new UserDirExclusionFilter(["wwwroot", "docs"]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(filter.IsExcluded(new FileInfo(FakePaths.Combine("MyApp", "docs", "guide.md"))), Is.True);
            Assert.That(filter.IsExcluded(new FileInfo(FakePaths.Combine("MyApp", "wwwroot", "index.html"))), Is.True);
        }
    }

    [Test]
    public void IsExcluded_IsCaseInsensitive()
    {
        var filter = new UserDirExclusionFilter(["WWWROOT"]);
        var path = FakePaths.Combine("MyApp", "wwwroot", "site.js");

        Assert.That(filter.IsExcluded(new FileInfo(path)), Is.True);
    }
}