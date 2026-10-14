using projdump.Engine.Rendering;
using projdump.Engine.Rendering.Sections;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Rendering.Sections;

[TestFixture]
public class ProjectSummarySectionBuilderTests
{
	static readonly DirectoryInfo RootDir = new(FakePaths.Combine("MyApp"));

	static FileInfo File(string name) => new(FakePaths.Combine("MyApp", name));

	readonly ProjectSummarySectionBuilder _builder = new();

	[Test]
	public void Build_MixedExtensions_ListsMostCommonFirst()
	{
		var request = ReportRenderRequestFactory.Create(RootDir, allFiles: [File("a.json"), File("b.cs"), File("c.cs")]);

		string markdown = _builder.Build(request)!.ToMarkdown();

		Assert.That(markdown.IndexOf("| .cs | 2 |", StringComparison.Ordinal), Is.LessThan(markdown.IndexOf("| .json | 1 |", StringComparison.Ordinal)));
	}

	[Test]
	public void Build_FileWithoutExtension_ListsAsNoExt()
	{
		var request = ReportRenderRequestFactory.Create(RootDir, allFiles: [File("Makefile")]);

		Assert.That(_builder.Build(request)!.ToMarkdown(), Does.Contain("| No Ext | 1 |"));
	}

	[Test]
	public void Build_Always_IsNamedProjectSummaryWithNoFiles()
	{
		var section = _builder.Build(ReportRenderRequestFactory.Create(RootDir))!;

		using (Assert.EnterMultipleScope())
		{
			Assert.That(section.Name, Is.EqualTo(ReportSectionNames.ProjectSummary));
			Assert.That(section.Files, Is.Empty);
		}
	}
}