using projdump.Engine.Rendering.Statistics;

namespace projdump.Engine.Rendering.Preamble;

// Runs after content sections are built, so it can report on them; preamble output is excluded from statistics.
interface IReportPreambleBuilder
{
	/// <summary>
	/// Builds a block shown between the token line and the first content section.
	/// </summary>
	/// <param name="request">The files and flags being rendered.</param>
	/// <param name="statistics">Token counts for the content sections.</param>
	/// <returns>The block, or null when it has nothing to show.</returns>
	ReportSection? Build(ReportRenderRequest request, ReportStatistics statistics);
}