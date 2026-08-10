namespace projdump.Engine.Core;

// Output of an IProjectAnalyzer, filtered by an IDumpMode; Role never changes section membership under DefaultMode.
public sealed class ProjectAnalysis
{
    public required FileInfo InputFileInfo { get; init; }
    public required DirectoryInfo RootDir { get; init; }
    public required bool IsSolution { get; init; }
    public required string Extension { get; init; }
    public required string ProjectName { get; init; }

    public required IReadOnlyList<FileEntry> AllFiles { get; init; }
    public required IReadOnlyList<FileEntry> CodeFiles { get; init; }
    public required IReadOnlyList<FileEntry> ConfigFiles { get; init; }
    public required IReadOnlyList<FileEntry> ReadmeFiles { get; init; }
    public required IReadOnlyList<FileEntry> ProjFiles { get; init; }

    public ProjectAnalysis WithFiles(
        IReadOnlyList<FileEntry> allFiles,
        IReadOnlyList<FileEntry> codeFiles,
        IReadOnlyList<FileEntry> configFiles,
        IReadOnlyList<FileEntry> readmeFiles,
        IReadOnlyList<FileEntry> projFiles) => new()
        {
            InputFileInfo = InputFileInfo,
            RootDir = RootDir,
            IsSolution = IsSolution,
            Extension = Extension,
            ProjectName = ProjectName,
            AllFiles = allFiles,
            CodeFiles = codeFiles,
            ConfigFiles = configFiles,
            ReadmeFiles = readmeFiles,
            ProjFiles = projFiles,
        };
}