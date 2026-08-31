using projdump.Engine.Core;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Core;

[TestFixture]
public class AncestorReadmeLocatorTests
{
	// TempProjectDirectory lives under the system temp folder, so an unbounded walk could
	// reach real directories and find a stray README. Marking the temp root as a repository
	// root keeps every test deterministic - and exercises the .git stop while it's there.
	static void MarkAsRepositoryRoot(TempProjectDirectory temp) =>
		Directory.CreateDirectory(temp.GetFullPath(".git"));

	[Test]
	public void Find_ReturnsReadme_FromTheStartingDirectory()
	{
		using var temp = new TempProjectDirectory();
		MarkAsRepositoryRoot(temp);
		temp.WriteFile(Path.Combine("src", "MyApp", "README.md"), "# project readme");

		var readme = AncestorReadmeLocator.Find(new DirectoryInfo(temp.GetFullPath(Path.Combine("src", "MyApp"))), searchAncestors: true);

		Assert.That(readme, Is.Not.Null);
		Assert.That(readme!.Name, Is.EqualTo("README.md"));
	}

	[Test]
	public void Find_PrefersMarkdown_WhenBothExistInTheSameFolder()
	{
		using var temp = new TempProjectDirectory();
		MarkAsRepositoryRoot(temp);
		temp.WriteFile("README.txt", "text");
		temp.WriteFile("README.md", "markdown");

		var readme = AncestorReadmeLocator.Find(temp.RootDirectoryInfo, searchAncestors: true);

		Assert.That(readme!.Name, Is.EqualTo("README.md"));
	}

	[Test]
	public void Find_ReturnsTextReadme_WhenNoMarkdownExists()
	{
		using var temp = new TempProjectDirectory();
		MarkAsRepositoryRoot(temp);
		temp.WriteFile("README.txt", "text");

		var readme = AncestorReadmeLocator.Find(temp.RootDirectoryInfo, searchAncestors: true);

		Assert.That(readme!.Name, Is.EqualTo("README.txt"));
	}

	[TestCase("readme.md")]
	[TestCase("ReadMe.MD")]
	[TestCase("README.MD")]
	public void Find_MatchesTheFileNameCaseInsensitively(string fileName)
	{
		using var temp = new TempProjectDirectory();
		MarkAsRepositoryRoot(temp);
		temp.WriteFile(fileName, "# readme");

		var readme = AncestorReadmeLocator.Find(temp.RootDirectoryInfo, searchAncestors: true);

		Assert.That(readme, Is.Not.Null);
	}

	[Test]
	public void Find_WalksUpwards_WhenTheStartingDirectoryHasNone()
	{
		using var temp = new TempProjectDirectory();
		MarkAsRepositoryRoot(temp);
		temp.WriteFile("README.md", "# repo readme");
		temp.WriteFile(Path.Combine("src", "MyApp", "MyApp.csproj"), "<Project />");

		var readme = AncestorReadmeLocator.Find(new DirectoryInfo(temp.GetFullPath(Path.Combine("src", "MyApp"))), searchAncestors: true);

		Assert.That(readme!.FullName, Is.EqualTo(temp.GetFullPath("README.md")));
	}

	[Test]
	public void Find_ReturnsTheNearestReadme_WhenSeveralAncestorsHaveOne()
	{
		using var temp = new TempProjectDirectory();
		MarkAsRepositoryRoot(temp);
		temp.WriteFile("README.md", "# repo readme");
		temp.WriteFile(Path.Combine("src", "README.md"), "# src readme");

		var readme = AncestorReadmeLocator.Find(new DirectoryInfo(temp.GetFullPath(Path.Combine("src", "MyApp"))), searchAncestors: true);

		Assert.That(readme!.FullName, Is.EqualTo(temp.GetFullPath(Path.Combine("src", "README.md"))));
	}

	[Test]
	public void Find_ChecksTheRepositoryRootItself_BeforeStopping()
	{
		using var temp = new TempProjectDirectory();
		MarkAsRepositoryRoot(temp);
		temp.WriteFile("README.md", "# repo readme");

		// The .git folder and the README share a directory - the stop must not pre-empt the check.
		var readme = AncestorReadmeLocator.Find(new DirectoryInfo(temp.GetFullPath("src")), searchAncestors: true);

		Assert.That(readme!.FullName, Is.EqualTo(temp.GetFullPath("README.md")));
	}

	[Test]
	public void Find_StopsAtTheRepositoryRoot_AndIgnoresReadmesAbove()
	{
		using var temp = new TempProjectDirectory();
		// README sits above the repo root; .git sits below it. The walk must not reach the README.
		temp.WriteFile("README.md", "# outside the repo");
		Directory.CreateDirectory(temp.GetFullPath(Path.Combine("repo", ".git")));
		Directory.CreateDirectory(temp.GetFullPath(Path.Combine("repo", "src")));

		var readme = AncestorReadmeLocator.Find(new DirectoryInfo(temp.GetFullPath(Path.Combine("repo", "src"))), searchAncestors: true);

		Assert.That(readme, Is.Null);
	}

