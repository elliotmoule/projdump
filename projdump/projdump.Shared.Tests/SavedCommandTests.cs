namespace projdump.Shared.Tests;

[TestFixture]
public class SavedCommandTests
{
	static SavedCommand MakeCommand() => new(
		new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
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

	[Test]
	public void HasSameOptions_IgnoresTimestampAndDisplayNames()
	{
		var original = MakeCommand();
		var laterRun = original with
		{
			SavedAt = DateTimeOffset.Now,
			SolutionName = "Renamed",
			ProjectName = null,
		};

		Assert.That(original.HasSameOptions(laterRun), Is.True);
	}

	[Test]
	public void HasSameOptions_IgnoresPathCasing()
	{
		var original = MakeCommand();
		var differentCasing = original with { InputPath = "myapp.SLN", ScopeDir = "SRC" };

		Assert.That(original.HasSameOptions(differentCasing), Is.True);
	}

	[Test]
	public void HasSameOptions_False_WhenASwitchDiffers()
	{
		var original = MakeCommand();

		Assert.That(original.HasSameOptions(original with { Slim = false }), Is.False);
		Assert.That(original.HasSameOptions(original with { ExcludeTests = false }), Is.False);
		Assert.That(original.HasSameOptions(original with { ModeArg = null }), Is.False);
	}

	[Test]
	public void HasSameOptions_False_WhenOutputPathDiffers()
	{
		var original = MakeCommand();

		Assert.That(original.HasSameOptions(original with { CustomOutputPath = null }), Is.False);
	}

	[Test]
	public void HasSameOptions_False_WhenExcludeDirsDiffer()
	{
		var original = MakeCommand();

		Assert.That(original.HasSameOptions(original with { ExcludeDirs = ["wwwroot"] }), Is.False);
	}

	[Test]
	public void HasSameOptions_True_WhenExcludeDirsMatchInAnyOrder()
	{
		var original = MakeCommand();

		Assert.That(original.HasSameOptions(original with { ExcludeDirs = ["docs", "wwwroot"] }), Is.True);
	}
}