namespace projdump.Engine.Rendering.Statistics;

// Content sections only; the report header and the stats block itself are excluded.
public sealed record ReportStatistics(
	string EstimationMethod,
	IReadOnlyList<SectionTokenCount> Sections,
	IReadOnlyList<FileTokenCount> Files)
{
	public const int LargestFileCount = 10;

	public int ContentTokens => Sections.Sum(section => section.Tokens);

	/// <summary>
	/// Lists the most expensive files, largest first, with ties ordered by path so reruns stay stable.
	/// </summary>
	/// <returns>Up to <see cref="LargestFileCount"/> files.</returns>
	public IReadOnlyList<FileTokenCount> GetLargestFiles() =>
	[
		.. Files
			.OrderByDescending(file => file.Tokens)
			.ThenBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
			.Take(LargestFileCount)
	];
}