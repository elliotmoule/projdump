namespace projdump.Engine.Core;

// Shell and automation scripts look the same in every ecosystem, so this lives in Core
// rather than being duplicated per analyzer.
static class ScriptFileClassifier
{
	static readonly HashSet<string> ScriptExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".ps1", ".psm1", ".psd1",
		".bat", ".cmd",
		".sh", ".bash", ".zsh",
	};

	// Folders that conventionally hold build, deploy, and tooling scripts.
	static readonly HashSet<string> ScriptFolderNames = new(StringComparer.OrdinalIgnoreCase)
	{
		"scripts", "script", "build", "tools", "deploy", "ci", ".github",
	};

	/// <summary>
	/// Determines whether a file is a shell or automation script.
	/// </summary>
	/// <param name="f">The file being classified.</param>
	/// <returns>True when the file's extension is a known script type.</returns>
	public static bool IsScriptFile(FileInfo f) => ScriptExtensions.Contains(f.Extension);

	/// <summary>
	/// Ranks a script for ordering within the Scripts section, lowest first.
	/// </summary>
	/// <param name="f">The script file being ranked.</param>
	/// <param name="rootDir">The analysis root, used to identify root-level scripts.</param>
	/// <returns>0 for root-level scripts, 1 for scripts in a conventional script folder, 2 otherwise.</returns>
	public static int ScriptFilePriority(FileInfo f, DirectoryInfo rootDir)
	{
		if (IsDirectlyInRoot(f, rootDir))
			return 0;

		if (IsInScriptFolder(f))
			return 1;

		return 2;
	}

	static bool IsDirectlyInRoot(FileInfo f, DirectoryInfo rootDir) =>
		f.DirectoryName != null &&
		Path.TrimEndingDirectorySeparator(f.DirectoryName)
			.Equals(Path.TrimEndingDirectorySeparator(rootDir.FullName), StringComparison.OrdinalIgnoreCase);

	static bool IsInScriptFolder(FileInfo f) =>
		f.DirectoryName != null &&
		f.DirectoryName
			.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
			.Any(ScriptFolderNames.Contains);
}