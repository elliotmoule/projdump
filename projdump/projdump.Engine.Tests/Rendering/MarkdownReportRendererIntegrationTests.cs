using projdump.Engine.Rendering;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Rendering;

[TestFixture]
public class MarkdownReportRendererIntegrationTests
{
    static ReportRenderRequest BuildRequest(
        TempProjectDirectory temp,
        bool isSolution,
        bool slim = false,
        bool excludeTests = false,
        string? scopeDir = null,
        bool includeReadme = true,
        bool includeConfig = true)
    {
        string csprojPath = temp.WriteFile("MyApp.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        string programPath = temp.WriteFile("Program.cs", "var builder = WebApplication.CreateBuilder(args);");
        string configPath = temp.WriteFile("appsettings.json", "{ \"Key\": \"Value\" }");
        string readmePath = temp.WriteFile("README.md", "# MyApp\n\nSample readme content.");

        var inputFileInfo = new FileInfo(csprojPath);
        var allFiles = new List<FileInfo> { new(csprojPath), new(programPath), new(configPath), new(readmePath) };

        return new ReportRenderRequest
        {
            InputFileInfo = inputFileInfo,
            RootDir = temp.RootDirectoryInfo,
            IsSolution = isSolution,
            Extension = ".csproj",
            Slim = slim,
            ExcludeTests = excludeTests,
            ScopeDir = scopeDir,
            AllFiles = allFiles,
            CodeFiles = [new FileInfo(programPath)],
            ConfigFiles = includeConfig ? [new FileInfo(configPath)] : [],
            ReadmeFiles = includeReadme ? [new FileInfo(readmePath)] : [],
            ProjFiles = [new FileInfo(csprojPath)],
        };
    }

    [Test]
    public void Render_HeaderShowsProjectFileName_AndKind()
    {
        using var temp = new TempProjectDirectory();
        var request = BuildRequest(temp, isSolution: false);

        var (output, _) = MarkdownReportRenderer.Render(request);

        Assert.That(output, Does.StartWith("# MyApp.csproj - App Project"));
    }

    [Test]
    public void Render_SlimMode_AddsModeLabelAndNote()
    {
        using var temp = new TempProjectDirectory();
        var request = BuildRequest(temp, isSolution: false, slim: true);

        var (output, _) = MarkdownReportRenderer.Render(request);

        Assert.That(output, Does.Contain("App Project (slim)"));
        Assert.That(output, Does.Contain("**Slim mode:** file contents are omitted"));
    }

    [Test]
    public void Render_SlimMode_ShowsFileSizeInsteadOfContent()
    {
        using var temp = new TempProjectDirectory();
        var request = BuildRequest(temp, isSolution: false, slim: true);

        var (output, _) = MarkdownReportRenderer.Render(request);

        Assert.That(output, Does.Contain("_File size:"));
        Assert.That(output, Does.Not.Contain("WebApplication.CreateBuilder"));
    }

    [Test]
    public void Render_NonSlimMode_IncludesRealFileContent()
    {
        using var temp = new TempProjectDirectory();
        var request = BuildRequest(temp, isSolution: false, slim: false);

        var (output, _) = MarkdownReportRenderer.Render(request);

        Assert.That(output, Does.Contain("WebApplication.CreateBuilder"));
        Assert.That(output, Does.Contain("Sample readme content."));
    }

    [Test]
    public void Render_ReplacesTokenPlaceholder_WithRealEstimate()
    {
        using var temp = new TempProjectDirectory();
        var request = BuildRequest(temp, isSolution: false);

        var (output, estimatedTokens) = MarkdownReportRenderer.Render(request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(output, Does.Not.Contain("~4,849"));
            Assert.That(output, Does.Contain($"~{estimatedTokens:N0}"));
            Assert.That(estimatedTokens, Is.GreaterThan(0));
        }
    }

    [Test]
    public void Render_FlagsNote_ListsActiveFlags()
    {
        using var temp = new TempProjectDirectory();
        var request = BuildRequest(temp, isSolution: false, slim: true, excludeTests: true, scopeDir: "src");

        var (output, _) = MarkdownReportRenderer.Render(request);

        Assert.That(output, Does.Contain("**Flags:**"));
        Assert.That(output, Does.Contain("`--slim`"));
        Assert.That(output, Does.Contain("`--exclude-tests`"));
        Assert.That(output, Does.Contain("`--scope src`"));
    }

    [Test]
    public void Render_NoFlagsSet_OmitsFlagsNote()
    {
        using var temp = new TempProjectDirectory();
        var request = BuildRequest(temp, isSolution: false);

        var (output, _) = MarkdownReportRenderer.Render(request);

        Assert.That(output, Does.Not.Contain("**Flags:**"));
    }

    [Test]
    public void Render_ProjectSummary_CountsFilesByExtension()
    {
        using var temp = new TempProjectDirectory();
        var request = BuildRequest(temp, isSolution: false);

        var (output, _) = MarkdownReportRenderer.Render(request);

        Assert.That(output, Does.Contain("| .cs | 1 |"));
        Assert.That(output, Does.Contain("| .csproj | 1 |"));
        Assert.That(output, Does.Contain("| .json | 1 |"));
        Assert.That(output, Does.Contain("| .md | 1 |"));
    }

    [Test]
    public void Render_ProjectStructure_ListsPathsRelativeToRoot()
    {
        using var temp = new TempProjectDirectory();
        var request = BuildRequest(temp, isSolution: false);

        var (output, _) = MarkdownReportRenderer.Render(request);

        Assert.That(output, Does.Contain("## Project Structure"));
        Assert.That(output, Does.Contain("Program.cs"));
        Assert.That(output, Does.Not.Contain(temp.RootPath)); // must be relative, not absolute
    }

    [Test]
    public void Render_SolutionConfiguration_OnlyAppearsWhenIsSolution()
    {
        using var temp = new TempProjectDirectory();
        var solutionRequest = BuildRequest(temp, isSolution: true);
        var projectRequest = BuildRequest(temp, isSolution: false);

        var (solutionOutput, _) = MarkdownReportRenderer.Render(solutionRequest);
        var (projectOutput, _) = MarkdownReportRenderer.Render(projectRequest);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(solutionOutput, Does.Contain("## Solution Configuration"));
            Assert.That(projectOutput, Does.Not.Contain("## Solution Configuration"));
        }
    }

