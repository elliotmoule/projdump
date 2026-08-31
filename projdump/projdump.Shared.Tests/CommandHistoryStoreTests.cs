using projdump.Shared.Tests.TestSupport;

namespace projdump.Shared.Tests;

[TestFixture]
public class CommandHistoryStoreTests
{
	static SavedCommand MakeCommand(string inputPath = "MyApp.sln", bool slim = false) => new(
		DateTimeOffset.Now,
		inputPath,
		null,
		slim,
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
	public void RecordUse_CreatesTheDirectory_WhenItDoesNotExist()
	{
		using var temp = new TempJsonFile();

		CommandHistoryStore.RecordUse(MakeCommand(), temp.FilePath);

		Assert.That(File.Exists(temp.FilePath), Is.True);
	}

	[Test]
	public void RecordUseThenLoad_RoundTripsCorrectly()
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
			["wwwroot", "docs"],
			"MyApp",
			"MyApp.Api");

		CommandHistoryStore.RecordUse(command, temp.FilePath);
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
			Assert.That(result.SolutionName, Is.EqualTo(command.SolutionName));
			Assert.That(result.ProjectName, Is.EqualTo(command.ProjectName));
		}
	}

	[Test]
	public void Load_ReturnsLegacyEntries_WithNullDisplayNames()
	{
		using var temp = new TempJsonFile();
		Directory.CreateDirectory(Path.GetDirectoryName(temp.FilePath)!);
		// Hand-written JSON without the display name properties, as older versions wrote it.
		File.WriteAllText(temp.FilePath, """
            [
              {
                "SavedAt": "2026-01-15T09:30:00+00:00",
                "InputPath": "Legacy.sln",
                "CustomOutputPath": null,
                "Slim": false,
                "ExcludeTests": false,
                "ScopeDir": null,
                "TypeArg": null,
                "ModeArg": null,
                "ExcludeDirs": []
              }
            ]
            """);

		var history = CommandHistoryStore.Load(temp.FilePath);

		Assert.That(history, Has.Count.EqualTo(1));
		using (Assert.EnterMultipleScope())
		{
			Assert.That(history[0].InputPath, Is.EqualTo("Legacy.sln"));
			Assert.That(history[0].SolutionName, Is.Null);
			Assert.That(history[0].ProjectName, Is.Null);
		}
	}

	[Test]
	public void RecordUse_KeepsEveryDistinctCommand_MostRecentFirst()
	{
		using var temp = new TempJsonFile();

		CommandHistoryStore.RecordUse(MakeCommand("First.sln"), temp.FilePath);
		CommandHistoryStore.RecordUse(MakeCommand("Second.sln"), temp.FilePath);
		CommandHistoryStore.RecordUse(MakeCommand("Third.sln"), temp.FilePath);

		var history = CommandHistoryStore.Load(temp.FilePath);

		Assert.That(history.Select(c => c.InputPath), Is.EqualTo(new[] { "Third.sln", "Second.sln", "First.sln" }));
	}

	[Test]
	public void RecordUse_MatchingCommand_MovesItToTheTopWithoutDuplicating()
	{
		using var temp = new TempJsonFile();

		CommandHistoryStore.RecordUse(MakeCommand("First.sln"), temp.FilePath);
		CommandHistoryStore.RecordUse(MakeCommand("Second.sln"), temp.FilePath);
		CommandHistoryStore.RecordUse(MakeCommand("First.sln"), temp.FilePath);

		var history = CommandHistoryStore.Load(temp.FilePath);

		Assert.That(history.Select(c => c.InputPath), Is.EqualTo(new[] { "First.sln", "Second.sln" }));
	}

	[Test]
	public void RecordUse_DifferingFlags_TreatedAsASeparateCommand()
	{
		using var temp = new TempJsonFile();

		CommandHistoryStore.RecordUse(MakeCommand("MyApp.sln", slim: false), temp.FilePath);
		CommandHistoryStore.RecordUse(MakeCommand("MyApp.sln", slim: true), temp.FilePath);

		var history = CommandHistoryStore.Load(temp.FilePath);

		Assert.That(history, Has.Count.EqualTo(2));
		Assert.That(history[0].Slim, Is.True);
	}

	[Test]
	public void RecordUse_MatchingCommand_RefreshesTheTimestamp()
	{
		using var temp = new TempJsonFile();
		var original = MakeCommand("MyApp.sln") with { SavedAt = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero) };
		var reused = MakeCommand("MyApp.sln") with { SavedAt = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero) };

		CommandHistoryStore.RecordUse(original, temp.FilePath);
		CommandHistoryStore.RecordUse(reused, temp.FilePath);

		var history = CommandHistoryStore.Load(temp.FilePath);

		Assert.That(history, Has.Count.EqualTo(1));
		Assert.That(history[0].SavedAt, Is.EqualTo(reused.SavedAt));
	}

	[Test]
	public void FindMatch_ReturnsStoredCommand_WhenOptionsMatch()
	{
		using var temp = new TempJsonFile();
		CommandHistoryStore.RecordUse(MakeCommand("MyApp.sln"), temp.FilePath);

		var match = CommandHistoryStore.FindMatch(MakeCommand("MyApp.sln"), temp.FilePath);

		Assert.That(match, Is.Not.Null);
		Assert.That(match!.InputPath, Is.EqualTo("MyApp.sln"));
	}

	[Test]
	public void FindMatch_ReturnsNull_WhenFlagsDiffer()
	{
		using var temp = new TempJsonFile();
		CommandHistoryStore.RecordUse(MakeCommand("MyApp.sln", slim: false), temp.FilePath);

		var match = CommandHistoryStore.FindMatch(MakeCommand("MyApp.sln", slim: true), temp.FilePath);

		Assert.That(match, Is.Null);
	}

	[Test]
	public void FindMatch_ReturnsNull_WhenHistoryIsEmpty()
	{
		using var temp = new TempJsonFile();

		var match = CommandHistoryStore.FindMatch(MakeCommand(), temp.FilePath);

		Assert.That(match, Is.Null);
	}

	[Test]
	public void GetDefaultFilePath_ReturnsPathUnderApplicationData()
	{
		string path = CommandHistoryStore.GetDefaultFilePath();

		Assert.That(path, Does.Contain("projdump"));
		Assert.That(path, Does.EndWith("command-history.json"));
	}
}