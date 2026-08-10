using projdump.Engine.Core.Filters;

namespace projdump.Engine.Tests.Core.Filters;

[TestFixture]
public class MinifiedAssetExclusionFilterTests
{
    readonly MinifiedAssetExclusionFilter _filter = new();

    [TestCase("jquery.min.js")]
    [TestCase("bootstrap.min.css")]
    public void IsExcluded_ReturnsTrue_ForMinifiedAssets(string fileName)
    {
        Assert.That(_filter.IsExcluded(new FileInfo(fileName)), Is.True);
    }

    [TestCase("site.js")]
    [TestCase("site.css")]
    public void IsExcluded_ReturnsFalse_ForNonMinifiedAssets(string fileName)
    {
        Assert.That(_filter.IsExcluded(new FileInfo(fileName)), Is.False);
    }
}