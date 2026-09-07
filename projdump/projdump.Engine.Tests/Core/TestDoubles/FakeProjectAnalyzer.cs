using projdump.Engine.Core;

namespace projdump.Engine.Tests.Core.TestDoubles;

sealed class FakeProjectAnalyzer(string typeKey, Func<string, bool> canHandle, params string[] supportedModes) : IProjectAnalyzer
{
    public string TypeKey => typeKey;
	public string DisplayName => "Fake";
	public IReadOnlyCollection<string> SupportedModes => supportedModes;

    public bool CanHandle(string inputPath) => canHandle(inputPath);

    public ProjectAnalysis Analyze(string inputPath, ProjectAnalysisOptions options) =>
        throw new NotSupportedException("Not needed for registry tests.");
}