    [Test]
    public void Render_ProjectDependencies_IncludesCsprojContentAsXml()
    {
        using var temp = new TempProjectDirectory();
        var request = BuildRequest(temp, isSolution: false);

        var (output, _) = MarkdownReportRenderer.Render(request);

        Assert.That(output, Does.Contain("## Project Dependencies"));
        Assert.That(output, Does.Contain("```xml"));
        Assert.That(output, Does.Contain("Microsoft.NET.Sdk"));
    }

    [Test]
    public void Render_Configuration_UsesCorrectLanguageForJson()
    {
        using var temp = new TempProjectDirectory();
        var request = BuildRequest(temp, isSolution: false);

        var (output, _) = MarkdownReportRenderer.Render(request);

        Assert.That(output, Does.Contain("## Configuration"));
        Assert.That(output, Does.Contain("```json"));
    }

    [Test]
    public void Render_AppCode_UsesCorrectLanguageForCs()
    {
        using var temp = new TempProjectDirectory();
        var request = BuildRequest(temp, isSolution: false);

        var (output, _) = MarkdownReportRenderer.Render(request);

        Assert.That(output, Does.Contain("## App Code"));
        Assert.That(output, Does.Contain("```csharp"));
    }

    [Test]
    public void Render_NoReadmeFiles_OmitsDocumentationSection()
    {
        using var temp = new TempProjectDirectory();
        var request = BuildRequest(temp, isSolution: false, includeReadme: false);

        var (output, _) = MarkdownReportRenderer.Render(request);

        Assert.That(output, Does.Not.Contain("## Documentation"));
    }

    [Test]
    public void Render_NoConfigFiles_OmitsConfigurationSection()
    {
        using var temp = new TempProjectDirectory();
        var request = BuildRequest(temp, isSolution: false, includeConfig: false);

        var (output, _) = MarkdownReportRenderer.Render(request);

        Assert.That(output, Does.Not.Contain("## Configuration"));
    }
}