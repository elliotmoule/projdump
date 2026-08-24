using projdump.Shared.Tests.TestSupport;

namespace projdump.Shared.Tests;

[TestFixture]
public class CommandHistoryStoreTests
{
    static SavedCommand MakeCommand(string inputPath = "MyApp.sln") => new(
        DateTimeOffset.Now,
        inputPath,
        null,
        false,
        false,
        null,
        null,
        null,
        []);

    [Test]
    public void Load_ReturnsEmptyList_WhenFileDoesNotExist()
    {
        using var temp = new TempJsonFile();

        var history = CommandHistoryStore.Load(temp.FilePath);

        Assert.That(history, Is.Empty);
    }

    [Test]
    public void Load_ReturnsEmptyList_WhenFileIsCorrupt()
    {
        using var temp = new TempJsonFile();
        Directory.CreateDirectory(Path.GetDirectoryName(temp.FilePath)!);
        File.WriteAllText(temp.FilePath, "{ not valid json");

        var history = CommandHistoryStore.Load(temp.FilePath);

        Assert.That(history, Is.Empty);
    }

    [Test]
    public void Save_CreatesTheDirectory_WhenItDoesNotExist()
    {
        using var temp = new TempJsonFile();

        CommandHistoryStore.Save(MakeCommand(), temp.FilePath);

        Assert.That(File.Exists(temp.FilePath), Is.True);
    }

    [Test]
    public void SaveThenLoad_RoundTripsCorrectly()
    {
        using var temp = new TempJsonFile();
        var command = new SavedCommand(
            new DateTimeOffset(2026, 1, 15, 9, 30, 0, TimeSpan.Zero),
            "MyApp.sln",
            "out.md",
            true,
            true,
            "src",
            "csharp",
            "webapi",
            ["wwwroot", "docs"]);

        CommandHistoryStore.Save(command, temp.FilePath);
        var loaded = CommandHistoryStore.Load(temp.FilePath);

        Assert.That(loaded, Has.Count.EqualTo(1));
        var result = loaded[0];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.SavedAt, Is.EqualTo(command.SavedAt));
            Assert.That(result.InputPath, Is.EqualTo(command.InputPath));
            Assert.That(result.CustomOutputPath, Is.EqualTo(command.CustomOutputPath));
            Assert.That(result.Slim, Is.EqualTo(command.Slim));
            Assert.That(result.ExcludeTests, Is.EqualTo(command.ExcludeTests));
            Assert.That(result.ScopeDir, Is.EqualTo(command.ScopeDir));
            Assert.That(result.TypeArg, Is.EqualTo(command.TypeArg));
            Assert.That(result.ModeArg, Is.EqualTo(command.ModeArg));
            Assert.That(result.ExcludeDirs, Is.EquivalentTo(command.ExcludeDirs));
        }
    }

    [Test]
    public void Save_AppendsIndefinitely_DoesNotOverwritePreviousEntries()
    {
        using var temp = new TempJsonFile();

        CommandHistoryStore.Save(MakeCommand("First.sln"), temp.FilePath);
        CommandHistoryStore.Save(MakeCommand("Second.sln"), temp.FilePath);
        CommandHistoryStore.Save(MakeCommand("Third.sln"), temp.FilePath);

        var history = CommandHistoryStore.Load(temp.FilePath);

        Assert.That(history.Select(c => c.InputPath), Is.EquivalentTo(new[] { "First.sln", "Second.sln", "Third.sln" }));
    }

    [Test]
    public void GetDefaultFilePath_ReturnsPathUnderApplicationData()
    {
        string path = CommandHistoryStore.GetDefaultFilePath();

        Assert.That(path, Does.Contain("projdump"));
        Assert.That(path, Does.EndWith("command-history.json"));
    }
}