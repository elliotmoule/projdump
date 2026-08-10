using System.Text.Json;
using projdump.Engine.Core;
using projdump.Engine.Core.Filters;

namespace projdump.Engine.Analyzers.Vue;

public sealed class VueProjectAnalyzer : IProjectAnalyzer
{
    static readonly IFileExclusionFilter ExclusionFilter = new CompositeExclusionFilter(
        new VcsAndToolingExclusionFilter(),
        new VueExclusionFilter());

    static readonly ITestFileDetector TestFileDetector = new CompositeTestFileDetector(
        new GenericTestPathDetector(),
        new VueTestFileDetector());

    public string TypeKey => "vue";

    // WebAPI mode is C#-only for now.
    public IReadOnlyCollection<string> SupportedModes => ["default"];

    public bool CanHandle(string inputPath)
    {
        string? packageJsonPath = ResolvePackageJsonPath(inputPath);
        return packageJsonPath != null && File.Exists(packageJsonPath) && DeclaresVueDependency(packageJsonPath);
    }

    public ProjectAnalysis Analyze(string inputPath, ProjectAnalysisOptions options)
    {
        string? packageJsonPath = ResolvePackageJsonPath(inputPath);
        if (packageJsonPath == null || !File.Exists(packageJsonPath))
            throw new ProjectAnalysisException($"'{inputPath}' is not a directory containing a package.json, or a package.json file itself.");

        if (!DeclaresVueDependency(packageJsonPath))
            throw new ProjectAnalysisException($"'{packageJsonPath}' does not declare a 'vue' dependency. Pass --type vue to force this analyzer if that's expected.");

        FileInfo inputFileInfo = new(packageJsonPath);
        DirectoryInfo? rootDir = inputFileInfo.Directory ?? throw new ProjectAnalysisException($"Could not resolve a parent directory for '{packageJsonPath}'.");

        // Computed before --scope reassigns rootDir, so scoping never affects the name.
        string projectName = ResolveProjectName(packageJsonPath, rootDir);

        // Apply --scope: restrict file discovery to a subdirectory
        if (options.ScopeDir != null)
        {
            string scopedPath = Path.GetFullPath(Path.Combine(rootDir.FullName, options.ScopeDir));
            if (!Directory.Exists(scopedPath))
                throw new ProjectAnalysisException($"--scope directory '{options.ScopeDir}' does not exist under '{rootDir.FullName}'.");
            rootDir = new DirectoryInfo(scopedPath);
        }

        // Gather all files
        var allFileInfos = rootDir.GetFiles("*.*", SearchOption.AllDirectories)
            .Where(f =>
                !ExclusionFilter.IsExcluded(f) &&
                !(options.ExcludeTests && TestFileDetector.IsTestFile(f))
            )
            .OrderBy(f => f.DirectoryName)
            .ThenBy(f => f.Name)
            .ToList();

        var allFiles = allFileInfos
            .Select(f => new FileEntry { File = f, Role = VueFileClassifier.AssignRole(f, TestFileDetector) })
            .ToList();

        var codeFiles = allFileInfos
            .Where(VueFileClassifier.IsCodeFile)
            .OrderBy(VueFileClassifier.CodeFilePriority)
            .ThenBy(f => f.DirectoryName)
            .ThenBy(f => f.Name)
            .Select(f => new FileEntry { File = f, Role = VueFileClassifier.AssignRole(f, TestFileDetector) })
            .ToList();

        var configFiles = allFileInfos
            .Where(VueFileClassifier.IsConfigFile)
            .Select(f => new FileEntry { File = f, Role = VueFileClassifier.AssignRole(f, TestFileDetector) })
            .ToList();

        var readmeFiles = allFileInfos
            .Where(f => f.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
            .Select(f => new FileEntry { File = f, Role = VueFileClassifier.AssignRole(f, TestFileDetector) })
            .ToList();

        // Vue has no solution concept - always treated like C#'s "project" (non-solution) case.
        var projFiles = new List<FileEntry> { new() { File = inputFileInfo, Role = FileRole.Build } };

        return new ProjectAnalysis
        {
            InputFileInfo = inputFileInfo,
            RootDir = rootDir,
            IsSolution = false,
            Extension = inputFileInfo.Extension,
            ProjectName = projectName,
            AllFiles = allFiles,
            CodeFiles = codeFiles,
            ConfigFiles = configFiles,
            ReadmeFiles = readmeFiles,
            ProjFiles = projFiles,
        };
    }

    static string? ResolvePackageJsonPath(string inputPath)
    {
        if (Directory.Exists(inputPath))
            return Path.Combine(inputPath, "package.json");

        if (File.Exists(inputPath) && Path.GetFileName(inputPath).Equals("package.json", StringComparison.OrdinalIgnoreCase))
            return inputPath;

        return null;
    }

    static string ResolveProjectName(string packageJsonPath, DirectoryInfo rootDir)
    {
        try
        {
            using var stream = File.OpenRead(packageJsonPath);
            using var doc = JsonDocument.Parse(stream);
            if (doc.RootElement.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String)
            {
                string? name = nameProp.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                    return name;
            }
        }
        catch (JsonException)
        {
            // fall through to directory name
        }

        return rootDir.Name;
    }

    static bool DeclaresVueDependency(string packageJsonPath)
    {
        try
        {
            using var stream = File.OpenRead(packageJsonPath);
            using var doc = JsonDocument.Parse(stream);
            var root = doc.RootElement;
            return HasDependency(root, "dependencies") || HasDependency(root, "devDependencies");
        }
        catch (JsonException)
        {
            return false;
        }
    }

    static bool HasDependency(JsonElement root, string section) =>
        root.TryGetProperty(section, out var deps) &&
        deps.ValueKind == JsonValueKind.Object &&
        deps.EnumerateObject().Any(p => p.Name.Equals("vue", StringComparison.OrdinalIgnoreCase));
}