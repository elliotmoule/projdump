using projdump.Engine.Rendering.Statistics;

namespace projdump.Engine.Rendering.Preamble;

sealed class FlagsNoteBuilder : IReportPreambleBuilder
{
	/// <summary>
	/// Builds a one-line note listing the flags the report was generated with.
	/// </summary>
	/// <param name="request">The files and flags being rendered.</param>
	/// <param name="statistics">Unused; part of the preamble contract.</param>
	/// <returns>The Flags note, or null when no flags are active.</returns>
	public ReportSection? Build(ReportRenderRequest request, ReportStatistics statistics)
	{
		var activeFlags = ListActiveFlags(request);
		if (activeFlags.Count == 0)
			return null;

		string note = $"> **Flags:** {string.Join(", ", activeFlags)}{Environment.NewLine}{Environment.NewLine}";
		return new ReportSection(ReportSectionNames.Flags, note, []);
	}

	static List<string> ListActiveFlags(ReportRenderRequest request)
	{
		var activeFlags = new List<string>();
		if (request.Slim) activeFlags.Add("`--slim`");
		if (request.ExcludeTests) activeFlags.Add("`--exclude-tests`");
		if (request.SearchForReadme) activeFlags.Add("`--find-readme`");
		if (request.IncludeStats) activeFlags.Add("`--include-stats`");
		if (request.ScopeDir != null) activeFlags.Add($"`--scope {request.ScopeDir}`");
		foreach (var dir in request.ExcludeDirs) activeFlags.Add($"`--exclude-dir {dir}`");
		return activeFlags;
	}
}