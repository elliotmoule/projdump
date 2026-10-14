using projdump.Engine.Rendering;
using projdump.Engine.Rendering.Preamble;
using projdump.Engine.Rendering.Statistics;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Rendering.Preamble;

[TestFixture]
public class FlagsNoteBuilderTests
{
	static readonly DirectoryInfo RootDir = new(FakePaths.Combine("MyApp"));
	static readonly ReportStatistics EmptyStatistics = new("test", [], []);

	readonly FlagsNoteBuilder _builder = new();

	[Test]
	public void Build_NoFlags_ReturnsNull()
	{
		Assert.That(_builder.Build(ReportRenderRequestFactory.Create(RootDir), EmptyStatistics), Is.Null);
	}

	[Test]
	public void Build_EveryFlag_ListsEachInOrder()
	{
		var request = ReportRenderRequestFactory.Create(
			RootDir, slim: true, excludeTests: true, searchForReadme: true, includeStats: true, scopeDir: "src", excludeDirs: ["wwwroot", "docs"]);

		string markdown = _builder.Build(request, EmptyStatistics)!.ToMarkdown();

		Assert.That(markdown, Does.Contain(
			"> **Flags:** `--slim`, `--exclude-tests`, `--find-readme`, `--include-stats`, `--scope src`, `--exclude-dir wwwroot`, `--exclude-dir docs`"));
	}

	[Test]
	public void Build_AnyFlag_IsNamedFlagsWithNoFiles()
	{
		var section = _builder.Build(ReportRenderRequestFactory.Create(RootDir, slim: true), EmptyStatistics)!;

		using (Assert.EnterMultipleScope())
		{
			Assert.That(section.Name, Is.EqualTo(ReportSectionNames.Flags));
			Assert.That(section.Files, Is.Empty);
		}
	}
}