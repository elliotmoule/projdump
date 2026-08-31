using projdump.Engine.Analyzers.CSharp;
using projdump.Engine.Core;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Analyzers.CSharp;

[TestFixture]
public class CSharpAnalyzerIntegrationTests
{
    static string BuildBasicProject(TempProjectDirectory temp)
    {
        temp.WriteFile("MyApp.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        temp.WriteFile("Program.cs", "// entry point");
        temp.WriteFile("appsettings.json", "{}");
        temp.WriteFile("README.md", "# MyApp");
        temp.WriteFile(Path.Combine("Controllers", "OrdersController.cs"), "// controller");
        temp.WriteFile(Path.Combine("Models", "OrderDto.cs"), "// dto");
        temp.WriteFile(Path.Combine("bin", "Debug", "MyApp.dll"), "junk");
        temp.WriteFile(Path.Combine("obj", "Debug", "MyApp.dll"), "junk");
        return temp.GetFullPath("MyApp.csproj");
    }

	// Mirrors a real repo layout - solution and README at the root, project a couple of levels down.
	// The .git marker bounds the ancestor walk so it can't escape into the system temp folder.
	static string BuildNestedProject(TempProjectDirectory temp, bool writeRootReadme = true)
	{
		Directory.CreateDirectory(temp.GetFullPath(".git"));
		temp.WriteFile("MyApp.slnx", "<Solution />");
		if (writeRootReadme)
			temp.WriteFile("README.md", "# repo readme");

		temp.WriteFile(Path.Combine("src", "MyApp.Api", "Program.cs"), "// entry point");
		return temp.WriteFile(Path.Combine("src", "MyApp.Api", "MyApp.Api.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
	}

	[Test]
	public void Analyze_PullsInTheRepositoryReadme_WhenTheProjectHasNone()
	{
		using var temp = new TempProjectDirectory();
		string csprojPath = BuildNestedProject(temp);

		var analysis = new CSharpAnalyzer().Analyze(csprojPath, new ProjectAnalysisOptions());

		Assert.That(analysis.ReadmeFiles.Select(f => f.File.FullName), Does.Contain(temp.GetFullPath("README.md")));
	}

	[Test]
	public void Analyze_KeepsTheAncestorReadmeOutOfTheFileListing()
	{
		using var temp = new TempProjectDirectory();
		string csprojPath = BuildNestedProject(temp);

		var analysis = new CSharpAnalyzer().Analyze(csprojPath, new ProjectAnalysisOptions());

		Assert.That(analysis.AllFiles.Select(f => f.File.Name), Does.Not.Contain("README.md"));
	}

	[Test]
	public void Analyze_DoesNotSearchUpwards_WhenAReadmeSitsBesideTheProject()
	{
		using var temp = new TempProjectDirectory();
		string csprojPath = BuildNestedProject(temp);
		temp.WriteFile(Path.Combine("src", "MyApp.Api", "README.md"), "# project readme");

		var analysis = new CSharpAnalyzer().Analyze(csprojPath, new ProjectAnalysisOptions());

		var readmePaths = analysis.ReadmeFiles.Select(f => f.File.FullName).ToList();
		Assert.That(readmePaths, Does.Contain(temp.GetFullPath(Path.Combine("src", "MyApp.Api", "README.md"))));
		Assert.That(readmePaths, Does.Not.Contain(temp.GetFullPath("README.md")));
	}

	[Test]
	public void Analyze_IncludesATextReadme_BesideTheProject()
	{
		using var temp = new TempProjectDirectory();
		string csprojPath = BuildNestedProject(temp, writeRootReadme: false);
		temp.WriteFile(Path.Combine("src", "MyApp.Api", "README.txt"), "project readme");

		var analysis = new CSharpAnalyzer().Analyze(csprojPath, new ProjectAnalysisOptions());

		// The recursive gather only collects .md, so this can only arrive via the readme merge.
		Assert.That(analysis.ReadmeFiles.Select(f => f.File.Name), Does.Contain("README.txt"));
	}

	[Test]
	public void Analyze_LeavesReadmeFilesEmpty_WhenNoneExistsAnywhere()
	{
		using var temp = new TempProjectDirectory();
		string csprojPath = BuildNestedProject(temp, writeRootReadme: false);

		var analysis = new CSharpAnalyzer().Analyze(csprojPath, new ProjectAnalysisOptions());

		Assert.That(analysis.ReadmeFiles, Is.Empty);
	}

	[Test]
	public void Analyze_SolutionInput_PullsInAReadmeFromAboveTheSolution()
	{
		using var temp = new TempProjectDirectory();
		Directory.CreateDirectory(temp.GetFullPath(".git"));
		temp.WriteFile("README.md", "# repo readme");
		temp.WriteFile(Path.Combine("build", "MyApp.slnx"), "<Solution />");
		temp.WriteFile(Path.Combine("build", "MyApp", "MyApp.csproj"), "<Project />");

		var analysis = new CSharpAnalyzer().Analyze(temp.GetFullPath(Path.Combine("build", "MyApp.slnx")), new ProjectAnalysisOptions());

		Assert.That(analysis.ReadmeFiles.Select(f => f.File.FullName), Does.Contain(temp.GetFullPath("README.md")));
	}

	[Test]
	public void Analyze_WithScope_StillFindsTheReadmeBesideTheProject()
	{
		using var temp = new TempProjectDirectory();
		string csprojPath = BuildNestedProject(temp, writeRootReadme: false);
		temp.WriteFile(Path.Combine("src", "MyApp.Api", "README.md"), "# project readme");
		temp.WriteFile(Path.Combine("src", "MyApp.Api", "Controllers", "OrdersController.cs"), "// controller");

		// Scoping moves RootDir into Controllers, so the README is outside the gathered tree.
		var analysis = new CSharpAnalyzer().Analyze(csprojPath, new ProjectAnalysisOptions { ScopeDir = "Controllers" });

		Assert.That(analysis.ReadmeFiles.Select(f => f.File.Name), Does.Contain("README.md"));
	}

	[Test]
    public void Analyze_ExcludesBinAndObjFolders()
    {
        using var temp = new TempProjectDirectory();
        string csprojPath = BuildBasicProject(temp);

        var analysis = new CSharpAnalyzer().Analyze(csprojPath, new ProjectAnalysisOptions());

        var names = analysis.AllFiles.Select(f => f.File.Name).ToList();
        Assert.That(names, Does.Not.Contain("MyApp.dll"));
    }

    [Test]
    public void Analyze_OrdersCodeFilesByPriority_EntryPointFirst()
    {
        using var temp = new TempProjectDirectory();
        string csprojPath = BuildBasicProject(temp);

        var analysis = new CSharpAnalyzer().Analyze(csprojPath, new ProjectAnalysisOptions());

        Assert.That(analysis.CodeFiles.First().File.Name, Is.EqualTo("Program.cs"));
    }

    [Test]
    public void Analyze_AssignsApiSurfaceRole_ToControllerFile()
    {
        using var temp = new TempProjectDirectory();
        string csprojPath = BuildBasicProject(temp);

        var analysis = new CSharpAnalyzer().Analyze(csprojPath, new ProjectAnalysisOptions());

        var controller = analysis.AllFiles.Single(f => f.File.Name == "OrdersController.cs");
        Assert.That(controller.Role, Is.EqualTo(FileRole.ApiSurface));
    }

    [Test]
    public void Analyze_IncludesReadmeFile()
    {
        using var temp = new TempProjectDirectory();
        string csprojPath = BuildBasicProject(temp);

        var analysis = new CSharpAnalyzer().Analyze(csprojPath, new ProjectAnalysisOptions());

        Assert.That(analysis.ReadmeFiles.Select(f => f.File.Name), Does.Contain("README.md"));
    }

    [Test]
    public void Analyze_IncludesConfigFile()
    {
        using var temp = new TempProjectDirectory();
        string csprojPath = BuildBasicProject(temp);

        var analysis = new CSharpAnalyzer().Analyze(csprojPath, new ProjectAnalysisOptions());

        Assert.That(analysis.ConfigFiles.Select(f => f.File.Name), Does.Contain("appsettings.json"));
    }

    [Test]
    public void Analyze_SetsProjectNameFromCsprojFileName()
    {
        using var temp = new TempProjectDirectory();
        string csprojPath = BuildBasicProject(temp);

        var analysis = new CSharpAnalyzer().Analyze(csprojPath, new ProjectAnalysisOptions());

        Assert.That(analysis.ProjectName, Is.EqualTo("MyApp"));
    }

    [Test]
    public void Analyze_ProjectMode_ProjFilesIsJustTheCsprojItself()
    {
        using var temp = new TempProjectDirectory();
        string csprojPath = BuildBasicProject(temp);

        var analysis = new CSharpAnalyzer().Analyze(csprojPath, new ProjectAnalysisOptions());

        Assert.That(analysis.ProjFiles.Select(f => f.File.Name), Is.EquivalentTo(new[] { "MyApp.csproj" }));
        Assert.That(analysis.IsSolution, Is.False);
    }

    [Test]
    public void Analyze_SolutionMode_ProjFilesIncludesEveryCsprojFound()
    {
        using var temp = new TempProjectDirectory();
        temp.WriteFile("MyApp.slnx", "<Solution></Solution>");
        temp.WriteFile(Path.Combine("Api", "MyApp.Api.csproj"), "<Project></Project>");
        temp.WriteFile(Path.Combine("Core", "MyApp.Core.csproj"), "<Project></Project>");

        var analysis = new CSharpAnalyzer().Analyze(temp.GetFullPath("MyApp.slnx"), new ProjectAnalysisOptions());

        Assert.That(analysis.IsSolution, Is.True);
        Assert.That(analysis.ProjFiles.Select(f => f.File.Name), Is.EquivalentTo(new[] { "MyApp.Api.csproj", "MyApp.Core.csproj" }));
    }

    [Test]
    public void Analyze_WithExcludeTests_RemovesTestFiles()
    {
        using var temp = new TempProjectDirectory();
        string csprojPath = BuildBasicProject(temp);
        temp.WriteFile(Path.Combine("Tests", "OrderServiceTests.cs"), "// test");

        var analysis = new CSharpAnalyzer().Analyze(csprojPath, new ProjectAnalysisOptions { ExcludeTests = true });

        Assert.That(analysis.AllFiles.Select(f => f.File.Name), Does.Not.Contain("OrderServiceTests.cs"));
    }

    [Test]
    public void Analyze_WithoutExcludeTests_KeepsTestFiles()
    {
        using var temp = new TempProjectDirectory();
        string csprojPath = BuildBasicProject(temp);
        temp.WriteFile(Path.Combine("Tests", "OrderServiceTests.cs"), "// test");

        var analysis = new CSharpAnalyzer().Analyze(csprojPath, new ProjectAnalysisOptions { ExcludeTests = false });

        Assert.That(analysis.AllFiles.Select(f => f.File.Name), Does.Contain("OrderServiceTests.cs"));
    }

    [Test]
    public void Analyze_WithScope_RestrictsToSubdirectory()
    {
        using var temp = new TempProjectDirectory();
        string csprojPath = BuildBasicProject(temp);
        temp.WriteFile(Path.Combine("OutsideScope", "Elsewhere.cs"), "// should not appear");

        var analysis = new CSharpAnalyzer().Analyze(csprojPath, new ProjectAnalysisOptions { ScopeDir = "Controllers" });

        var names = analysis.AllFiles.Select(f => f.File.Name).ToList();
        Assert.That(names, Does.Contain("OrdersController.cs"));
        Assert.That(names, Does.Not.Contain("Elsewhere.cs"));
        Assert.That(names, Does.Not.Contain("Program.cs"));
    }

    [Test]
    public void Analyze_ThrowsForNonExistentFile()
    {
        Assert.Throws<ProjectAnalysisException>(() =>
            new CSharpAnalyzer().Analyze(Path.Combine(Path.GetTempPath(), "definitely-not-real.csproj"), new ProjectAnalysisOptions()));
    }

    [Test]
    public void Analyze_ThrowsForInvalidScopeDirectory()
    {
        using var temp = new TempProjectDirectory();
        string csprojPath = BuildBasicProject(temp);

        Assert.Throws<ProjectAnalysisException>(() =>
            new CSharpAnalyzer().Analyze(csprojPath, new ProjectAnalysisOptions { ScopeDir = "DoesNotExist" }));
    }

    [Test]
    public void Analyze_ExcludesMinifiedVendorAssets_UnderWwwroot()
    {
        // Regression coverage: .min.js/.min.css exclusion previously only
        // applied to Vue projects, so vendored libraries under a C#
        // project's wwwroot/lib were never dropped.
        using var temp = new TempProjectDirectory();
        string csprojPath = BuildBasicProject(temp);
        temp.WriteFile(Path.Combine("wwwroot", "lib", "jquery.min.js"), "/* huge vendored file */");
        temp.WriteFile(Path.Combine("wwwroot", "lib", "bootstrap.min.css"), "/* huge vendored file */");

        var analysis = new CSharpAnalyzer().Analyze(csprojPath, new ProjectAnalysisOptions());

        var names = analysis.AllFiles.Select(f => f.File.Name).ToList();
        Assert.That(names, Does.Not.Contain("jquery.min.js"));
        Assert.That(names, Does.Not.Contain("bootstrap.min.css"));
    }

    [Test]
    public void Analyze_TagsWwwrootImagesAndCssWithRolesWebApiModeCanFilter()
    {
        using var temp = new TempProjectDirectory();
        string csprojPath = BuildBasicProject(temp);
        temp.WriteFile(Path.Combine("wwwroot", "images", "logo.png"), "binary");
        temp.WriteFile(Path.Combine("wwwroot", "site.css"), "body {}");
        temp.WriteFile(Path.Combine("wwwroot", "js", "site.js"), "// hand-written, not vendored");

        var analysis = new CSharpAnalyzer().Analyze(csprojPath, new ProjectAnalysisOptions());

        Assert.That(analysis.AllFiles.Single(f => f.File.Name == "logo.png").Role, Is.EqualTo(FileRole.Asset));
        Assert.That(analysis.AllFiles.Single(f => f.File.Name == "site.css").Role, Is.EqualTo(FileRole.Style));
        // Hand-written JS gets Other, same as any other unclassified code - not dropped by role alone.
        Assert.That(analysis.AllFiles.Single(f => f.File.Name == "site.js").Role, Is.EqualTo(FileRole.Other));
    }

    [Test]
    public void Analyze_ExcludeDirs_DropsNamedDirectoryEntirely()
    {
        using var temp = new TempProjectDirectory();
        string csprojPath = BuildBasicProject(temp);
        temp.WriteFile(Path.Combine("wwwroot", "js", "site.js"), "// hand-written");
        temp.WriteFile(Path.Combine("wwwroot", "images", "logo.png"), "binary");

        var analysis = new CSharpAnalyzer().Analyze(csprojPath, new ProjectAnalysisOptions { ExcludeDirs = ["wwwroot"] });

        var names = analysis.AllFiles.Select(f => f.File.Name).ToList();
        Assert.That(names, Does.Not.Contain("site.js"));
        Assert.That(names, Does.Not.Contain("logo.png"));
        Assert.That(names, Does.Contain("Program.cs"));
    }

    [Test]
    public void CanHandle_ReturnsTrue_ForDirectoryContainingSlnx()
    {
        using var temp = new TempProjectDirectory();
        temp.WriteFile("MyApp.slnx", "<Solution></Solution>");

        Assert.That(new CSharpAnalyzer().CanHandle(temp.RootPath), Is.True);
    }

    [Test]
    public void CanHandle_ReturnsTrue_ForDirectoryContainingSln()
    {
        using var temp = new TempProjectDirectory();
        temp.WriteFile("MyApp.sln", "Microsoft Visual Studio Solution File");

        Assert.That(new CSharpAnalyzer().CanHandle(temp.RootPath), Is.True);
    }

    [Test]
    public void CanHandle_ReturnsFalse_ForDirectoryWithNoSolutionFile()
    {
        using var temp = new TempProjectDirectory();
        temp.WriteFile("Program.cs", "// entry");

        Assert.That(new CSharpAnalyzer().CanHandle(temp.RootPath), Is.False);
    }

    [Test]
    public void CanHandle_ReturnsFalse_ForDirectoryWhereSolutionIsOnlyInASubfolder()
    {
        // Non-recursive - matches how VueProjectAnalyzer only looks for
        // package.json directly in the given directory, not deeper.
        using var temp = new TempProjectDirectory();
        temp.WriteFile(Path.Combine("Nested", "MyApp.sln"), "Microsoft Visual Studio Solution File");

        Assert.That(new CSharpAnalyzer().CanHandle(temp.RootPath), Is.False);
    }

    [Test]
    public void Analyze_DirectoryInput_PrefersSlnxOverSln_WhenBothExist()
    {
        using var temp = new TempProjectDirectory();
        temp.WriteFile("MyApp.sln", "Microsoft Visual Studio Solution File");
        temp.WriteFile("MyApp.slnx", "<Solution></Solution>");
        temp.WriteFile("Program.cs", "// entry");

        var analysis = new CSharpAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions());

        Assert.That(analysis.Extension, Is.EqualTo(".slnx"));
    }

    [Test]
    public void Analyze_DirectoryInput_ResolvesSingleSlnFile()
    {
        using var temp = new TempProjectDirectory();
        temp.WriteFile("MyApp.sln", "Microsoft Visual Studio Solution File");
        temp.WriteFile("Program.cs", "// entry");

        var analysis = new CSharpAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions());

        Assert.That(analysis.IsSolution, Is.True);
        Assert.That(analysis.ProjectName, Is.EqualTo("MyApp"));
    }

    [Test]
    public void Analyze_DirectoryInput_ThrowsForMultipleSlnxFiles()
    {
        using var temp = new TempProjectDirectory();
        temp.WriteFile("First.slnx", "<Solution></Solution>");
        temp.WriteFile("Second.slnx", "<Solution></Solution>");

        Assert.Throws<ProjectAnalysisException>(() =>
            new CSharpAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions()));
    }

    [Test]
    public void Analyze_DirectoryInput_ThrowsForMultipleSlnFiles()
    {
        using var temp = new TempProjectDirectory();
        temp.WriteFile("First.sln", "Microsoft Visual Studio Solution File");
        temp.WriteFile("Second.sln", "Microsoft Visual Studio Solution File");

        Assert.Throws<ProjectAnalysisException>(() =>
            new CSharpAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions()));
    }

    [Test]
    public void Analyze_DirectoryInput_ThrowsWhenNoSolutionFileFound()
    {
        using var temp = new TempProjectDirectory();
        temp.WriteFile("Program.cs", "// entry");

        Assert.Throws<ProjectAnalysisException>(() =>
            new CSharpAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions()));
    }
}