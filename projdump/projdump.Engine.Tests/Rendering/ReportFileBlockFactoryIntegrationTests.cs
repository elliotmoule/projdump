using projdump.Engine.Rendering;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Rendering;

[TestFixture]
[Category("Integration")]
public class ReportFileBlockFactoryIntegrationTests
{
	static readonly string NewLine = Environment.NewLine;
	readonly ReportFileBlockFactory _factory = new();

	[Test]
	public void Create_FencedFile_WritesHeadingPathAndFencedContent()
	{
		using var temp = new TempProjectDirectory();
		var file = new FileInfo(temp.WriteFile("Program.cs", "  var x = 1;  "));

		var block = _factory.Create(file, "Program.cs", "csharp", slim: false);

		Assert.That(block.Markdown, Is.EqualTo(
			$"### Program.cs{NewLine}**Path:** `Program.cs`{NewLine}{NewLine}```csharp{NewLine}var x = 1;{NewLine}```{NewLine}{NewLine}"));
	}

	[Test]
	public void Create_NullFenceLanguage_WritesContentUnfenced()
	{
		using var temp = new TempProjectDirectory();
		var file = new FileInfo(temp.WriteFile("README.md", "# Title"));

		var block = _factory.Create(file, "README.md", fenceLanguage: null, slim: false);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(block.Markdown, Does.Contain($"{NewLine}# Title{NewLine}"));
			Assert.That(block.Markdown, Does.Not.Contain("```"));
		}
	}

	[Test]
	public void Create_Slim_WritesSizeInsteadOfContent()
	{
		using var temp = new TempProjectDirectory();
		var file = new FileInfo(temp.WriteFile("Program.cs", "secret content"));

		var block = _factory.Create(file, "Program.cs", "csharp", slim: true);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(block.Markdown, Does.Contain("_File size: 14 B_"));
			Assert.That(block.Markdown, Does.Not.Contain("secret content"));
		}
	}

	[Test]
	public void Create_WithNote_PlacesNoteBeforeContent()
	{
		using var temp = new TempProjectDirectory();
		var file = new FileInfo(temp.WriteFile("README.md", "readme body"));

		var block = _factory.Create(file, "README.md", fenceLanguage: null, slim: false, note: "> note");

		Assert.That(block.Markdown.IndexOf("> note", StringComparison.Ordinal), Is.LessThan(block.Markdown.IndexOf("readme body", StringComparison.Ordinal)));
	}

	[Test]
	public void Create_DisplayPathDiffersFromFileName_RecordsDisplayPath()
	{
		using var temp = new TempProjectDirectory();
		var file = new FileInfo(temp.WriteFile(Path.Combine("src", "Program.cs"), "// code"));
		string displayPath = Path.Combine("src", "Program.cs");

		var block = _factory.Create(file, displayPath, "csharp", slim: false);

		Assert.That(block.RelativePath, Is.EqualTo(displayPath));
	}
}