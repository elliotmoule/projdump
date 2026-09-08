using projdump.Engine.Core;

namespace projdump.Engine.Modes;

// Exclude-list, not keep-list (most C# API logic falls under FileRole.Other); Test is excluded via --exclude-tests only.
public sealed class WebApiMode : IDumpMode
{
	public string ModeKey => "webapi";

	// Script is deliberately absent: build and deploy scripts describe how the API runs.
	static readonly HashSet<FileRole> ExcludedRoles = [FileRole.Component, FileRole.Style, FileRole.Asset];

	/// <summary>
	/// Drops UI-only files from every collection in the analysis.
	/// </summary>
	/// <param name="analysis">The analysis to filter.</param>
	/// <returns>A copy carrying only files relevant to the API surface.</returns>
	public ProjectAnalysis Apply(ProjectAnalysis analysis)
	{
		var allFiles = Filter(analysis.AllFiles);
		var codeFiles = Filter(analysis.CodeFiles);
		var configFiles = Filter(analysis.ConfigFiles);
		var scriptFiles = Filter(analysis.ScriptFiles);
		var readmeFiles = Filter(analysis.ReadmeFiles);
		var projFiles = Filter(analysis.ProjFiles);

		return analysis.WithFiles(allFiles, codeFiles, configFiles, scriptFiles, readmeFiles, projFiles);
	}

	static List<FileEntry> Filter(IReadOnlyList<FileEntry> files) =>
		[.. files.Where(f => !ExcludedRoles.Contains(f.Role))];
}