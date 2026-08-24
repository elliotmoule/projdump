using projdump.Terminal.Tests.TestSupport;

namespace projdump.Terminal.Tests;

[TestFixture]
public class ProgramExecuteIntegrationTests
{
    [Test]
    public void Execute_CSharpProject_WritesOutputFileWithProjectNameInFileName()
    {
        using var temp = new TempProjectDirectory();
        using var outputScope = new DefaultOutputDirectoryOverrideScope(temp.RootPath);
        string csprojPath = temp.WriteFile("MyApp.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        temp.WriteFile("Program.cs", "var builder = WebApplication.CreateBuilder(args);");

        var options = new Program.RunOptions(csprojPath, null, false, false, null, null, null, []);
        Program.Execute(options);

        string expectedOutputPath = temp.GetFullPath("MyApp-app-project.md");
        Assert.That(File.Exists(expectedOutputPath), Is.True);

        string content = File.ReadAllText(expectedOutputPath);
        Assert.That(content, Does.StartWith("# MyApp.csproj - App Project"));
        Assert.That(content, Does.Contain("WebApplication.CreateBuilder"));
    }

    [Test]
    public void Execute_SlimMode_AddsSlimSuffixToFileName_AndOmitsContent()
    {
        using var temp = new TempProjectDirectory();
        using var outputScope = new DefaultOutputDirectoryOverrideScope(temp.RootPath);
        string csprojPath = temp.WriteFile("MyApp.csproj", "<Project></Project>");
        temp.WriteFile("Program.cs", "var builder = WebApplication.CreateBuilder(args);");

        var options = new Program.RunOptions(csprojPath, null, true, false, null, null, null, []);
        Program.Execute(options);

        string expectedOutputPath = temp.GetFullPath("MyApp-app-project-slim.md");
        Assert.That(File.Exists(expectedOutputPath), Is.True);

        string content = File.ReadAllText(expectedOutputPath);
        Assert.That(content, Does.Not.Contain("WebApplication.CreateBuilder"));
        Assert.That(content, Does.Contain("_File size:"));
    }

    [Test]
    public void Execute_VueProject_UsesPackageJsonNameInOutputFileName()
    {
        using var temp = new TempProjectDirectory();
        using var outputScope = new DefaultOutputDirectoryOverrideScope(temp.RootPath);
        temp.WriteFile("package.json", """{ "name": "my-vue-app", "dependencies": { "vue": "^3.4.0" } }""");
        temp.WriteFile("main.js", "// entry");

        var options = new Program.RunOptions(temp.RootPath, null, false, false, null, null, null, []);
        Program.Execute(options);

        string expectedOutputPath = temp.GetFullPath("my-vue-app-app-project.md");
        Assert.That(File.Exists(expectedOutputPath), Is.True);
    }

    [Test]
    public void Execute_CustomOutputPath_WritesToSpecifiedFile()
    {
        // No DefaultOutputDirectoryOverrideScope needed here - an explicit
        // CustomOutputPath never reaches the default-resolution branch.
        using var temp = new TempProjectDirectory();
        string csprojPath = temp.WriteFile("MyApp.csproj", "<Project></Project>");
        temp.WriteFile("Program.cs", "// entry");
        string customOutput = temp.GetFullPath(Path.Combine("out", "context.md"));

        var options = new Program.RunOptions(csprojPath, customOutput, false, false, null, null, null, []);
        Program.Execute(options);

        Assert.That(File.Exists(customOutput), Is.True);
    }

    [Test]
    public void Execute_NoCustomOutputPath_WritesToConfiguredDefaultDirectory()
    {
        // Proves the Desktop-default resolution actually routes through
        // ResolveDefaultOutputDirectory / DefaultOutputDirectoryOverride,
        // without writing to the real Desktop during a test run.
        using var temp = new TempProjectDirectory();
        string csprojPath = temp.WriteFile("MyApp.csproj", "<Project></Project>");
        temp.WriteFile("Program.cs", "// entry");

        string fakeDesktop = temp.GetFullPath("fake-desktop");
        Directory.CreateDirectory(fakeDesktop);
        using var outputScope = new DefaultOutputDirectoryOverrideScope(fakeDesktop);

        var options = new Program.RunOptions(csprojPath, null, false, false, null, null, null, []);
        Program.Execute(options);

        Assert.That(File.Exists(Path.Combine(fakeDesktop, "MyApp-app-project.md")), Is.True);
    }

    [Test]
    public void Execute_InvalidPath_PrintsErrorAndWritesNoFile()
    {
        // Errors out before output-path resolution is ever reached - no override needed.
        using var temp = new TempProjectDirectory();
        string invalidPath = temp.GetFullPath("does-not-exist.csproj");
        var options = new Program.RunOptions(invalidPath, null, false, false, null, null, null, []);

        using var console = new ConsoleCapture();
        Program.Execute(options);

        Assert.That(console.Output, Does.Contain("Error:"));
        Assert.That(Directory.GetFiles(temp.RootPath, "*.md"), Is.Empty);
    }

    [Test]
    public void Execute_WebApiMode_DropsComponentFiles_ButKeepsControllers()
    {
        using var temp = new TempProjectDirectory();
        using var outputScope = new DefaultOutputDirectoryOverrideScope(temp.RootPath);
        string csprojPath = temp.WriteFile("MyApp.csproj", "<Project></Project>");
        temp.WriteFile("Program.cs", "// entry");
        temp.WriteFile(Path.Combine("Controllers", "OrdersController.cs"), "// controller");
        temp.WriteFile("MainWindow.xaml", "<Window></Window>");

        var options = new Program.RunOptions(csprojPath, null, false, false, null, null, "webapi", []);
        Program.Execute(options);

        string content = File.ReadAllText(temp.GetFullPath("MyApp-app-project.md"));
        Assert.That(content, Does.Contain("OrdersController.cs"));
        Assert.That(content, Does.Not.Contain("MainWindow.xaml"));
    }

    [Test]
    public void Execute_UnsupportedModeForType_PrintsErrorAndWritesNoFile()
    {
        // Vue only supports "default" - webapi is C#-only. Errors before output resolution.
        using var temp = new TempProjectDirectory();
        temp.WriteFile("package.json", """{ "name": "my-vue-app", "dependencies": { "vue": "^3.4.0" } }""");
        temp.WriteFile("main.js", "// entry");

        var options = new Program.RunOptions(temp.RootPath, null, false, false, null, null, "webapi", []);

        using var console = new ConsoleCapture();
        Program.Execute(options);

        Assert.That(console.Output, Does.Contain("Error:"));
        Assert.That(Directory.GetFiles(temp.RootPath, "*.md"), Is.Empty);
    }

    [Test]
    public void Execute_WebApiMode_DropsVendoredWwwrootNoise_ButKeepsHandWrittenCode()
    {
        // End-to-end coverage for the original bug report: vendored minified
        // libraries and images under wwwroot should disappear in webapi
        // mode, but hand-written wwwroot JS is a judgement call the user
        // gets to make (kept by default, droppable via --exclude-dir).
        using var temp = new TempProjectDirectory();
        using var outputScope = new DefaultOutputDirectoryOverrideScope(temp.RootPath);
        string csprojPath = temp.WriteFile("MyApp.csproj", "<Project></Project>");
        temp.WriteFile("Program.cs", "// entry");
        temp.WriteFile(Path.Combine("wwwroot", "lib", "jquery.min.js"), "/* vendored, should vanish */");
        temp.WriteFile(Path.Combine("wwwroot", "lib", "bootstrap.min.css"), "/* vendored, should vanish */");
        temp.WriteFile(Path.Combine("wwwroot", "images", "logo.png"), "binary");
        temp.WriteFile(Path.Combine("wwwroot", "site.css"), "body {}");
        temp.WriteFile(Path.Combine("wwwroot", "js", "site.js"), "// hand-written, calls the API");

        var options = new Program.RunOptions(csprojPath, null, false, false, null, null, "webapi", []);
        Program.Execute(options);

        string content = File.ReadAllText(temp.GetFullPath("MyApp-app-project.md"));
        Assert.That(content, Does.Not.Contain("jquery.min.js"));
        Assert.That(content, Does.Not.Contain("bootstrap.min.css"));
        Assert.That(content, Does.Not.Contain("logo.png"));
        Assert.That(content, Does.Not.Contain("site.css"));
        Assert.That(content, Does.Contain("site.js"));
    }

    [Test]
    public void Execute_ExcludeDirFlag_DropsWwwrootEntirely()
    {
        using var temp = new TempProjectDirectory();
        using var outputScope = new DefaultOutputDirectoryOverrideScope(temp.RootPath);
        string csprojPath = temp.WriteFile("MyApp.csproj", "<Project></Project>");
        temp.WriteFile("Program.cs", "// entry");
        temp.WriteFile(Path.Combine("wwwroot", "js", "site.js"), "// hand-written");

        var options = new Program.RunOptions(csprojPath, null, false, false, null, null, "webapi", ["wwwroot"]);
        Program.Execute(options);

        string content = File.ReadAllText(temp.GetFullPath("MyApp-app-project.md"));
        Assert.That(content, Does.Not.Contain("site.js"));
        Assert.That(content, Does.Contain("`--exclude-dir wwwroot`"));
    }
}