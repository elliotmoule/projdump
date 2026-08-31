namespace projdump.Shared;

public sealed record SavedCommand(
	DateTimeOffset SavedAt,
	string InputPath,
	string? CustomOutputPath,
	bool Slim,
	bool ExcludeTests,
	string? ScopeDir,
	string? TypeArg,
	string? ModeArg,
	IReadOnlyList<string> ExcludeDirs,
	string? SolutionName = null,
	string? ProjectName = null)
{
	/// <summary>
	/// Determines whether another command runs with identical options, ignoring when it was
	/// last used and the display names resolved at run time.
	/// </summary>
	/// <param name="other">The command to compare against.</param>
	/// <returns>True when every option matches exactly.</returns>
	public bool HasSameOptions(SavedCommand other) =>
		PathsMatch(InputPath, other.InputPath) &&
		PathsMatch(CustomOutputPath, other.CustomOutputPath) &&
		Slim == other.Slim &&
		ExcludeTests == other.ExcludeTests &&
		PathsMatch(ScopeDir, other.ScopeDir) &&
		PathsMatch(TypeArg, other.TypeArg) &&
		PathsMatch(ModeArg, other.ModeArg) &&
		ExcludeDirs.OrderBy(dir => dir, StringComparer.OrdinalIgnoreCase)
			.SequenceEqual(other.ExcludeDirs.OrderBy(dir => dir, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);

	static bool PathsMatch(string? left, string? right) =>
		string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}