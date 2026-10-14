using projdump.Engine.Rendering.Statistics;

namespace projdump.Engine.Rendering;

// EstimatedTokens covers the whole report; Statistics covers content sections only.
public sealed record RenderedReport(
	string Markdown,
	int EstimatedTokens,
	ReportStatistics Statistics,
	IReadOnlyList<ReportSection> PreambleSections,
	IReadOnlyList<ReportSection> ContentSections);