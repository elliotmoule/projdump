namespace projdump.Engine.Rendering;

// What MarkdownReportRenderer needs; kept separate from ProjectAnalysis so the renderer stays type-agnostic.
public sealed class ReportRenderRequest
{
	public required string ProjectName { get; init; }
	public required FileInfo InputFileInfo { get; init; }
	public required DirectoryInfo RootDir { get; init; }
	public required bool IsSolution { get; init; }
	public required string Extension { get; init; }
	public required bool Slim { get; init; }
	public required bool ExcludeTests { get; init; }
	public bool SearchForReadme { get; init; }
	public bool IncludeStats { get; init; }
	public string? ScopeDir { get; init; }
	public IReadOnlyList<string> ExcludeDirs { get; init; } = [];
	public required IReadOnlyList<FileInfo> AllFiles { get; init; }
	public required IReadOnlyList<FileInfo> CodeFiles { get; init; }
	public required IReadOnlyList<FileInfo> ConfigFiles { get; init; }
	public required IReadOnlyList<FileInfo> ScriptFiles { get; init; }
	public required IReadOnlyList<FileInfo> SqlFiles { get; init; }
	public required IReadOnlyList<FileInfo> ReadmeFiles { get; init; }
	public required IReadOnlyList<FileInfo> ProjFiles { get; init; }

	/// <summary>
	/// Converts a file's full path to one relative to the report root.
	/// </summary>
	/// <param name="file">The file to locate.</param>
	/// <returns>The relative path, starting with ".." when the file sits outside the root.</returns>
	public string GetRelativePath(FileInfo file) => Path.GetRelativePath(RootDir.FullName, file.FullName);
}