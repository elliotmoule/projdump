using projdump.Engine.Rendering.Statistics;

namespace projdump.Engine.Tests.Rendering.Statistics;

[TestFixture]
public class ReportStatisticsTests
{
	static ReportStatistics Create(IReadOnlyList<SectionTokenCount>? sections = null, IReadOnlyList<FileTokenCount>? files = null) =>
		new("test", sections ?? [], files ?? []);

	[Test]
	public void ContentTokens_SeveralSections_SumsSectionTokens()
	{
		var statistics = Create(sections: [new("A", 0, 100), new("B", 2, 250)]);

		Assert.That(statistics.ContentTokens, Is.EqualTo(350));
	}

	[Test]
	public void GetLargestFiles_MoreFilesThanTheLimit_ReturnsTopTenLargestFirst()
	{
		var files = Enumerable.Range(1, 15)
			.Select(size => new FileTokenCount($"File{size:D2}.cs", "App Code", size))
			.ToList();

		var largest = Create(files: files).GetLargestFiles();

		using (Assert.EnterMultipleScope())
		{
			Assert.That(largest, Has.Count.EqualTo(ReportStatistics.LargestFileCount));
			Assert.That(largest[0].Tokens, Is.EqualTo(15));
			Assert.That(largest[^1].Tokens, Is.EqualTo(6));
		}
	}

	[Test]
	public void GetLargestFiles_TiedTokenCounts_OrdersByPath()
	{
		var statistics = Create(files: [new("b.cs", "App Code", 10), new("a.cs", "App Code", 10)]);

		var paths = statistics.GetLargestFiles().Select(file => file.RelativePath);

		Assert.That(paths, Is.EqualTo(new[] { "a.cs", "b.cs" }));
	}
}