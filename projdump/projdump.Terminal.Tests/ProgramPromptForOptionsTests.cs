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
            "n",            // find-readme
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
		// A spare line is queued after type. If the mode question were (incorrectly)
		// asked, it would consume this sentinel and ModeArg would be non-null. When mode
		// is skipped, the token-stats question consumes it instead, which reads as "none".
		// Console.ReadLine() returns null (not an exception) once input runs out, so
		// without the sentinel this test would pass even if the skip were broken.
		using var input = new ConsoleInputScope(
            solutionPath,
            "",                     // output
            "n",                    // slim
            "n",                    // exclude-tests
			"n",                    // find-readme
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
        using var input = new ConsoleInputScope("MyApp.sln", "", "n", "n", "n", "", "", "");
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
        using var input = new ConsoleInputScope("MyApp.sln", "", "yes", "y", "n", "", "", "");

        var options = Program.PromptForOptions();

        Assert.That(options!.Slim, Is.True);
        Assert.That(options.ExcludeTests, Is.True);
    }

    [Test]
    public void PromptForOptions_ExcludeDirsCommaSeparated_ParsesAndTrims()
    {
        using var input = new ConsoleInputScope("MyApp.sln", "", "n", "n", "n", "", " wwwroot , docs ", "");

        var options = Program.PromptForOptions();

        Assert.That(options!.ExcludeDirs, Is.EquivalentTo(new[] { "wwwroot", "docs" }));
    }

	[Test]
	public void PromptForOptions_FindReadmeYes_SetsSearchReadme()
	{
		using var input = new ConsoleInputScope("MyApp.sln", "", "n", "n", "y", "", "", "");

		var options = Program.PromptForOptions();

		Assert.That(options!.SearchReadme, Is.True);
	}

	[TestCase("report", true, false)]
	[TestCase("REPORT", true, false)]
	[TestCase("console", false, true)]
	[TestCase("", false, false)]
	[TestCase("nonsense", false, false)]
	public void PromptForOptions_StatsAnswer_SetsMatchingFlags(string statsAnswer, bool expectedIncludeStats, bool expectedStatsOnly)
	{
		using var input = new ConsoleInputScope(
			"MyApp.csproj", // path
			"",             // output
			"n",            // slim
			"n",            // exclude-tests
			"n",            // find-readme
			"",             // scope
			"",             // exclude-dirs
			"",             // type
			"",             // mode
			statsAnswer);   // token stats

		var options = Program.PromptForOptions();

		using (Assert.EnterMultipleScope())
		{
			Assert.That(options!.IncludeStats, Is.EqualTo(expectedIncludeStats));
			Assert.That(options.StatsOnly, Is.EqualTo(expectedStatsOnly));
		}
	}
}