namespace projdump.Engine.Rendering;

public sealed record MarkdownTableColumn(string Header, MarkdownColumnAlignment Alignment = MarkdownColumnAlignment.Left);