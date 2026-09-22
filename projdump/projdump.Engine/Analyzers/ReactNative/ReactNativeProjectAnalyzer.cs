using System.Text.Json;
using projdump.Engine.Core;
using projdump.Engine.Core.Filters;

namespace projdump.Engine.Analyzers.ReactNative;

public sealed class ReactNativeProjectAnalyzer : IProjectAnalyzer
{
	static readonly IFileExclusionFilter ExclusionFilter = new CompositeExclusionFilter(
		new VcsAndToolingExclusionFilter(),
		new MinifiedAssetExclusionFilter(),
		new ReactNativeExclusionFilter());

	static readonly ITestFileDetector TestFileDetector = new CompositeTestFileDetector(
		new GenericTestPathDetector(),
		new ReactNativeTestFileDetector());

	public string TypeKey => "react-native";

	public string DisplayName => "React Native";

	// WebAPI mode is C#-only for now.
	public IReadOnlyCollection<string> SupportedModes => ["default"];

	public bool CanHandle(string inputPath)
	{
		string? packageJsonPath = ResolvePackageJsonPath(inputPath);
		return packageJsonPath != null && File.Exists(packageJsonPath) && DeclaresReactNativeDependency(packageJsonPath);
	}

	public ProjectAnalysis Analyze(string inputPath, ProjectAnalysisOptions options)
	{
		string? packageJsonPath = ResolvePackageJsonPath(inputPath);
		if (packageJsonPath == null || !File.Exists(packageJsonPath))
			throw new ProjectAnalysisException($"'{inputPath}' is not a directory containing a package.json, or a package.json file itself.");

		if (!DeclaresReactNativeDependency(packageJsonPath))
			throw new ProjectAnalysisException($"'{packageJsonPath}' does not declare a 'react-native' dependency. Pass --type react-native to force this analyzer if that's expected.");

		FileInfo inputFileInfo = new(packageJsonPath);
		DirectoryInfo? rootDir = inputFileInfo.Directory ?? throw new ProjectAnalysisException($"Could not resolve a parent directory for '{packageJsonPath}'.");

		// Computed before --scope reassigns rootDir, so scoping never affects the name.
		string projectName = ResolveProjectName(packageJsonPath, rootDir);

		// Also captured before --scope moves rootDir - entry-point detection is anchored
		// to the real project root, since index.ts elsewhere is a barrel file.
		DirectoryInfo projectRoot = rootDir;
		var classifier = new ReactNativeFileClassifier(projectRoot, ResolveRoutingRoot(packageJsonPath, projectRoot));

		// Apply --scope: restrict file discovery to a subdirectory
		if (options.ScopeDir != null)
		{
			string scopedPath = Path.GetFullPath(Path.Combine(rootDir.FullName, options.ScopeDir));
			if (!Directory.Exists(scopedPath))
				throw new ProjectAnalysisException($"--scope directory '{options.ScopeDir}' does not exist under '{rootDir.FullName}'.");
			rootDir = new DirectoryInfo(scopedPath);
		}

		// Gather all files
		IFileExclusionFilter effectiveExclusionFilter = options.ExcludeDirs.Count > 0
			? new CompositeExclusionFilter(ExclusionFilter, new UserDirExclusionFilter(options.ExcludeDirs))
			: ExclusionFilter;

		var allFileInfos = rootDir.GetFiles("*.*", SearchOption.AllDirectories)
			.Where(f =>
				!effectiveExclusionFilter.IsExcluded(f) &&
				!(options.ExcludeTests && TestFileDetector.IsTestFile(f))
			)
			.OrderBy(f => f.DirectoryName)
			.ThenBy(f => f.Name)
			.ToList();

		var allFiles = allFileInfos
			.Select(f => new FileEntry { File = f, Role = classifier.AssignRole(f, TestFileDetector) })
			.ToList();

		var codeFiles = allFileInfos
			.Where(ReactNativeFileClassifier.IsCodeFile)
			.OrderBy(classifier.CodeFilePriority)
			.ThenBy(f => f.DirectoryName)
			.ThenBy(f => f.Name)
			.Select(f => new FileEntry { File = f, Role = classifier.AssignRole(f, TestFileDetector) })
			.ToList();

		var configFiles = allFileInfos
			.Where(ReactNativeFileClassifier.IsConfigFile)
			.Select(f => new FileEntry { File = f, Role = classifier.AssignRole(f, TestFileDetector) })
			.ToList();

		var scriptFiles = allFileInfos
			.Where(ScriptFileClassifier.IsScriptFile)
			.OrderBy(f => ScriptFileClassifier.ScriptFilePriority(f, rootDir))
			.ThenBy(f => f.DirectoryName)
			.ThenBy(f => f.Name)
			.Select(f => new FileEntry { File = f, Role = classifier.AssignRole(f, TestFileDetector) })
			.ToList();

		var sqlFiles = allFileInfos
			.Where(SqlFileClassifier.IsSqlFile)
			.OrderBy(f => SqlFileClassifier.SqlFilePriority(f, rootDir))
			.ThenBy(f => f.DirectoryName)
			.ThenBy(f => f.Name)
			.Select(f => new FileEntry { File = f, Role = classifier.AssignRole(f, TestFileDetector) })
			.ToList();

		var readmeFiles = allFileInfos
			.Where(f => f.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
			.Select(f => new FileEntry { File = f, Role = classifier.AssignRole(f, TestFileDetector) })
			.ToList();

		AncestorReadmeLocator.AddNearestReadme(readmeFiles, inputFileInfo.Directory, options.SearchForReadme);

		// No solution concept - package.json stands in as the single "project" file, same as Vue.
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
			ScriptFiles = scriptFiles,
			SqlFiles = sqlFiles,
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

	static readonly string[] RoutingRootCandidates = ["app", Path.Combine("src", "app")];

	// Expo Router replaces the root App.tsx with a file-based route tree, so the
	// routing folder is where the app shell and navigation actually live.
	static DirectoryInfo? ResolveRoutingRoot(string packageJsonPath, DirectoryInfo projectRoot)
	{
		if (!DeclaresDependency(packageJsonPath, "expo-router"))
			return null;

		return RoutingRootCandidates
			.Select(candidate => new DirectoryInfo(Path.Combine(projectRoot.FullName, candidate)))
			.FirstOrDefault(dir => dir.Exists);
	}

	static bool DeclaresReactNativeDependency(string packageJsonPath) =>
		DeclaresDependency(packageJsonPath, "react-native");

	static bool DeclaresDependency(string packageJsonPath, string packageName)
	{
		try
		{
			using var stream = File.OpenRead(packageJsonPath);
			using var doc = JsonDocument.Parse(stream);
			var root = doc.RootElement;
			return HasDependency(root, "dependencies", packageName) || HasDependency(root, "devDependencies", packageName);
		}
		catch (JsonException)
		{
			return false;
		}
	}

	// Exact name only - "react-native-reanimated" and friends are libraries a plain
	// React web app could also pull in, so they don't count.
	static bool HasDependency(JsonElement root, string section, string packageName) =>
		root.TryGetProperty(section, out var deps) &&
		deps.ValueKind == JsonValueKind.Object &&
		deps.EnumerateObject().Any(p => p.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase));
}