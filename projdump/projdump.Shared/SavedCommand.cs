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
    IReadOnlyList<string> ExcludeDirs);