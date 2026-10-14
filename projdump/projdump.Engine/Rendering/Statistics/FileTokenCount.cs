namespace projdump.Engine.Rendering.Statistics;

// Tokens cover the whole rendered block (heading, path line, fence and content), which is what partitioning will need.
public sealed record FileTokenCount(string RelativePath, string SectionName, int Tokens);