	[Test]
	public void Find_TreatsAGitFileAsARepositoryRoot()
	{
		using var temp = new TempProjectDirectory();
		temp.WriteFile("README.md", "# outside the repo");
		// Worktrees and submodules write .git as a file rather than a directory.
		temp.WriteFile(Path.Combine("repo", ".git"), "gitdir: ../.git/worktrees/repo");
		Directory.CreateDirectory(temp.GetFullPath(Path.Combine("repo", "src")));

		var readme = AncestorReadmeLocator.Find(new DirectoryInfo(temp.GetFullPath(Path.Combine("repo", "src"))), searchAncestors: true);

		Assert.That(readme, Is.Null);
	}

	[Test]
	public void Find_StopsAfterMaxSearchDepth()
	{
		using var temp = new TempProjectDirectory();
		temp.WriteFile("README.md", "# too far up");

		// One level deeper than the walk reaches, so the depth limit ends it before the README.
		string[] levels = Enumerable.Range(1, AncestorReadmeLocator.MaxSearchDepth + 1)
			.Select(level => $"level{level}")
			.ToArray();
		string deepPath = temp.GetFullPath(Path.Combine(levels));
		Directory.CreateDirectory(deepPath);

		var readme = AncestorReadmeLocator.Find(new DirectoryInfo(deepPath), searchAncestors: true);

		Assert.That(readme, Is.Null);
	}

	[TestCase(true)]
	[TestCase(false)]
	public void Find_ReturnsNull_WhenStartingDirectoryIsNull(bool searchAncestors)
	{
		Assert.That(AncestorReadmeLocator.Find(null, searchAncestors), Is.Null);
	}

	[Test]
	public void AddNearestReadme_InsertsAheadOfOtherDocumentation()
	{
		using var temp = new TempProjectDirectory();
		MarkAsRepositoryRoot(temp);
		temp.WriteFile("README.md", "# repo readme");
		string notesPath = temp.WriteFile(Path.Combine("src", "MyApp", "docs", "notes.md"), "# notes");

		var readmeFiles = new List<FileEntry> { new() { File = new FileInfo(notesPath), Role = FileRole.Doc } };
		AncestorReadmeLocator.AddNearestReadme(readmeFiles, new DirectoryInfo(temp.GetFullPath(Path.Combine("src", "MyApp"))), searchAncestors: true);

		Assert.That(readmeFiles.Select(entry => entry.File.Name), Is.EqualTo(new[] { "README.md", "notes.md" }));
	}

	[Test]
	public void AddNearestReadme_DoesNotDuplicate_WhenAlreadyGathered()
	{
		using var temp = new TempProjectDirectory();
		MarkAsRepositoryRoot(temp);
		string readmePath = temp.WriteFile("README.md", "# repo readme");

		var readmeFiles = new List<FileEntry> { new() { File = new FileInfo(readmePath), Role = FileRole.Doc } };
		AncestorReadmeLocator.AddNearestReadme(readmeFiles, temp.RootDirectoryInfo, searchAncestors: true);

		Assert.That(readmeFiles, Has.Count.EqualTo(1));
	}

	[Test]
	public void AddNearestReadme_LeavesTheListUntouched_WhenNoReadmeIsFound()
	{
		using var temp = new TempProjectDirectory();
		MarkAsRepositoryRoot(temp);

		var readmeFiles = new List<FileEntry>();
		AncestorReadmeLocator.AddNearestReadme(readmeFiles, temp.RootDirectoryInfo, searchAncestors: true);

		Assert.That(readmeFiles, Is.Empty);
	}

	[Test]
	public void Find_WithoutTheSwitch_StillReturnsAnAdjacentReadme()
	{
		using var temp = new TempProjectDirectory();
		MarkAsRepositoryRoot(temp);
		temp.WriteFile(Path.Combine("src", "MyApp", "README.md"), "# project readme");

		var readme = AncestorReadmeLocator.Find(
			new DirectoryInfo(temp.GetFullPath(Path.Combine("src", "MyApp"))),
			searchAncestors: false);

		Assert.That(readme, Is.Not.Null);
	}

	[Test]
	public void Find_WithoutTheSwitch_DoesNotClimbToAnAncestorReadme()
	{
		using var temp = new TempProjectDirectory();
		MarkAsRepositoryRoot(temp);
		temp.WriteFile("README.md", "# repo readme");
		Directory.CreateDirectory(temp.GetFullPath(Path.Combine("src", "MyApp")));

		var readme = AncestorReadmeLocator.Find(
			new DirectoryInfo(temp.GetFullPath(Path.Combine("src", "MyApp"))),
			searchAncestors: false);

		Assert.That(readme, Is.Null);
	}

	[Test]
	public void Find_WithTheSwitch_DoesNotClimbAboveAProjectAtTheRepositoryRoot()
	{
		using var temp = new TempProjectDirectory();
		temp.WriteFile("README.md", "# outside the repo");
		Directory.CreateDirectory(temp.GetFullPath(Path.Combine("repo", ".git")));

		// The project sits at the repo root itself, so the walk must not start.
		var readme = AncestorReadmeLocator.Find(
			new DirectoryInfo(temp.GetFullPath("repo")),
			searchAncestors: true);

		Assert.That(readme, Is.Null);
	}
}