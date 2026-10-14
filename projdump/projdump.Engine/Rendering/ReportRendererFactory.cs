using projdump.Engine.Measurement;
using projdump.Engine.Rendering.Preamble;
using projdump.Engine.Rendering.Sections;
using projdump.Engine.Rendering.Statistics;

namespace projdump.Engine.Rendering;

// The single place that knows which sections ship and in what order, mirroring AnalyzerCatalog.
public static class ReportRendererFactory
{
	/// <summary>
	/// Builds the standard markdown renderer using the default token estimator.
	/// </summary>
	/// <returns>A renderer producing every section in report order.</returns>
	public static IReportRenderer Create() => Create(new CharacterHeuristicTokenEstimator());

	/// <summary>
	/// Builds the standard markdown renderer around a given token estimator.
	/// </summary>
	/// <param name="tokenEstimator">Measures the header total and section statistics.</param>
	/// <returns>A renderer producing every section in report order.</returns>
	public static IReportRenderer Create(ITokenEstimator tokenEstimator)
	{
		var blockFactory = new ReportFileBlockFactory();

		// Order is report order. Scripts and Database sit between config and code: how the project is
		// built, run, and shaped in the database is usually more valuable context than any single source file.
		IReportSectionBuilder[] contentBuilders =
		[
			new ProjectSummarySectionBuilder(),
			new ProjectStructureSectionBuilder(),
			new DocumentationSectionBuilder(blockFactory),
			new SolutionConfigurationSectionBuilder(blockFactory),
			new FileListSectionBuilder(blockFactory, ReportSectionNames.ProjectDependencies, request => request.ProjFiles, renderWhenEmpty: true),
			new FileListSectionBuilder(blockFactory, ReportSectionNames.Configuration, request => request.ConfigFiles),
			new FileListSectionBuilder(blockFactory, ReportSectionNames.Scripts, request => request.ScriptFiles),
			new FileListSectionBuilder(blockFactory, ReportSectionNames.Database, request => request.SqlFiles),
			new FileListSectionBuilder(blockFactory, ReportSectionNames.AppCode, request => request.CodeFiles, renderWhenEmpty: true),
		];

		IReportPreambleBuilder[] preambleBuilders = [new FlagsNoteBuilder(), new StatisticsSectionBuilder()];

		return new MarkdownReportRenderer(tokenEstimator, new ReportStatisticsCalculator(tokenEstimator), contentBuilders, preambleBuilders);
	}
}