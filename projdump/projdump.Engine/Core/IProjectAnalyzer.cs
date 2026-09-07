namespace projdump.Engine.Core;

public interface IProjectAnalyzer
{
	string TypeKey { get; }                              // e.g. "csharp", "vue"

	// Human-readable label for prompts and help text; TypeKey is what the user types.
	string DisplayName { get; }

	IReadOnlyCollection<string> SupportedModes { get; }   // which report modes this type supports

	bool CanHandle(string inputPath);                     // "could this input plausibly be my type?"

	// Validates the input, gathers and classifies files. Throws ProjectAnalysisException for bad input.
	ProjectAnalysis Analyze(string inputPath, ProjectAnalysisOptions options);
}