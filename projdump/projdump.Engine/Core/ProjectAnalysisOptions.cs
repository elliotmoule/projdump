namespace projdump.Engine.Core;

public sealed class ProjectAnalysisOptions
{
	public bool ExcludeTests { get; init; }
	public string? ScopeDir { get; init; }
	public IReadOnlyList<string> ExcludeDirs { get; init; } = [];
	public bool SearchForReadme { get; init; }
}