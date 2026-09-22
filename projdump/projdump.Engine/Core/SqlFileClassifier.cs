namespace projdump.Engine.Core;

// Shared across stacks: a .sql file means the same thing in a C# solution as it does
// in a Vue or React Native repo, so the rules live here rather than per-analyzer.
static class SqlFileClassifier
{
	static readonly HashSet<string> SqlExtensions = new(StringComparer.OrdinalIgnoreCase) { ".sql" };

	// Folders teams conventionally use to hold schema, procedures, and seed data.
	static readonly string[] ConventionalSqlFolders =
	[
		"sql",
		"db",
		"database",
		"schema",
		"queries",
		"procedures",
		"storedprocedures",
		"seed",
		"seeds",
	];

	/// <summary>
	/// Determines whether a file is a SQL script.
	/// </summary>
	/// <param name="f">The file being classified.</param>
	/// <returns>True when the extension identifies the file as SQL.</returns>
	public static bool IsSqlFile(FileInfo f) => SqlExtensions.Contains(f.Extension);

	/// <summary>
	/// Orders SQL files by how likely they are to describe the database as a whole.
	/// </summary>
	/// <param name="f">The file being ordered.</param>
	/// <param name="rootDir">The project or solution root the file was gathered under.</param>
	/// <returns>Lower values sort earlier; 0 for root-level, 1 for conventional folders, 2 otherwise.</returns>
	public static int SqlFilePriority(FileInfo f, DirectoryInfo rootDir)
	{
		if (IsDirectlyInRoot(f, rootDir))
			return 0;

		if (IsInConventionalSqlFolder(f))
			return 1;

		return 2;
	}

	static bool IsDirectlyInRoot(FileInfo f, DirectoryInfo rootDir) =>
		f.DirectoryName != null &&
		f.DirectoryName.TrimEnd(Path.DirectorySeparatorChar)
			.Equals(rootDir.FullName.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);

	static bool IsInConventionalSqlFolder(FileInfo f)
	{
		if (f.DirectoryName == null)
			return false;

		var segments = f.DirectoryName.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
		return segments.Any(segment => ConventionalSqlFolders.Contains(segment, StringComparer.OrdinalIgnoreCase));
	}
}