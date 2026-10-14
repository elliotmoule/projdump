using projdump.Engine.Measurement;

namespace projdump.Engine.Rendering.Statistics;

sealed class ReportStatisticsCalculator(ITokenEstimator tokenEstimator)
{
	/// <summary>
	/// Measures each content section and every file block inside it.
	/// </summary>
	/// <param name="sections">The content sections, in render order.</param>
	/// <returns>Per-section and per-file token counts.</returns>
	public ReportStatistics Calculate(IReadOnlyList<ReportSection> sections)
	{
		var sectionCounts = sections.Select(MeasureSection).ToList();
		var fileCounts = sections
			.SelectMany(section => section.Files.Select(file => MeasureFile(section.Name, file)))
			.ToList();

		return new ReportStatistics(tokenEstimator.MethodDescription, sectionCounts, fileCounts);
	}

	FileTokenCount MeasureFile(string sectionName, ReportFileBlock file) =>
		new(file.RelativePath, sectionName, tokenEstimator.EstimateTokens(file.Markdown));

	SectionTokenCount MeasureSection(ReportSection section) =>
		new(section.Name, section.Files.Count, tokenEstimator.EstimateTokens(section.ToMarkdown()));
}