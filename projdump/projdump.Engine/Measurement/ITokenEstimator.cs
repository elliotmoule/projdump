namespace projdump.Engine.Measurement;

// Swappable so a real tokenizer (e.g. Microsoft.ML.Tokenizers) can replace the heuristic later.
public interface ITokenEstimator
{
	/// <summary>
	/// Estimates how many tokens a piece of text costs an LLM.
	/// </summary>
	/// <param name="text">The text to measure.</param>
	/// <returns>The estimated token count; zero for empty text.</returns>
	int EstimateTokens(string text);

	/// <summary>
	/// Short label describing how estimates are produced, shown beside counts in output.
	/// </summary>
	string MethodDescription { get; }
}