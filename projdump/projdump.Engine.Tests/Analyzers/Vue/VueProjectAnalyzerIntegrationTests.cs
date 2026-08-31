using projdump.Engine.Analyzers.Vue;
using projdump.Engine.Core;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Analyzers.Vue;

[TestFixture]
public class VueProjectAnalyzerIntegrationTests
{
    const string VuePackageJson = """{ "name": "my-vue-app", "dependencies": { "vue": "^3.4.0" } }""";

    static void BuildBasicProject(TempProjectDirectory temp, string packageJson = VuePackageJson)
    {
        temp.WriteFile("package.json", packageJson);
        temp.WriteFile("main.js", "// entry point");
        temp.WriteFile("App.vue", "<template></template>");
        temp.WriteFile(Path.Combine("components", "OrderList.vue"), "<template></template>");
        temp.WriteFile("README.md", "# my-vue-app");
        temp.WriteFile("vite.config.ts", "export default {}");
        temp.WriteFile(Path.Combine("node_modules", "vue", "index.js"), "junk");
        temp.WriteFile(Path.Combine("dist", "index.html"), "junk");
    }

	[Test]
	public void Analyze_PullsInTheRepositoryReadme_WhenThePackageHasNone()
	{
		using var temp = new TempProjectDirectory();
		Directory.CreateDirectory(temp.GetFullPath(".git"));
		temp.WriteFile("README.md", "# repo readme");
		temp.WriteFile(Path.Combine("frontend", "package.json"), VuePackageJson);
		temp.WriteFile(Path.Combine("frontend", "main.js"), "// entry point");

		var analysis = new VueProjectAnalyzer().Analyze(temp.GetFullPath("frontend"), new ProjectAnalysisOptions());

		Assert.That(analysis.ReadmeFiles.Select(f => f.File.FullName), Does.Contain(temp.GetFullPath("README.md")));
		Assert.That(analysis.AllFiles.Select(f => f.File.Name), Does.Not.Contain("README.md"));
	}

	[Test]
	public void Analyze_DoesNotSearchUpwards_WhenAReadmeSitsBesideThePackage()
	{
		using var temp = new TempProjectDirectory();
		Directory.CreateDirectory(temp.GetFullPath(".git"));
		temp.WriteFile("README.md", "# repo readme");
		temp.WriteFile(Path.Combine("frontend", "package.json"), VuePackageJson);
		temp.WriteFile(Path.Combine("frontend", "README.md"), "# frontend readme");

		var analysis = new VueProjectAnalyzer().Analyze(temp.GetFullPath("frontend"), new ProjectAnalysisOptions());

		Assert.That(analysis.ReadmeFiles.Select(f => f.File.FullName), Does.Not.Contain(temp.GetFullPath("README.md")));
	}

	[Test]
    public void CanHandle_ReturnsTrue_ForDirectoryWithVuePackageJson()
    {
        using var temp = new TempProjectDirectory();
        BuildBasicProject(temp);

        Assert.That(new VueProjectAnalyzer().CanHandle(temp.RootPath), Is.True);
    }

    [Test]
    public void CanHandle_ReturnsFalse_ForPackageJsonWithoutVue()
    {
        using var temp = new TempProjectDirectory();
        temp.WriteFile("package.json", """{ "name": "plain-node-app", "dependencies": { "express": "^4.0.0" } }""");

        Assert.That(new VueProjectAnalyzer().CanHandle(temp.RootPath), Is.False);
    }

    [Test]
    public void CanHandle_ReturnsTrue_ForVueInDevDependencies()
    {
        using var temp = new TempProjectDirectory();
        temp.WriteFile("package.json", """{ "name": "my-app", "devDependencies": { "vue": "^3.4.0" } }""");

        Assert.That(new VueProjectAnalyzer().CanHandle(temp.RootPath), Is.True);
    }

    [Test]
    public void CanHandle_ReturnsFalse_ForDirectoryWithoutPackageJson()
    {
        using var temp = new TempProjectDirectory();

        Assert.That(new VueProjectAnalyzer().CanHandle(temp.RootPath), Is.False);
    }

    [Test]
    public void Analyze_UsesPackageJsonNameField_AsProjectName()
    {
        using var temp = new TempProjectDirectory();
        BuildBasicProject(temp);

        var analysis = new VueProjectAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions());

