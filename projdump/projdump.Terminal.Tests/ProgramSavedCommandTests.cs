using projdump.Shared;
using projdump.Terminal.Tests.TestSupport;

namespace projdump.Terminal.Tests;

[TestFixture]
public class ProgramSavedCommandTests
{
    static Program.RunOptions MakeOptions(string inputPath = "MyApp.sln") =>
        new(inputPath, null, false, false, null, null, null, []);

    static SavedCommand MakeSavedCommand(DateTimeOffset savedAt, string inputPath) =>
        new(savedAt, inputPath, null, false, false, null, null, null, []);

    [Test]
    public void TryUseSavedCommand_ReturnsNull_WhenHistoryFileDoesNotExist()
    {
        using var temp = new TempProjectDirectory();
        using var scope = new CommandHistoryFilePathOverrideScope(temp.GetFullPath("history.json"));
        // No console input queued - if this tried to prompt, there'd be nothing to answer.

        var result = Program.TryUseSavedCommand();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void TryUseSavedCommand_UserDeclines_ReturnsNull()
    {
        using var temp = new TempProjectDirectory();
        string historyPath = temp.GetFullPath("history.json");
        using var scope = new CommandHistoryFilePathOverrideScope(historyPath);
        CommandHistoryStore.Save(MakeSavedCommand(DateTimeOffset.Now, "MyApp.sln"), historyPath);

        using var input = new ConsoleInputScope("n");
        var result = Program.TryUseSavedCommand();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void TryUseSavedCommand_UserAccepts_ListsMostRecentFirst()
    {
        using var temp = new TempProjectDirectory();
        string historyPath = temp.GetFullPath("history.json");
        using var scope = new CommandHistoryFilePathOverrideScope(historyPath);

        CommandHistoryStore.Save(MakeSavedCommand(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), "Older.sln"), historyPath);
        CommandHistoryStore.Save(MakeSavedCommand(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero), "Newer.sln"), historyPath);

        using var input = new ConsoleInputScope("y", "1"); // pick the first listed item
        using var console = new ConsoleCapture();

        var result = Program.TryUseSavedCommand();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.InputPath, Is.EqualTo("Newer.sln")); // most recent should be option 1

        int newerIndex = console.Output.IndexOf("Newer.sln", StringComparison.Ordinal);
        int olderIndex = console.Output.IndexOf("Older.sln", StringComparison.Ordinal);
        Assert.That(newerIndex, Is.GreaterThan(-1));
        Assert.That(olderIndex, Is.GreaterThan(-1));
        Assert.That(newerIndex, Is.LessThan(olderIndex));
    }

    [Test]
    public void TryUseSavedCommand_InvalidSelection_ReturnsNull()
    {
        using var temp = new TempProjectDirectory();
        string historyPath = temp.GetFullPath("history.json");
        using var scope = new CommandHistoryFilePathOverrideScope(historyPath);
        CommandHistoryStore.Save(MakeSavedCommand(DateTimeOffset.Now, "MyApp.sln"), historyPath);

        using var input = new ConsoleInputScope("y", "99");
        var result = Program.TryUseSavedCommand();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void TryUseSavedCommand_BlankSelection_ReturnsNull()
    {
        using var temp = new TempProjectDirectory();
        string historyPath = temp.GetFullPath("history.json");
        using var scope = new CommandHistoryFilePathOverrideScope(historyPath);
        CommandHistoryStore.Save(MakeSavedCommand(DateTimeOffset.Now, "MyApp.sln"), historyPath);

        using var input = new ConsoleInputScope("y", "");
        var result = Program.TryUseSavedCommand();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void OfferToSaveCommand_UserDeclines_DoesNotWriteFile()
    {
        using var temp = new TempProjectDirectory();
        string historyPath = temp.GetFullPath("history.json");
        using var scope = new CommandHistoryFilePathOverrideScope(historyPath);

        using var input = new ConsoleInputScope("n");
        Program.OfferToSaveCommand(MakeOptions());

        Assert.That(File.Exists(historyPath), Is.False);
    }

    [Test]
    public void OfferToSaveCommand_UserAccepts_WritesCommandToHistory()
    {
        using var temp = new TempProjectDirectory();
        string historyPath = temp.GetFullPath("history.json");
        using var scope = new CommandHistoryFilePathOverrideScope(historyPath);

        using var input = new ConsoleInputScope("y");
        Program.OfferToSaveCommand(MakeOptions("MyApp.sln"));

        var history = CommandHistoryStore.Load(historyPath);
        Assert.That(history.Select(c => c.InputPath), Is.EquivalentTo(new[] { "MyApp.sln" }));
    }

    [Test]
    public void ToSavedCommand_ThenToRunOptions_PreservesAllFields()
    {
        var original = new Program.RunOptions("MyApp.sln", "out.md", true, true, "src", "csharp", "webapi", ["wwwroot", "docs"]);

        var saved = Program.ToSavedCommand(original);
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
        }
    }
}