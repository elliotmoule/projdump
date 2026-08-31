namespace projdump.Terminal.Tests;

[TestFixture]
public class ProgramParseArgsTests
{
    [Test]
    public void ParseArgs_SinglePositionalArg_SetsInputPathOnly()
    {
        var options = Program.ParseArgs(["MyApp.sln"]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(options, Is.Not.Null);
            Assert.That(options!.InputPath, Is.EqualTo("MyApp.sln"));
            Assert.That(options.CustomOutputPath, Is.Null);
            Assert.That(options.Slim, Is.False);
            Assert.That(options.ExcludeTests, Is.False);
        }
    }

    [Test]
    public void ParseArgs_TwoPositionalArgs_SetsInputAndOutputPath()
    {
        var options = Program.ParseArgs(["MyApp.sln", "output.md"]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(options!.InputPath, Is.EqualTo("MyApp.sln"));
            Assert.That(options.CustomOutputPath, Is.EqualTo("output.md"));
        }
    }

    [Test]
    public void ParseArgs_SlimFlag_SetsSlimTrue()
    {
        var options = Program.ParseArgs(["MyApp.sln", "--slim"]);

        Assert.That(options!.Slim, Is.True);
    }

    [Test]
    public void ParseArgs_ExcludeTestsFlag_SetsExcludeTestsTrue()
    {
        var options = Program.ParseArgs(["MyApp.sln", "--exclude-tests"]);

        Assert.That(options!.ExcludeTests, Is.True);
    }

    [Test]
    public void ParseArgs_ScopeFlag_SetsScopeDir()
    {
        var options = Program.ParseArgs(["MyApp.sln", "--scope", "src/Api"]);

        Assert.That(options!.ScopeDir, Is.EqualTo("src/Api"));
    }

    [Test]
    public void ParseArgs_ScopeFlagWithoutValue_ReturnsNull()
    {
        var options = Program.ParseArgs(["MyApp.sln", "--scope"]);

        Assert.That(options, Is.Null);
    }

    [Test]
    public void ParseArgs_ExcludeDirFlag_AddsToList()
    {
        var options = Program.ParseArgs(["MyApp.sln", "--exclude-dir", "wwwroot"]);

        Assert.That(options!.ExcludeDirs, Is.EquivalentTo(["wwwroot"]));
    }

    [Test]
    public void ParseArgs_ExcludeDirFlag_IsRepeatable()
    {
        var options = Program.ParseArgs(["MyApp.sln", "--exclude-dir", "wwwroot", "--exclude-dir", "docs"]);

        Assert.That(options!.ExcludeDirs, Is.EquivalentTo(["wwwroot", "docs"]));
    }

    [Test]
    public void ParseArgs_ExcludeDirFlagWithoutValue_ReturnsNull()
    {
        var options = Program.ParseArgs(["MyApp.sln", "--exclude-dir"]);

        Assert.That(options, Is.Null);
    }

    [Test]
    public void ParseArgs_NoExcludeDirFlag_DefaultsToEmpty()
    {
        var options = Program.ParseArgs(["MyApp.sln"]);

        Assert.That(options!.ExcludeDirs, Is.Empty);
    }

    [Test]
    public void ParseArgs_TypeFlag_SetsTypeArg()
    {
        var options = Program.ParseArgs(["MyApp.sln", "--type", "csharp"]);

        Assert.That(options!.TypeArg, Is.EqualTo("csharp"));
    }

    [Test]
    public void ParseArgs_TypeFlagWithoutValue_ReturnsNull()
    {
        var options = Program.ParseArgs(["MyApp.sln", "--type"]);

        Assert.That(options, Is.Null);
    }

    [Test]
    public void ParseArgs_ModeFlag_SetsModeArg()
    {
        var options = Program.ParseArgs(["MyApp.sln", "--mode", "webapi"]);

        Assert.That(options!.ModeArg, Is.EqualTo("webapi"));
    }

    [Test]
    public void ParseArgs_ModeFlagWithoutValue_ReturnsNull()
    {
        var options = Program.ParseArgs(["MyApp.sln", "--mode"]);

        Assert.That(options, Is.Null);
    }

    [TestCase("--help")]
    [TestCase("-h")]
    public void ParseArgs_HelpFlag_ReturnsNull(string helpFlag)
    {
        var options = Program.ParseArgs([helpFlag]);

        Assert.That(options, Is.Null);
    }

    [Test]
    public void ParseArgs_NoPositionalArgs_ReturnsNull()
    {
        var options = Program.ParseArgs(["--slim", "--exclude-tests"]);

        Assert.That(options, Is.Null);
    }

    [Test]
    public void ParseArgs_AllFlagsTogether_SetsEverything()
    {
        var options = Program.ParseArgs(["MyApp.sln", "out.md", "--slim", "--exclude-tests", "--scope", "src", "--exclude-dir", "wwwroot", "--type", "csharp", "--mode", "webapi"]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(options!.InputPath, Is.EqualTo("MyApp.sln"));
            Assert.That(options.CustomOutputPath, Is.EqualTo("out.md"));
            Assert.That(options.Slim, Is.True);
            Assert.That(options.ExcludeTests, Is.True);
            Assert.That(options.ScopeDir, Is.EqualTo("src"));
            Assert.That(options.ExcludeDirs, Is.EquivalentTo(["wwwroot"]));
            Assert.That(options.TypeArg, Is.EqualTo("csharp"));
            Assert.That(options.ModeArg, Is.EqualTo("webapi"));
        }
    }

	[Test]
	public void ParseArgs_FindReadme_SetsSearchReadme()
	{
		var options = Program.ParseArgs(["MyApp.sln", "--find-readme"]);

		Assert.That(options!.SearchReadme, Is.True);
	}

	[Test]
	public void ParseArgs_WithoutFindReadme_LeavesSearchReadmeOff()
	{
		var options = Program.ParseArgs(["MyApp.sln"]);

		Assert.That(options!.SearchReadme, Is.False);
	}
}