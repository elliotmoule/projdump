using projdump.Engine.Rendering.Sections;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Rendering.Sections;

[TestFixture]
public class ProjectStructureSectionBuilderTests
{
	readonly ProjectStructureSectionBuilder _builder = new();

	[Test]
	public void Build_NestedFile_ListsPathRelativeToRoot()
	{
		var rootDir = new DirectoryInfo(FakePaths.Combine("MyApp"));
		var nestedFile = new FileInfo(FakePaths.Combine("MyApp", "src", "Program.cs"));
		var request = ReportRenderRequestFactory.Create(rootDir, allFiles: [nestedFile]);

		string markdown = _builder.Build(request)!.ToMarkdown();

		using (Assert.EnterMultipleScope())
		{
			Assert.That(markdown, Does.Contain(Path.Combine("src", "Program.cs")));
			Assert.That(markdown, Does.Not.Contain(rootDir.FullName));
			Assert.That(markdown, Does.Contain("```text"));
		}
	}
}