using projdump.Shared;
using projdump.Terminal.Tests.TestSupport;

namespace projdump.Terminal.Tests;

[TestFixture]
public class ProgramSavedCommandTests
{
	static Program.RunOptions MakeOptions(string inputPath = "MyApp.sln", bool slim = false) =>
		new(inputPath, null, slim, false, null, null, null, []);

	static SavedCommand MakeSavedCommand(
		string inputPath,
		bool slim = false,
		string? solutionName = null,
		string? projectName = null) =>
		new(DateTimeOffset.Now, inputPath, null, slim, false, null, null, null, [], solutionName, projectName);

	static Program.ExecutionResult Succeeded(string? solutionName = null, string? projectName = null) =>
		new(true, solutionName, projectName);

	[Test]
	public void ShowRecentCommands_ReturnsEnterNewCommand_WhenHistoryFileDoesNotExist()
	{
		using var temp = new TempProjectDirectory();
		using var scope = new CommandHistoryFilePathOverrideScope(temp.GetFullPath("history.json"));
		// No console input queued - an empty history must not prompt at all.

		var result = Program.ShowRecentCommands();

		Assert.That(result.Action, Is.EqualTo(Program.RecentMenuAction.EnterNewCommand));
		Assert.That(result.Options, Is.Null);
	}

	[Test]
	public void ShowRecentCommands_ListsMostRecentFirst_AndReturnsTheSelection()
	{
		using var temp = new TempProjectDirectory();
		string historyPath = temp.GetFullPath("history.json");
		using var scope = new CommandHistoryFilePathOverrideScope(historyPath);

		CommandHistoryStore.RecordUse(MakeSavedCommand("Older.sln", solutionName: "Older"), historyPath);
		CommandHistoryStore.RecordUse(MakeSavedCommand("Newer.sln", solutionName: "Newer"), historyPath);

		using var input = new ConsoleInputScope("1"); // no yes/no gate - the list is shown straight away
		using var console = new ConsoleCapture();

		var result = Program.ShowRecentCommands();

		Assert.That(result.Action, Is.EqualTo(Program.RecentMenuAction.RunSelected));
		Assert.That(result.Options!.InputPath, Is.EqualTo("Newer.sln"));

		int newerIndex = console.Output.IndexOf("Newer", StringComparison.Ordinal);
		int olderIndex = console.Output.IndexOf("Older", StringComparison.Ordinal);
		Assert.That(newerIndex, Is.GreaterThan(-1));
		Assert.That(olderIndex, Is.GreaterThan(-1));
		Assert.That(newerIndex, Is.LessThan(olderIndex));
	}

	[Test]
	public void ShowRecentCommands_RendersNamesAndFlags_WithoutTheInputPath()
	{
		using var temp = new TempProjectDirectory();
		string historyPath = temp.GetFullPath("history.json");
		using var scope = new CommandHistoryFilePathOverrideScope(historyPath);

		var command = new SavedCommand(
			DateTimeOffset.Now,
			Path.Combine("C:", "src", "repos", "MyApp.sln"),
			"out.md",
			true,
			false,
			null,
			null,
			null,
			[],
			"MyApp",
			"MyApp.Api");
		CommandHistoryStore.RecordUse(command, historyPath);

		using var input = new ConsoleInputScope("q");
		using var console = new ConsoleCapture();

		Program.ShowRecentCommands();

		using (Assert.EnterMultipleScope())
		{
			Assert.That(console.Output, Does.Contain("MyApp/MyApp.Api"));
			Assert.That(console.Output, Does.Contain("--slim"));
			Assert.That(console.Output, Does.Contain("out.md"));
			Assert.That(console.Output, Does.Not.Contain("repos"));
		}
	}

	[Test]
	public void ShowRecentCommands_LegacyEntryWithoutNames_FallsBackToTheFileName()
	{
		using var temp = new TempProjectDirectory();
		string historyPath = temp.GetFullPath("history.json");
		using var scope = new CommandHistoryFilePathOverrideScope(historyPath);

		CommandHistoryStore.RecordUse(MakeSavedCommand(Path.Combine("C:", "repos", "Legacy.sln")), historyPath);

		using var input = new ConsoleInputScope("q");
		using var console = new ConsoleCapture();

		Program.ShowRecentCommands();

		Assert.That(console.Output, Does.Contain("Legacy"));
		Assert.That(console.Output, Does.Not.Contain("repos"));
	}

	[TestCase("q")]
	[TestCase("Q")]
	public void ShowRecentCommands_QuitInput_ReturnsQuit(string choice)
	{
		using var temp = new TempProjectDirectory();
		string historyPath = temp.GetFullPath("history.json");
		using var scope = new CommandHistoryFilePathOverrideScope(historyPath);
		CommandHistoryStore.RecordUse(MakeSavedCommand("MyApp.sln"), historyPath);

		using var input = new ConsoleInputScope(choice);
		var result = Program.ShowRecentCommands();

		Assert.That(result.Action, Is.EqualTo(Program.RecentMenuAction.Quit));
	}

	[TestCase("99")]
	[TestCase("0")]
	[TestCase("")]
	[TestCase("nonsense")]
	public void ShowRecentCommands_UnusableSelection_ReturnsEnterNewCommand(string choice)
	{
		using var temp = new TempProjectDirectory();
		string historyPath = temp.GetFullPath("history.json");
		using var scope = new CommandHistoryFilePathOverrideScope(historyPath);
		CommandHistoryStore.RecordUse(MakeSavedCommand("MyApp.sln"), historyPath);

		using var input = new ConsoleInputScope(choice);
		var result = Program.ShowRecentCommands();

		Assert.That(result.Action, Is.EqualTo(Program.RecentMenuAction.EnterNewCommand));
		Assert.That(result.Options, Is.Null);
	}

