namespace projdump.Engine.Rendering;

// IntroMarkdown holds the heading plus any non-file content, such as the summary table.
public sealed record ReportSection(string Name, string IntroMarkdown, IReadOnlyList<ReportFileBlock> Files)
{
	/// <summary>
	/// Joins the intro and file blocks into the section's final markdown.
	/// </summary>
	/// <returns>The section exactly as it appears in the report.</returns>
	public string ToMarkdown() => string.Concat(Files.Select(file => file.Markdown).Prepend(IntroMarkdown));

	/// <summary>
	/// Creates a section whose intro opens with a level-two heading named after the section.
	/// </summary>
	/// <param name="name">The section name, used as the heading text.</param>
	/// <param name="body">Markdown written under the heading, before any file blocks.</param>
	/// <param name="files">The section's file blocks, in render order.</param>
	/// <returns>The assembled section.</returns>
	public static ReportSection WithHeading(string name, string body = "", IReadOnlyList<ReportFileBlock>? files = null) =>
		new(name, $"## {name}{Environment.NewLine}{Environment.NewLine}{body}", files ?? []);
}