namespace projdump.Engine.Measurement;

// GPT and Claude tokenizers average roughly four characters per token across mixed code and prose.
public sealed class CharacterHeuristicTokenEstimator : ITokenEstimator
{
	const double CharactersPerToken = 4.0;

	/// <summary>
	/// Estimates tokens as character count divided by four, rounded up.
	/// </summary>
	/// <param name="text">The text to measure.</param>
	/// <returns>The estimated token count; zero for empty text.</returns>
	public int EstimateTokens(string text) => (int)Math.Ceiling(text.Length / CharactersPerToken);

	public string MethodDescription => "character count ÷ 4";
}