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

        Assert.That(analysis.CodeFiles[0].File.Name, Is.EqualTo("Program.cs"));
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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(analysis.ProjFiles.Select(f => f.File.Name), Is.EquivalentTo(["MyApp.csproj"]));
            Assert.That(analysis.IsSolution, Is.False);
        }
    }

    [Test]
    public void Analyze_SolutionMode_ProjFilesIncludesEveryCsprojFound()
    {
        using var temp = new TempProjectDirectory();
        temp.WriteFile("MyApp.slnx", "<Solution></Solution>");
        temp.WriteFile(Path.Combine("Api", "MyApp.Api.csproj"), "<Project></Project>");
        temp.WriteFile(Path.Combine("Core", "MyApp.Core.csproj"), "<Project></Project>");

        var analysis = new CSharpAnalyzer().Analyze(temp.GetFullPath("MyApp.slnx"), new ProjectAnalysisOptions());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(analysis.IsSolution, Is.True);
            Assert.That(analysis.ProjFiles.Select(f => f.File.Name), Is.EquivalentTo(["MyApp.Api.csproj", "MyApp.Core.csproj"]));
        }
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
}