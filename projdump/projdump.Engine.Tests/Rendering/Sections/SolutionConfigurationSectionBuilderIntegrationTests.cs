using projdump.Engine.Rendering;
using projdump.Engine.Rendering.Sections;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Rendering.Sections;

[TestFixture]
[Category("Integration")]
public class SolutionConfigurationSectionBuilderIntegrationTests
{
	readonly SolutionConfigurationSectionBuilder _builder = new(new ReportFileBlockFactory());

	[Test]
	public void Build_ProjectInput_ReturnsNull()
	{
		using var temp = new TempProjectDirectory();
		var csproj = new FileInfo(temp.WriteFile("MyApp.csproj", "<Project />"));
		var request = ReportRenderRequestFactory.Create(temp.RootDirectoryInfo, inputFileInfo: csproj, isSolution: false);

		Assert.That(_builder.Build(request), Is.Null);
	}

	[Test]
	public void Build_SlnxInput_FencesAsXmlWithFileNameAsPath()
	{
		using var temp = new TempProjectDirectory();
		var slnx = new FileInfo(temp.WriteFile("MyApp.slnx", "<Solution />"));
		var request = ReportRenderRequestFactory.Create(temp.RootDirectoryInfo, inputFileInfo: slnx, isSolution: true);

		var section = _builder.Build(request)!;

		using (Assert.EnterMultipleScope())
		{
			Assert.That(section.Name, Is.EqualTo(ReportSectionNames.SolutionConfiguration));
			Assert.That(section.Files.Single().RelativePath, Is.EqualTo("MyApp.slnx"));
			Assert.That(section.ToMarkdown(), Does.Contain("```xml"));
		}
	}

	[Test]
	public void Build_SlnInput_FencesAsText()
	{
		using var temp = new TempProjectDirectory();
		var sln = new FileInfo(temp.WriteFile("MyApp.sln", "Microsoft Visual Studio Solution File"));
		var request = ReportRenderRequestFactory.Create(temp.RootDirectoryInfo, inputFileInfo: sln, isSolution: true);

		Assert.That(_builder.Build(request)!.ToMarkdown(), Does.Contain("```text"));
	}
}