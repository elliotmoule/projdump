using System.Text;
using projdump.Engine.Rendering.Statistics;

namespace projdump.Engine.Rendering.Preamble;

sealed class StatisticsSectionBuilder : IReportPreambleBuilder
{
	/// <summary>
	/// Builds the Token Statistics section: per-section counts and the largest files.
	/// </summary>
	/// <param name="request">The files and flags being rendered.</param>
	/// <param name="statistics">Token counts for the content sections.</param>
	/// <returns>The section, or null unless stats were requested.</returns>
	public ReportSection? Build(ReportRenderRequest request, ReportStatistics statistics)
	{
		if (!request.IncludeStats)
			return null;

		StringBuilder body = new();
		body.AppendLine($"> Estimated by {statistics.EstimationMethod}. Totals cover content sections only, not this block or the header.");
		body.AppendLine();
		body.Append(BuildSectionTable(statistics));
		body.AppendLine();
		body.AppendLine($"**Largest {ReportStatistics.LargestFileCount} files**");
		body.AppendLine();
		body.Append(BuildLargestFilesTable(statistics));
		body.AppendLine();

		return ReportSection.WithHeading(ReportSectionNames.TokenStatistics, body.ToString());
	}

	static string BuildLargestFilesTable(ReportStatistics statistics)
	{
		var table = new MarkdownTable(
			new MarkdownTableColumn("File"),
			new MarkdownTableColumn("Section"),
			new MarkdownTableColumn("Tokens", MarkdownColumnAlignment.Right));

		foreach (var file in statistics.GetLargestFiles())
			table.AddRow($"`{file.RelativePath}`", file.SectionName, file.Tokens.ToString("N0"));

		return table.ToMarkdown();
	}

	static string BuildSectionTable(ReportStatistics statistics)
	{
		var table = new MarkdownTable(
			new MarkdownTableColumn("Section"),
			new MarkdownTableColumn("Files", MarkdownColumnAlignment.Right),
			new MarkdownTableColumn("Tokens", MarkdownColumnAlignment.Right));

		foreach (var section in statistics.Sections)
		{
			// Summary and Structure describe files without rendering any, so a count would mislead.
			string fileCount = section.FileCount > 0 ? section.FileCount.ToString("N0") : "-";
			table.AddRow(section.SectionName, fileCount, section.Tokens.ToString("N0"));
		}

		table.AddRow("**Content total**", $"**{statistics.Files.Count:N0}**", $"**{statistics.ContentTokens:N0}**");
		return table.ToMarkdown();
	}
}