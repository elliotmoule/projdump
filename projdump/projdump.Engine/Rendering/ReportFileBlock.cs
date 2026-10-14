namespace projdump.Engine.Rendering;

// Markdown covers the whole entry (heading, path line, note, content), which is what stats and partitioning measure.
public sealed record ReportFileBlock(string RelativePath, string Markdown);