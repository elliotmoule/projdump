using projdump.Engine.Rendering;
using projdump.Engine.Rendering.Preamble;
using projdump.Engine.Rendering.Statistics;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Rendering.Preamble;

[TestFixture]
public class StatisticsSectionBuilderTests
{
	static readonly DirectoryInfo RootDir = new(FakePaths.Combine("MyApp"));

	static readonly ReportStatistics Statistics = new(
		"character count ÷ 4",
		[new("Project Summary", 0, 40), new("App Code", 1, 1200)],
		[new("Program.cs", "App Code", 1200)]);

	readonly StatisticsSectionBuilder _builder = new();

	string BuildMarkdown() =>
		_builder.Build(ReportRenderRequestFactory.Create(RootDir, includeStats: true), Statistics)!.ToMarkdown();

	[Test]
	public void Build_IncludeStatsOff_ReturnsNull()
	{
		Assert.That(_builder.Build(ReportRenderRequestFactory.Create(RootDir), Statistics), Is.Null);
	}

	[Test]
	public void Build_IncludeStatsOn_IsNamedTokenStatistics()
	{
		var section = _builder.Build(ReportRenderRequestFactory.Create(RootDir, includeStats: true), Statistics)!;

		Assert.That(section.Name, Is.EqualTo(ReportSectionNames.TokenStatistics));
	}

	[Test]
	public void Build_SectionWithFiles_ShowsFileCountAndTokens()
	{
		Assert.That(BuildMarkdown(), Does.Contain($"| App Code | 1 | {1200:N0} |"));
	}

	[Test]
	public void Build_SectionWithoutFiles_ShowsDashForFileCount()
	{
		Assert.That(BuildMarkdown(), Does.Contain("| Project Summary | - | 40 |"));
	}

	[Test]
	public void Build_IncludeStatsOn_ShowsContentTotalAndLargestFiles()
	{
		string markdown = BuildMarkdown();

		using (Assert.EnterMultipleScope())
		{
			Assert.That(markdown, Does.Contain($"| **Content total** | **1** | **{1240:N0}** |"));
			Assert.That(markdown, Does.Contain($"| `Program.cs` | App Code | {1200:N0} |"));
		}
	}

	[Test]
	public void Build_IncludeStatsOn_NamesTheEstimationMethod()
	{
		Assert.That(BuildMarkdown(), Does.Contain("Estimated by character count ÷ 4."));
	}
}