	[Test]
	public void RecordCommandUse_NewCommand_UserDeclines_DoesNotWriteFile()
	{
		using var temp = new TempProjectDirectory();
		string historyPath = temp.GetFullPath("history.json");
		using var scope = new CommandHistoryFilePathOverrideScope(historyPath);

		using var input = new ConsoleInputScope("n");
		Program.RecordCommandUse(MakeOptions(), Succeeded(), offerToSave: true);

		Assert.That(File.Exists(historyPath), Is.False);
	}

	[Test]
	public void RecordCommandUse_NewCommand_UserAccepts_WritesCommandToHistory()
	{
		using var temp = new TempProjectDirectory();
		string historyPath = temp.GetFullPath("history.json");
		using var scope = new CommandHistoryFilePathOverrideScope(historyPath);

		using var input = new ConsoleInputScope("y");
		Program.RecordCommandUse(MakeOptions("MyApp.sln"), Succeeded("MyApp"), offerToSave: true);

		var history = CommandHistoryStore.Load(historyPath);
		Assert.That(history.Select(c => c.InputPath), Is.EqualTo(new[] { "MyApp.sln" }));
		Assert.That(history[0].SolutionName, Is.EqualTo("MyApp"));
	}

	[Test]
	public void RecordCommandUse_ReusedCommand_PromotesWithoutPrompting()
	{
		using var temp = new TempProjectDirectory();
		string historyPath = temp.GetFullPath("history.json");
		using var scope = new CommandHistoryFilePathOverrideScope(historyPath);

		CommandHistoryStore.RecordUse(MakeSavedCommand("First.sln"), historyPath);
		CommandHistoryStore.RecordUse(MakeSavedCommand("Second.sln"), historyPath);

		// Nothing queued: a prompt would read blank, be treated as "no", and skip the write.
		using var input = new ConsoleInputScope();
		Program.RecordCommandUse(MakeOptions("First.sln"), Succeeded(), offerToSave: false);

		var history = CommandHistoryStore.Load(historyPath);
		Assert.That(history.Select(c => c.InputPath), Is.EqualTo(new[] { "First.sln", "Second.sln" }));
	}

	[Test]
	public void RecordCommandUse_NewCommandMatchingHistory_PromotesWithoutPrompting()
	{
		using var temp = new TempProjectDirectory();
		string historyPath = temp.GetFullPath("history.json");
		using var scope = new CommandHistoryFilePathOverrideScope(historyPath);

		CommandHistoryStore.RecordUse(MakeSavedCommand("First.sln"), historyPath);
		CommandHistoryStore.RecordUse(MakeSavedCommand("Second.sln"), historyPath);

		// offerToSave is true, but the identical entry already in history suppresses the question.
		using var input = new ConsoleInputScope();
		Program.RecordCommandUse(MakeOptions("First.sln"), Succeeded(), offerToSave: true);

		var history = CommandHistoryStore.Load(historyPath);
		Assert.That(history.Select(c => c.InputPath), Is.EqualTo(new[] { "First.sln", "Second.sln" }));
	}

	[Test]
	public void RecordCommandUse_SameInputPathDifferentFlags_StillOffersToSave()
	{
		using var temp = new TempProjectDirectory();
		string historyPath = temp.GetFullPath("history.json");
		using var scope = new CommandHistoryFilePathOverrideScope(historyPath);

		CommandHistoryStore.RecordUse(MakeSavedCommand("MyApp.sln", slim: false), historyPath);

		using var input = new ConsoleInputScope("y");
		Program.RecordCommandUse(MakeOptions("MyApp.sln", slim: true), Succeeded(), offerToSave: true);

		var history = CommandHistoryStore.Load(historyPath);
		Assert.That(history, Has.Count.EqualTo(2));
		Assert.That(history[0].Slim, Is.True);
	}

	[Test]
	public void ToSavedCommand_ThenToRunOptions_PreservesAllFields()
	{
		var original = new Program.RunOptions("MyApp.sln", "out.md", true, true, "src", "csharp", "webapi", ["wwwroot", "docs"]);

		var saved = Program.ToSavedCommand(original, "MyApp", "MyApp.Api");
		var roundTripped = Program.ToRunOptions(saved);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(roundTripped.InputPath, Is.EqualTo(original.InputPath));
			Assert.That(roundTripped.CustomOutputPath, Is.EqualTo(original.CustomOutputPath));
			Assert.That(roundTripped.Slim, Is.EqualTo(original.Slim));
			Assert.That(roundTripped.ExcludeTests, Is.EqualTo(original.ExcludeTests));
			Assert.That(roundTripped.ScopeDir, Is.EqualTo(original.ScopeDir));
			Assert.That(roundTripped.TypeArg, Is.EqualTo(original.TypeArg));
			Assert.That(roundTripped.ModeArg, Is.EqualTo(original.ModeArg));
			Assert.That(roundTripped.ExcludeDirs, Is.EquivalentTo(original.ExcludeDirs));
			Assert.That(saved.SolutionName, Is.EqualTo("MyApp"));
			Assert.That(saved.ProjectName, Is.EqualTo("MyApp.Api"));
		}
	}
}