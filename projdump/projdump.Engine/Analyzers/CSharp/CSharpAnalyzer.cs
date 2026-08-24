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
        if (Directory.Exists(inputPath))
            return DirectoryContainsSolutionFile(inputPath);

        string extension = Path.GetExtension(inputPath).ToLower();
        return extension is ".sln" or ".slnx" or ".csproj";
    }

    public ProjectAnalysis Analyze(string inputPath, ProjectAnalysisOptions options)
    {
        string resolvedInputPath = Directory.Exists(inputPath)
            ? ResolveSolutionFileInDirectory(inputPath)
            : inputPath;

        string extension = Path.GetExtension(resolvedInputPath).ToLower();
        bool isValidExtension = extension == ".sln" || extension == ".slnx" || extension == ".csproj";

        if (!File.Exists(resolvedInputPath) || !isValidExtension)
            throw new ProjectAnalysisException($"'{resolvedInputPath}' is not a valid or existing .sln, .slnx, or .csproj file.");

        FileInfo inputFileInfo = new(resolvedInputPath);
        DirectoryInfo? rootDir = inputFileInfo.Directory ?? throw new ProjectAnalysisException($"Could not resolve a parent directory for '{resolvedInputPath}'.");

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

    static bool DirectoryContainsSolutionFile(string dirPath) =>
        Directory.GetFiles(dirPath, "*.slnx", SearchOption.TopDirectoryOnly).Length > 0 ||
        Directory.GetFiles(dirPath, "*.sln", SearchOption.TopDirectoryOnly).Length > 0;

    // .slnx is preferred over .sln when both exist. Non-recursive - matches
    // how VueProjectAnalyzer only looks for package.json directly in the
    // given directory, not anywhere deeper in the tree.
    static string ResolveSolutionFileInDirectory(string dirPath)
    {
        var slnxFiles = Directory.GetFiles(dirPath, "*.slnx", SearchOption.TopDirectoryOnly);
        if (slnxFiles.Length == 1) return slnxFiles[0];
        if (slnxFiles.Length > 1)
            throw new ProjectAnalysisException($"Multiple .slnx files found in '{dirPath}'. Point directly at the one you want instead.");

        var slnFiles = Directory.GetFiles(dirPath, "*.sln", SearchOption.TopDirectoryOnly);
        if (slnFiles.Length == 1) return slnFiles[0];
        if (slnFiles.Length > 1)
            throw new ProjectAnalysisException($"Multiple .sln files found in '{dirPath}'. Point directly at the one you want instead.");

        throw new ProjectAnalysisException($"No .sln or .slnx file found in '{dirPath}'.");
    }
}