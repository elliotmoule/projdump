using projdump.Terminal.Tests.TestSupport;

namespace projdump.Terminal.Tests;

[TestFixture]
public class ProgramExecuteIntegrationTests
{
    [Test]
    public void Execute_CSharpProject_WritesOutputFileWithProjectNameInFileName()
    {
        using var temp = new TempProjectDirectory();
        string csprojPath = temp.WriteFile("MyApp.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        temp.WriteFile("Program.cs", "var builder = WebApplication.CreateBuilder(args);");

        var options = new Program.RunOptions(csprojPath, null, false, false, null, null, null);
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
        string csprojPath = temp.WriteFile("MyApp.csproj", "<Project></Project>");
        temp.WriteFile("Program.cs", "var builder = WebApplication.CreateBuilder(args);");

        var options = new Program.RunOptions(csprojPath, null, true, false, null, null, null);
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
        temp.WriteFile("package.json", """{ "name": "my-vue-app", "dependencies": { "vue": "^3.4.0" } }""");
        temp.WriteFile("main.js", "// entry");

        var options = new Program.RunOptions(temp.RootPath, null, false, false, null, null, null);
        Program.Execute(options);

        string expectedOutputPath = temp.GetFullPath("my-vue-app-app-project.md");
        Assert.That(File.Exists(expectedOutputPath), Is.True);
    }

    [Test]
    public void Execute_CustomOutputPath_WritesToSpecifiedFile()
    {
        using var temp = new TempProjectDirectory();
        string csprojPath = temp.WriteFile("MyApp.csproj", "<Project></Project>");
        temp.WriteFile("Program.cs", "// entry");
        string customOutput = temp.GetFullPath(Path.Combine("out", "context.md"));

        var options = new Program.RunOptions(csprojPath, customOutput, false, false, null, null, null);
        Program.Execute(options);

        Assert.That(File.Exists(customOutput), Is.True);
    }

    [Test]
    public void Execute_InvalidPath_PrintsErrorAndWritesNoFile()
    {
        using var temp = new TempProjectDirectory();
        string invalidPath = temp.GetFullPath("does-not-exist.csproj");
        var options = new Program.RunOptions(invalidPath, null, false, false, null, null, null);

        using var console = new ConsoleCapture();
        Program.Execute(options);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(console.Output, Does.Contain("Error:"));
            Assert.That(Directory.GetFiles(temp.RootPath, "*.md"), Is.Empty);
        }
    }

    [Test]
    public void Execute_WebApiMode_DropsComponentFiles_ButKeepsControllers()
    {
        using var temp = new TempProjectDirectory();
        string csprojPath = temp.WriteFile("MyApp.csproj", "<Project></Project>");
        temp.WriteFile("Program.cs", "// entry");
        temp.WriteFile(Path.Combine("Controllers", "OrdersController.cs"), "// controller");
        temp.WriteFile("MainWindow.xaml", "<Window></Window>");

        var options = new Program.RunOptions(csprojPath, null, false, false, null, null, "webapi");
        Program.Execute(options);

        string content = File.ReadAllText(temp.GetFullPath("MyApp-app-project.md"));
        Assert.That(content, Does.Contain("OrdersController.cs"));
        Assert.That(content, Does.Not.Contain("MainWindow.xaml"));
    }

    [Test]
    public void Execute_UnsupportedModeForType_PrintsErrorAndWritesNoFile()
    {
        // Vue only supports "default" - webapi is C#-only.
        using var temp = new TempProjectDirectory();
        temp.WriteFile("package.json", """{ "name": "my-vue-app", "dependencies": { "vue": "^3.4.0" } }""");
        temp.WriteFile("main.js", "// entry");

        var options = new Program.RunOptions(temp.RootPath, null, false, false, null, null, "webapi");

        using var console = new ConsoleCapture();
        Program.Execute(options);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(console.Output, Does.Contain("Error:"));
            Assert.That(Directory.GetFiles(temp.RootPath, "*.md"), Is.Empty);
        }
    }
}