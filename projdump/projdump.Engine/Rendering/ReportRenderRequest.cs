namespace projdump.Engine.Rendering;

// What MarkdownReportRenderer needs; kept separate from ProjectAnalysis so the renderer stays type-agnostic.
public sealed class ReportRenderRequest
{
    public required FileInfo InputFileInfo { get; init; }
    public required DirectoryInfo RootDir { get; init; }
    public required bool IsSolution { get; init; }
    public required string Extension { get; init; }
    public required bool Slim { get; init; }
    public required bool ExcludeTests { get; init; }
	public bool SearchForReadme { get; init; }
	public string? ScopeDir { get; init; }
    public IReadOnlyList<string> ExcludeDirs { get; init; } = [];
    public required List<FileInfo> AllFiles { get; init; }
    public required List<FileInfo> CodeFiles { get; init; }
    public required List<FileInfo> ConfigFiles { get; init; }
    public required List<FileInfo> ReadmeFiles { get; init; }
    public required List<FileInfo> ProjFiles { get; init; }
}