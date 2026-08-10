using projdump.Engine.Core;
using projdump.Engine.Tests.Core.TestDoubles;

namespace projdump.Engine.Tests.Core;

[TestFixture]
public class CompositeExclusionFilterTests
{
    [Test]
    public void IsExcluded_ReturnsFalse_WhenNoFiltersMatch()
    {
        var composite = new CompositeExclusionFilter(new FakeExclusionFilter(false), new FakeExclusionFilter(false));

        Assert.That(composite.IsExcluded(new FileInfo("anything.cs")), Is.False);
    }

    [Test]
    public void IsExcluded_ReturnsTrue_WhenAnyFilterMatches()
    {
        var composite = new CompositeExclusionFilter(new FakeExclusionFilter(false), new FakeExclusionFilter(true));

        Assert.That(composite.IsExcluded(new FileInfo("anything.cs")), Is.True);
    }

    [Test]
    public void IsExcluded_ReturnsFalse_WhenNoFiltersProvided()
    {
        var composite = new CompositeExclusionFilter();

        Assert.That(composite.IsExcluded(new FileInfo("anything.cs")), Is.False);
    }
}