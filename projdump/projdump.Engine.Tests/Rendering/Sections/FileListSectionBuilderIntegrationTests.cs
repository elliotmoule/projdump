using projdump.Engine.Rendering;
using projdump.Engine.Rendering.Sections;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Rendering.Sections;

[TestFixture]
[Category("Integration")]
public class FileListSectionBuilderIntegrationTests
{
	static readonly string NewLine = Environment.NewLine;

	static FileListSectionBuilder CreateBuilder(bool renderWhenEmpty = false) =>
		new(new ReportFileBlockFactory(), ReportSectionNames.AppCode, request => request.CodeFiles, renderWhenEmpty);

	[Test]
	public void Build_NoFilesAndNotRenderWhenEmpty_ReturnsNull()
	{
		using var temp = new TempProjectDirectory();

		var section = CreateBuilder().Build(ReportRenderRequestFactory.Create(temp.RootDirectoryInfo));

		Assert.That(section, Is.Null);
	}

	[Test]
	public void Build_NoFilesAndRenderWhenEmpty_ReturnsHeadingOnly()
	{
		using var temp = new TempProjectDirectory();

		var section = CreateBuilder(renderWhenEmpty: true).Build(ReportRenderRequestFactory.Create(temp.RootDirectoryInfo));

		Assert.That(section!.ToMarkdown(), Is.EqualTo($"## App Code{NewLine}{NewLine}"));
	}

	[Test]
	public void Build_SeveralFiles_KeepsSelectorOrderWithRelativePaths()
	{
		using var temp = new TempProjectDirectory();
		var program = new FileInfo(temp.WriteFile("Program.cs", "// entry"));
		var service = new FileInfo(temp.WriteFile(Path.Combine("Services", "OrderService.cs"), "// service"));
		var request = ReportRenderRequestFactory.Create(temp.RootDirectoryInfo, codeFiles: [program, service]);

		var paths = CreateBuilder().Build(request)!.Files.Select(file => file.RelativePath);

		Assert.That(paths, Is.EqualTo(new[] { "Program.cs", Path.Combine("Services", "OrderService.cs") }));
	}

	[Test]
	public void Build_CsFile_FencesAsCsharp()
	{
		using var temp = new TempProjectDirectory();
		var program = new FileInfo(temp.WriteFile("Program.cs", "// entry"));
		var request = ReportRenderRequestFactory.Create(temp.RootDirectoryInfo, codeFiles: [program]);

		Assert.That(CreateBuilder().Build(request)!.ToMarkdown(), Does.Contain("```csharp"));
	}
}