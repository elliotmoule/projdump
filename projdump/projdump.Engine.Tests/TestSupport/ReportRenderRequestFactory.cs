using projdump.Engine.Rendering;

namespace projdump.Engine.Tests.TestSupport;

static class ReportRenderRequestFactory
{
	public static ReportRenderRequest Create(
		DirectoryInfo rootDir,
		IReadOnlyList<FileInfo>? allFiles = null,
		IReadOnlyList<FileInfo>? codeFiles = null,
		IReadOnlyList<FileInfo>? configFiles = null,
		IReadOnlyList<FileInfo>? readmeFiles = null,
		IReadOnlyList<FileInfo>? projFiles = null,
		FileInfo? inputFileInfo = null,
		bool isSolution = false,
		bool slim = false,
		bool excludeTests = false,
		bool searchForReadme = false,
		bool includeStats = false,
		string? scopeDir = null,
		IReadOnlyList<string>? excludeDirs = null) => new()
		{
			ProjectName = "MyApp",
			InputFileInfo = inputFileInfo ?? new FileInfo(Path.Combine(rootDir.FullName, "MyApp.csproj")),
			RootDir = rootDir,
			IsSolution = isSolution,
			Extension = inputFileInfo?.Extension.ToLower() ?? ".csproj",
			Slim = slim,
			ExcludeTests = excludeTests,
			SearchForReadme = searchForReadme,
			IncludeStats = includeStats,
			ScopeDir = scopeDir,
			ExcludeDirs = excludeDirs ?? [],
			AllFiles = allFiles ?? [],
			CodeFiles = codeFiles ?? [],
			ConfigFiles = configFiles ?? [],
			ScriptFiles = [],
			SqlFiles = [],
			ReadmeFiles = readmeFiles ?? [],
			ProjFiles = projFiles ?? [],
		};
}