using projdump.Engine.Rendering;
using projdump.Engine.Rendering.Statistics;

// Console counterpart to the report's Token Statistics section, used by --stats.
static class StatisticsConsoleWriter
{
	const string ContentTotalLabel = "Content total";

	/// <summary>
	/// Prints the report total, per-section counts and the largest files.
	/// </summary>
	/// <param name="projectName">The project the report was rendered for.</param>
	/// <param name="report">The rendered report carrying the statistics.</param>
	public static void Write(string projectName, RenderedReport report)
	{
		ReportStatistics statistics = report.Statistics;

		Console.ForegroundColor = ConsoleColor.Green;
		Console.WriteLine($"Token statistics for {projectName} ({statistics.EstimationMethod})");
		Console.ResetColor();
		Console.WriteLine($"  Whole report: ~{report.EstimatedTokens:N0}");
		Console.WriteLine();

		WriteSectionTable(statistics);
		Console.WriteLine();
		WriteLargestFiles(statistics);
	}

	static void WriteLargestFiles(ReportStatistics statistics)
	{
		var largestFiles = statistics.GetLargestFiles();
		if (largestFiles.Count == 0)
			return;

		Console.WriteLine($"  Largest {ReportStatistics.LargestFileCount} files:");
		int pathWidth = largestFiles.Max(file => file.RelativePath.Length);
		foreach (var file in largestFiles)
			Console.WriteLine($"  {file.RelativePath.PadRight(pathWidth)}  {file.Tokens,10:N0}  {file.SectionName}");
	}

	static void WriteSectionTable(ReportStatistics statistics)
	{
		int nameWidth = statistics.Sections
			.Select(section => section.SectionName.Length)
			.Append(ContentTotalLabel.Length)
			.Max();

		Console.WriteLine($"  {"Section".PadRight(nameWidth)}  {"Files",6}  {"Tokens",10}");
		foreach (var section in statistics.Sections)
		{
			// Summary and Structure describe files without rendering any, so a count would mislead.
			string fileCount = section.FileCount > 0 ? section.FileCount.ToString("N0") : "-";
			Console.WriteLine($"  {section.SectionName.PadRight(nameWidth)}  {fileCount,6}  {section.Tokens,10:N0}");
		}
		Console.WriteLine($"  {ContentTotalLabel.PadRight(nameWidth)}  {statistics.Files.Count,6:N0}  {statistics.ContentTokens,10:N0}");
	}
}