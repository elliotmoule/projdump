namespace projdump.Engine.Core;

// The single source of truth for how far up the tree projdump will walk - shared by the
// readme search and the Terminal's owning-solution lookup so the two can't drift apart.
public static class AncestorReadmeLocator
{
	// Covers the usual repo/src/MyApp.Api layouts without reaching the drive root
	// on deep or network paths.
	public const int MaxSearchDepth = 5;

	// Markdown is listed first so a folder holding both yields README.md.
	static readonly string[] ReadmeFileNames = ["README.md", "README.txt"];

	/// <summary>
	/// Finds a README beside the starting directory, optionally continuing up the tree.
	/// </summary>
	/// <param name="startDir">The directory holding the project or solution file.</param>
	/// <param name="searchAncestors">True to keep climbing when the starting directory has none.</param>
	/// <returns>The closest README found, or null when there is none within reach.</returns>
	public static FileInfo? Find(DirectoryInfo? startDir, bool searchAncestors)
	{
		if (startDir == null)
			return null;

		FileInfo? adjacentReadme = FindInDirectory(startDir);
		if (adjacentReadme != null || !searchAncestors)
			return adjacentReadme;

		// The starting directory is already ruled out, so the walk begins at its parent.
		// It still counts as level 0 for the depth limit.
		if (IsRepositoryRoot(startDir))
			return null;

		DirectoryInfo? currentDir = startDir.Parent;

		for (int level = 1; level <= MaxSearchDepth && currentDir != null; level++)
		{
			FileInfo? readme = FindInDirectory(currentDir);
			if (readme != null)
				return readme;

			// A .git entry marks the repository root - nothing above it belongs to this project.
			// Only helps for repos, so the depth limit still does the work everywhere else.
			if (IsRepositoryRoot(currentDir))
				return null;

			currentDir = currentDir.Parent;
		}

		return null;
	}

	static FileInfo? FindInDirectory(DirectoryInfo dir)
	{
		if (!dir.Exists)
			return null;

		try
		{
			var candidates = dir
				.EnumerateFiles()
				.Where(file => ReadmeFileNames.Contains(file.Name, StringComparer.OrdinalIgnoreCase))
				.ToList();

			return candidates.FirstOrDefault(file => file.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
				   ?? candidates.FirstOrDefault();
		}
		catch (UnauthorizedAccessException)
		{
			// An unreadable ancestor shouldn't abort the walk - skip it and keep climbing.
			return null;
		}
	}

	// Worktrees and submodules use a .git file rather than a directory, so both count.
	static bool IsRepositoryRoot(DirectoryInfo dir)
	{
		string gitPath = Path.Combine(dir.FullName, ".git");
		return Directory.Exists(gitPath) || File.Exists(gitPath);
	}

	/// <summary>
	/// Adds the nearest README to a gathered list when the gathering didn't already find it.
	/// </summary>
	/// <param name="readmeFiles">The documentation files collected from the project tree.</param>
	/// <param name="startDir">The directory holding the project or solution file.</param>
	/// <param name="searchAncestors">True to look above the starting directory when it has none.</param>
	public static void AddNearestReadme(List<FileEntry> readmeFiles, DirectoryInfo? startDir, bool searchAncestors)
	{
		FileInfo? nearestReadme = Find(startDir, searchAncestors);
		if (nearestReadme == null)
			return;

		bool alreadyGathered = readmeFiles.Any(entry =>
			entry.File.FullName.Equals(nearestReadme.FullName, StringComparison.OrdinalIgnoreCase));

		if (!alreadyGathered)
			readmeFiles.Insert(0, new FileEntry { File = nearestReadme, Role = FileRole.Doc });
	}
}