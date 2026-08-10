using projdump.Engine.Core;
using projdump.Engine.Core.Filters;

namespace projdump.Engine.Analyzers.CSharp;

public sealed class CSharpAnalyzer : IProjectAnalyzer
{
    // Generic cross-stack rules plus this stack's own.
    static readonly IFileExclusionFilter ExclusionFilter = new CompositeExclusionFilter(
        new VcsAndToolingExclusionFilter(),
        new MinifiedAssetExclusionFilter(),
        new CSharpExclusionFilter());

    static readonly ITestFileDetector TestFileDetector = new CompositeTestFileDetector(
        new GenericTestPathDetector(),
        new CSharpTestFileDetector());

    public string TypeKey => "csharp";

    public IReadOnlyCollection<string> SupportedModes => ["default", "webapi"];

    public bool CanHandle(string inputPath)
    {
        string extension = Path.GetExtension(inputPath).ToLower();
        return extension is ".sln" or ".slnx" or ".csproj";
    }

    public ProjectAnalysis Analyze(string inputPath, ProjectAnalysisOptions options)
    {
        string extension = Path.GetExtension(inputPath).ToLower();
        bool isValidExtension = extension == ".sln" || extension == ".slnx" || extension == ".csproj";

        if (!File.Exists(inputPath) || !isValidExtension)
            throw new ProjectAnalysisException($"'{inputPath}' is not a valid or existing .sln, .slnx, or .csproj file.");

        FileInfo inputFileInfo = new(inputPath);
        DirectoryInfo? rootDir = inputFileInfo.Directory ?? throw new ProjectAnalysisException($"Could not resolve a parent directory for '{inputPath}'.");

        // Apply --scope: restrict file discovery to a subdirectory
        if (options.ScopeDir != null)
        {
            string scopedPath = Path.GetFullPath(Path.Combine(rootDir.FullName, options.ScopeDir));
            if (!Directory.Exists(scopedPath))
                throw new ProjectAnalysisException($"--scope directory '{options.ScopeDir}' does not exist under '{rootDir.FullName}'.");
            rootDir = new DirectoryInfo(scopedPath);
        }

        bool isSolution = extension == ".sln" || extension == ".slnx";

        IFileExclusionFilter effectiveExclusionFilter = options.ExcludeDirs.Count > 0
            ? new CompositeExclusionFilter(ExclusionFilter, new UserDirExclusionFilter(options.ExcludeDirs))
            : ExclusionFilter;

        // Gather all files
        var allFileInfos = rootDir.GetFiles("*.*", SearchOption.AllDirectories)
            .Where(f =>
                !effectiveExclusionFilter.IsExcluded(f) &&
                !(options.ExcludeTests && TestFileDetector.IsTestFile(f))
            )
            .OrderBy(f => f.DirectoryName)
            .ThenBy(f => f.Name)
            .ToList();

        var allFiles = allFileInfos
            .Select(f => new FileEntry { File = f, Role = CSharpFileClassifier.AssignRole(f, TestFileDetector) })
            .ToList();

        var codeFiles = allFileInfos
            .Where(CSharpFileClassifier.IsCodeFile)
            .OrderBy(CSharpFileClassifier.CodeFilePriority)
            .ThenBy(f => f.DirectoryName)
            .ThenBy(f => f.Name)
            .Select(f => new FileEntry { File = f, Role = CSharpFileClassifier.AssignRole(f, TestFileDetector) })
            .ToList();

        var configFiles = allFileInfos
            .Where(CSharpFileClassifier.IsConfigFile)
            .Select(f => new FileEntry { File = f, Role = CSharpFileClassifier.AssignRole(f, TestFileDetector) })
            .ToList();

        var readmeFiles = allFileInfos
            .Where(f => f.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
            .Select(f => new FileEntry { File = f, Role = CSharpFileClassifier.AssignRole(f, TestFileDetector) })
            .ToList();

        var projFileInfos = isSolution
            ? allFileInfos.Where(f => f.Extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase)).ToList()
            : [inputFileInfo];

        var projFiles = projFileInfos
            .Select(f => new FileEntry { File = f, Role = CSharpFileClassifier.AssignRole(f, TestFileDetector) })
            .ToList();

        return new ProjectAnalysis
        {
            InputFileInfo = inputFileInfo,
            RootDir = rootDir,
            IsSolution = isSolution,
            Extension = extension,
            ProjectName = Path.GetFileNameWithoutExtension(inputFileInfo.Name),
            AllFiles = allFiles,
            CodeFiles = codeFiles,
            ConfigFiles = configFiles,
            ReadmeFiles = readmeFiles,
            ProjFiles = projFiles,
        };
    }
}