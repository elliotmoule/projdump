using System.Text;
using projdump.Engine.Measurement;
using projdump.Engine.Rendering.Preamble;
using projdump.Engine.Rendering.Sections;
using projdump.Engine.Rendering.Statistics;

namespace projdump.Engine.Rendering;

// Built through ReportRendererFactory; the constructor is internal because the builder contracts are.
public sealed class MarkdownReportRenderer : IReportRenderer
{
	readonly IReadOnlyList<IReportSectionBuilder> _contentBuilders;
	readonly IReadOnlyList<IReportPreambleBuilder> _preambleBuilders;
	readonly ReportStatisticsCalculator _statisticsCalculator;
	readonly ITokenEstimator _tokenEstimator;

	internal MarkdownReportRenderer(
		ITokenEstimator tokenEstimator,
		ReportStatisticsCalculator statisticsCalculator,
		IReadOnlyList<IReportSectionBuilder> contentBuilders,
		IReadOnlyList<IReportPreambleBuilder> preambleBuilders)
	{
		_tokenEstimator = tokenEstimator;
		_statisticsCalculator = statisticsCalculator;
		_contentBuilders = contentBuilders;
		_preambleBuilders = preambleBuilders;
	}

	/// <summary>
	/// Runs each builder in order, keeping only the sections that have something to show.
	/// </summary>
	/// <typeparam name="TBuilder">The builder contract.</typeparam>
	/// <param name="builders">Builders in report order.</param>
	/// <param name="build">Invokes one builder.</param>
	/// <returns>The non-null sections, in report order.</returns>
	static List<ReportSection> BuildSections<TBuilder>(IEnumerable<TBuilder> builders, Func<TBuilder, ReportSection?> build) =>
		[.. builders.Select(build).OfType<ReportSection>()];

	/// <summary>
	/// Builds the header line showing the report's estimated token count.
	/// </summary>
	/// <param name="estimatedTokens">The count to display.</param>
	/// <returns>The line followed by a blank line.</returns>
	string FormatTokenLine(int estimatedTokens) =>
		$"> **Estimated tokens:** ~{estimatedTokens:N0}  _({_tokenEstimator.MethodDescription}, treat as a rough guide)_{Environment.NewLine}{Environment.NewLine}";

	static string JoinMarkdown(IEnumerable<ReportSection> sections) =>
		string.Concat(sections.Select(section => section.ToMarkdown()));

	/// <summary>
	/// Builds the full report: content first, then statistics, then the preamble and header.
	/// </summary>
	/// <param name="request">The files and flags to render.</param>
	/// <returns>The report, its estimated token count, statistics, and its sections.</returns>
	public RenderedReport Render(ReportRenderRequest request)
	{
		var contentSections = BuildSections(_contentBuilders, builder => builder.Build(request));
		ReportStatistics statistics = _statisticsCalculator.Calculate(contentSections);
		var preambleSections = BuildSections(_preambleBuilders, builder => builder.Build(request, statistics));

		string title = RenderTitle(request);
		string preamble = JoinMarkdown(preambleSections);
		string body = JoinMarkdown(contentSections);

		// Header is assembled last rather than patched with Replace, which also rewrote matching text inside dumped files.
		// The token line's own cost is added after measuring everything else.
		int tokensWithoutTokenLine = _tokenEstimator.EstimateTokens(title) + _tokenEstimator.EstimateTokens(preamble) + _tokenEstimator.EstimateTokens(body);
		int estimatedTokens = tokensWithoutTokenLine + _tokenEstimator.EstimateTokens(FormatTokenLine(tokensWithoutTokenLine));

		string markdown = string.Concat(title, FormatTokenLine(estimatedTokens), preamble, body);
		return new RenderedReport(markdown, estimatedTokens, statistics, preambleSections, contentSections);
	}

	/// <summary>
	/// Builds the report title, plus the slim-mode note when relevant.
	/// </summary>
	/// <param name="request">The render request.</param>
	/// <returns>The title block, ending with a blank line.</returns>
	static string RenderTitle(ReportRenderRequest request)
	{
		StringBuilder sb = new();

		string modeLabel = request.Slim ? " (slim)" : "";
		sb.AppendLine($"# {request.ProjectName} - {(request.IsSolution ? "App Solution" : "App Project")}{modeLabel}");
		sb.AppendLine();

		if (request.Slim)
		{
			sb.AppendLine("> **Slim mode:** file contents are omitted. Each entry shows the file name, path, and size.");
			sb.AppendLine();
		}

		return sb.ToString();
	}
}