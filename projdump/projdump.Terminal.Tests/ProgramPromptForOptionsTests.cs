using projdump.Terminal.Tests.TestSupport;

namespace projdump.Terminal.Tests;

[TestFixture]
public class ProgramPromptForOptionsTests
{
    [Test]
    public void PromptForOptions_NonSolutionInput_AsksModeQuestion()
    {
        using var input = new ConsoleInputScope(
            "MyApp.csproj", // path
            "",             // output
            "n",            // slim
            "n",            // exclude-tests
            "",             // scope
            "",             // exclude-dirs
            "",             // type
            "webapi");      // mode

        var options = Program.PromptForOptions();

        Assert.That(options, Is.Not.Null);
        Assert.That(options!.InputPath, Is.EqualTo("MyApp.csproj"));
        Assert.That(options.ModeArg, Is.EqualTo("webapi"));
    }

    [TestCase("MyApp.sln")]
    [TestCase("MyApp.slnx")]
    public void PromptForOptions_SolutionInput_SkipsModeQuestion(string solutionPath)
    {
        // An 8th line is queued so that, if the mode question were still
        // (incorrectly) asked, it would consume this value instead of being
        // skipped. Console.ReadLine() returns null (not an exception) once
        // input is exhausted, and Prompt() treats that as blank - so with
        // too few lines this test would pass even if the skip were broken.
        // The sentinel makes the assertion a real one.
        using var input = new ConsoleInputScope(
            solutionPath,
            "",                     // output
            "n",                    // slim
            "n",                    // exclude-tests
            "",                     // scope
            "",                     // exclude-dirs
            "",                     // type
            "SHOULD-NOT-BE-READ");  // would be consumed as mode if not skipped

        var options = Program.PromptForOptions();

        Assert.That(options!.ModeArg, Is.Null);
    }

    [Test]
    public void PromptForOptions_SolutionInput_PrintsSkipNotice()
    {
        using var input = new ConsoleInputScope("MyApp.sln", "", "n", "n", "", "", "");
        using var console = new ConsoleCapture();

        Program.PromptForOptions();

        Assert.That(console.Output, Does.Contain("Solution detected"));
    }

    [Test]
    public void PromptForOptions_BlankPath_ReturnsNull()
    {
        using var input = new ConsoleInputScope("");

        var options = Program.PromptForOptions();

        Assert.That(options, Is.Null);
    }

    [Test]
    public void PromptForOptions_YesVariants_SetBooleanFlags()
    {
        using var input = new ConsoleInputScope("MyApp.sln", "", "yes", "y", "", "", "");

        var options = Program.PromptForOptions();

        Assert.That(options!.Slim, Is.True);
        Assert.That(options.ExcludeTests, Is.True);
    }

    [Test]
    public void PromptForOptions_ExcludeDirsCommaSeparated_ParsesAndTrims()
    {
        using var input = new ConsoleInputScope("MyApp.sln", "", "n", "n", "", " wwwroot , docs ", "");

        var options = Program.PromptForOptions();

        Assert.That(options!.ExcludeDirs, Is.EquivalentTo(new[] { "wwwroot", "docs" }));
    }
}