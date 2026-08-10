namespace projdump.Engine.Core;

public sealed class ProjectTypeRegistry
{
    readonly List<IProjectAnalyzer> _analyzers;

    public ProjectTypeRegistry(IEnumerable<IProjectAnalyzer> analyzers)
    {
        _analyzers = [.. analyzers];
    }

    public IProjectAnalyzer Resolve(string inputPath, string? requestedTypeKey)
    {
        if (requestedTypeKey != null)
        {
            var match = _analyzers.FirstOrDefault(a => a.TypeKey.Equals(requestedTypeKey, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                string supported = string.Join(", ", _analyzers.Select(a => a.TypeKey));
                throw new ProjectAnalysisException($"Unknown project type '{requestedTypeKey}'. Supported types: {supported}.");
            }
            return match;
        }

        var detected = _analyzers.FirstOrDefault(a => a.CanHandle(inputPath)) ?? throw new ProjectAnalysisException($"Could not detect a project type for '{inputPath}'. Specify one explicitly with --type.");
        return detected;
    }

    public static void ValidateMode(IProjectAnalyzer analyzer, string modeKey)
    {
        if (!analyzer.SupportedModes.Contains(modeKey, StringComparer.OrdinalIgnoreCase))
        {
            string supported = string.Join(", ", analyzer.SupportedModes);
            throw new ProjectAnalysisException($"Mode '{modeKey}' is not supported for project type '{analyzer.TypeKey}'. Supported modes: {supported}.");
        }
    }
}