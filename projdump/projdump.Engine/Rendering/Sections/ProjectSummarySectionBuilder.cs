namespace projdump.Engine.Rendering.Sections;

sealed class ProjectSummarySectionBuilder : IReportSectionBuilder
{
	/// <summary>
	/// Builds a table of file counts per extension, most common first.
	/// </summary>
	/// <param name="request">The files and flags being rendered.</param>
	/// <returns>The Project Summary section.</returns>
	public ReportSection? Build(ReportRenderRequest request)
	{
		var table = new MarkdownTable(new MarkdownTableColumn("File Extension"), new MarkdownTableColumn("Count"));

		var extensionCounts = request.AllFiles
			.GroupBy(file => file.Extension.ToLower())
			.Select(group => (Extension: string.IsNullOrEmpty(group.Key) ? "No Ext" : group.Key, Count: group.Count()))
			.OrderByDescending(entry => entry.Count);

		foreach (var (extension, count) in extensionCounts)
			table.AddRow(extension, count.ToString());

		return ReportSection.WithHeading(ReportSectionNames.ProjectSummary, table.ToMarkdown() + Environment.NewLine);
	}
}