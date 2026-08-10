namespace projdump.Engine.Core;

public interface IProjectAnalyzer
{
    string TypeKey { get; }
    IReadOnlyCollection<string> SupportedModes { get; }

    bool CanHandle(string inputPath);

    // Throws ProjectAnalysisException for bad input.
    ProjectAnalysis Analyze(string inputPath, ProjectAnalysisOptions options);
}