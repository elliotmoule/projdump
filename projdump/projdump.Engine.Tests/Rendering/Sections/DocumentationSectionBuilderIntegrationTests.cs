using projdump.Engine.Rendering;
using projdump.Engine.Rendering.Sections;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Rendering.Sections;

[TestFixture]
[Category("Integration")]
public class DocumentationSectionBuilderIntegrationTests
{
	readonly DocumentationSectionBuilder _builder = new(new ReportFileBlockFactory());

	[Test]
	public void Build_NoReadmeFiles_ReturnsNull()
	{
		using var temp = new TempProjectDirectory();

		Assert.That(_builder.Build(ReportRenderRequestFactory.Create(temp.RootDirectoryInfo)), Is.Null);
	}

	[Test]
	public void Build_InTreeReadme_UsesRelativePathWithoutNote()
	{
		using var temp = new TempProjectDirectory();
		var readme = new FileInfo(temp.WriteFile(Path.Combine("docs", "README.md"), "# MyApp"));
		var request = ReportRenderRequestFactory.Create(temp.RootDirectoryInfo, readmeFiles: [readme]);

		var section = _builder.Build(request)!;

		using (Assert.EnterMultipleScope())
		{
			Assert.That(section.Files.Single().RelativePath, Is.EqualTo(Path.Combine("docs", "README.md")));
			Assert.That(section.ToMarkdown(), Does.Not.Contain("Sourced from outside"));
		}
	}

	[Test]
	public void Build_ExternalReadme_ShowsFullPathAndNote()
	{
		using var temp = new TempProjectDirectory();
		var readme = new FileInfo(temp.WriteFile("README.md", "# repo readme"));
		Directory.CreateDirectory(temp.GetFullPath("src"));
		var request = ReportRenderRequestFactory.Create(new DirectoryInfo(temp.GetFullPath("src")), readmeFiles: [readme]);

		var section = _builder.Build(request)!;

		using (Assert.EnterMultipleScope())
		{
			Assert.That(section.Files.Single().RelativePath, Is.EqualTo(readme.FullName));
			Assert.That(section.ToMarkdown(), Does.Contain("Sourced from outside the project tree"));
		}
	}

	[Test]
	public void Build_Readme_WritesContentUnfenced()
	{
		using var temp = new TempProjectDirectory();
		var readme = new FileInfo(temp.WriteFile("README.md", "# MyApp"));
		var request = ReportRenderRequestFactory.Create(temp.RootDirectoryInfo, readmeFiles: [readme]);

		string markdown = _builder.Build(request)!.ToMarkdown();

		using (Assert.EnterMultipleScope())
		{
			Assert.That(markdown, Does.Contain("# MyApp"));
			Assert.That(markdown, Does.Not.Contain("```"));
		}
	}
}