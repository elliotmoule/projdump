using projdump.Engine.Measurement;

namespace projdump.Engine.Tests.Measurement;

[TestFixture]
public class CharacterHeuristicTokenEstimatorTests
{
	readonly CharacterHeuristicTokenEstimator _estimator = new();

	[Test]
	public void EstimateTokens_EmptyText_ReturnsZero()
	{
		Assert.That(_estimator.EstimateTokens(""), Is.EqualTo(0));
	}

	[Test]
	public void EstimateTokens_LengthDivisibleByFour_ReturnsExactQuotient()
	{
		Assert.That(_estimator.EstimateTokens("12345678"), Is.EqualTo(2));
	}

	[TestCase("a", 1)]
	[TestCase("12345", 2)]
	[TestCase("1234567", 2)]
	public void EstimateTokens_PartialToken_RoundsUp(string text, int expected)
	{
		Assert.That(_estimator.EstimateTokens(text), Is.EqualTo(expected));
	}
}