        Assert.That(analysis.ProjectName, Is.EqualTo("my-vue-app"));
    }

    [Test]
    public void Analyze_FallsBackToDirectoryName_WhenNameFieldMissing()
    {
        using var temp = new TempProjectDirectory();
        BuildBasicProject(temp, """{ "dependencies": { "vue": "^3.4.0" } }""");

        var analysis = new VueProjectAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions());

        Assert.That(analysis.ProjectName, Is.EqualTo(temp.RootDirectoryInfo.Name));
    }

    [Test]
    public void Analyze_FallsBackToDirectoryName_WhenNameFieldIsNotAString()
    {
        // Valid JSON (so the vue-dependency check passes) but "name" is the
        // wrong type - exercises ResolveProjectName's fallback branch.
        using var temp = new TempProjectDirectory();
        BuildBasicProject(temp, """{ "name": 12345, "dependencies": { "vue": "^3.4.0" } }""");

        var analysis = new VueProjectAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions());

        Assert.That(analysis.ProjectName, Is.EqualTo(temp.RootDirectoryInfo.Name));
    }

    [Test]
    public void Analyze_ThrowsForMalformedPackageJson()
    {
        // Unparseable JSON fails the vue-dependency check before project-name
        // resolution is ever reached - this should throw, not silently guess.
        using var temp = new TempProjectDirectory();
        temp.WriteFile("package.json", "{ this is not valid json");

        Assert.Throws<ProjectAnalysisException>(() =>
            new VueProjectAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions()));
    }

    [Test]
    public void Analyze_ExcludesNodeModulesAndDist()
    {
        using var temp = new TempProjectDirectory();
        BuildBasicProject(temp);

        var analysis = new VueProjectAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions());

        var fullPaths = analysis.AllFiles.Select(f => f.File.FullName).ToList();
        
        using (Assert.EnterMultipleScope())
        {
            Assert.That(fullPaths, Has.None.Contains("node_modules"));
            Assert.That(fullPaths, Has.None.Contains("dist"));
            Assert.That(analysis.AllFiles.Select(f => f.File.Name), Does.Contain("main.js"));
        }
    }

    [Test]
    public void Analyze_ExcludesMinifiedAssets()
    {
        using var temp = new TempProjectDirectory();
        BuildBasicProject(temp);
        temp.WriteFile(Path.Combine("public", "vendor.min.js"), "/* vendored */");

        var analysis = new VueProjectAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions());

        Assert.That(analysis.AllFiles.Select(f => f.File.Name), Does.Not.Contain("vendor.min.js"));
    }

    [Test]
    public void Analyze_TagsImagesWithAssetRole()
    {
        using var temp = new TempProjectDirectory();
        BuildBasicProject(temp);
        temp.WriteFile(Path.Combine("public", "logo.png"), "binary");

        var analysis = new VueProjectAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions());

        Assert.That(analysis.AllFiles.Single(f => f.File.Name == "logo.png").Role, Is.EqualTo(FileRole.Asset));
    }

    [Test]
    public void Analyze_ExcludeDirs_DropsNamedDirectoryEntirely()
    {
        using var temp = new TempProjectDirectory();
        BuildBasicProject(temp);
        temp.WriteFile(Path.Combine("public", "logo.png"), "binary");

        var analysis = new VueProjectAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions { ExcludeDirs = ["public"] });

        var names = analysis.AllFiles.Select(f => f.File.Name).ToList();
        Assert.That(names, Does.Not.Contain("logo.png"));
        Assert.That(names, Does.Contain("main.js"));
    }

    [Test]
    public void Analyze_IsSolutionIsAlwaysFalse()
    {
        using var temp = new TempProjectDirectory();
        BuildBasicProject(temp);

        var analysis = new VueProjectAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions());

        Assert.That(analysis.IsSolution, Is.False);
    }

    [Test]
    public void Analyze_ProjFilesIsThePackageJsonItself()
    {
        using var temp = new TempProjectDirectory();
        BuildBasicProject(temp);

        var analysis = new VueProjectAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions());

        Assert.That(analysis.ProjFiles.Select(f => f.File.Name), Is.EquivalentTo(["package.json"]));
    }

    [Test]
    public void Analyze_WithScope_DoesNotAffectProjectName()
    {
        // Regression coverage: ProjectName must be resolved from the original
        // project root before --scope reassigns the analyzer's rootDir.
        using var temp = new TempProjectDirectory();
        BuildBasicProject(temp);

        var analysis = new VueProjectAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions { ScopeDir = "components" });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(analysis.ProjectName, Is.EqualTo("my-vue-app"));
            Assert.That(analysis.AllFiles.Select(f => f.File.Name), Does.Contain("OrderList.vue"));
            Assert.That(analysis.AllFiles.Select(f => f.File.Name), Does.Not.Contain("main.js"));
        }
    }

    [Test]
    public void Analyze_ThrowsForDirectoryWithoutPackageJson()
    {
        using var temp = new TempProjectDirectory();

        Assert.Throws<ProjectAnalysisException>(() =>
            new VueProjectAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions()));
    }

    [Test]
    public void Analyze_ThrowsWhenPackageJsonDoesNotDeclareVue()
    {
        using var temp = new TempProjectDirectory();
        temp.WriteFile("package.json", """{ "name": "plain-node-app" }""");

        Assert.Throws<ProjectAnalysisException>(() =>
            new VueProjectAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions()));
    }
}