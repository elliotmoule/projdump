namespace projdump.Engine.Core;

// Output of an IProjectAnalyzer, filtered by an IDumpMode; Role never changes section membership under DefaultMode.
public sealed class ProjectAnalysis
{
	public required FileInfo InputFileInfo { get; init; }
	public required DirectoryInfo RootDir { get; init; }
	public required bool IsSolution { get; init; }
	public required string Extension { get; init; }
	public required string ProjectName { get; init; }

	public required IReadOnlyList<FileEntry> AllFiles { get; init; }
	public required IReadOnlyList<FileEntry> CodeFiles { get; init; }
	public required IReadOnlyList<FileEntry> ConfigFiles { get; init; }
	public required IReadOnlyList<FileEntry> ScriptFiles { get; init; }
	public required IReadOnlyList<FileEntry> SqlFiles { get; init; }
	public required IReadOnlyList<FileEntry> ReadmeFiles { get; init; }
	public required IReadOnlyList<FileEntry> ProjFiles { get; init; }

	/// <summary>
	/// Produces a copy of the analysis carrying a different set of files, leaving project identity untouched.
	/// </summary>
	/// <param name="allFiles">Every gathered file, used for the summary and structure sections.</param>
	/// <param name="codeFiles">Application source files, in render order.</param>
	/// <param name="configFiles">Configuration files.</param>
	/// <param name="scriptFiles">Shell and automation scripts.</param>
	/// <param name="sqlFiles">SQL scripts.</param>
	/// <param name="readmeFiles">Documentation files.</param>
	/// <param name="projFiles">Project and dependency manifest files.</param>
	/// <returns>A new analysis with the supplied file lists.</returns>
	public ProjectAnalysis WithFiles(
		IReadOnlyList<FileEntry> allFiles,
		IReadOnlyList<FileEntry> codeFiles,
		IReadOnlyList<FileEntry> configFiles,
		IReadOnlyList<FileEntry> scriptFiles,
		IReadOnlyList<FileEntry> sqlFiles,
		IReadOnlyList<FileEntry> readmeFiles,
		IReadOnlyList<FileEntry> projFiles) => new()
		{
			InputFileInfo = InputFileInfo,
			RootDir = RootDir,
			IsSolution = IsSolution,
			Extension = Extension,
			ProjectName = ProjectName,
			AllFiles = allFiles,
			CodeFiles = codeFiles,
			ConfigFiles = configFiles,
			ScriptFiles = scriptFiles,
			SqlFiles = sqlFiles,
			ReadmeFiles = readmeFiles,
			ProjFiles = projFiles,
		};
}