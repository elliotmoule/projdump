using projdump.Engine.Core;

namespace projdump.Engine.Analyzers.ReactNative;

sealed class ReactNativeExclusionFilter : IFileExclusionFilter
{
	static readonly HashSet<string> ExcludedFileNames = new(StringComparer.OrdinalIgnoreCase)
	{
		"package-lock.json",
		"yarn.lock",
		"pnpm-lock.yaml",
		"Podfile.lock",
		"Gemfile.lock",
		"gradlew",
		"gradlew.bat",
		"local.properties",

        // Expo template boilerplate - ships with create-expo-app, describes nothing about the project.
        "reset-project.js",

        // Compiler output people redirect into the repo root.
        ".tsc-output.txt",
	};

	// Packager output ships as a single generated bundle - never hand-written.
	static readonly string[] ExcludedFileSuffixes = [".bundle", ".jsbundle", ".keystore"];

	static readonly string[] ExcludedPathSegments =
	[
		$"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}",
		$"{Path.DirectorySeparatorChar}coverage{Path.DirectorySeparatorChar}",

        // Expo caches and generated native projects.
        $"{Path.DirectorySeparatorChar}.expo{Path.DirectorySeparatorChar}",
		$"{Path.DirectorySeparatorChar}.expo-shared{Path.DirectorySeparatorChar}",

        // Icon Composer source bundle - raw SVG/PNG plus a generated manifest.
        $".icon{Path.DirectorySeparatorChar}",

        // Android build output and Gradle tooling; hand-written Gradle scripts sit outside these.
        $"{Path.DirectorySeparatorChar}android{Path.DirectorySeparatorChar}build{Path.DirectorySeparatorChar}",
		$"{Path.DirectorySeparatorChar}android{Path.DirectorySeparatorChar}app{Path.DirectorySeparatorChar}build{Path.DirectorySeparatorChar}",
		$"{Path.DirectorySeparatorChar}.gradle{Path.DirectorySeparatorChar}",
		$"{Path.DirectorySeparatorChar}gradle{Path.DirectorySeparatorChar}wrapper{Path.DirectorySeparatorChar}",

        // iOS dependency and build output; .xcodeproj/.xcworkspace are directories, not files.
        $"{Path.DirectorySeparatorChar}ios{Path.DirectorySeparatorChar}Pods{Path.DirectorySeparatorChar}",
		$"{Path.DirectorySeparatorChar}ios{Path.DirectorySeparatorChar}build{Path.DirectorySeparatorChar}",
		$".xcodeproj{Path.DirectorySeparatorChar}",
		$".xcworkspace{Path.DirectorySeparatorChar}",
		$"{Path.DirectorySeparatorChar}.bundle{Path.DirectorySeparatorChar}",
	];

	/// <summary>
	/// Determines whether a file is React Native build output, a dependency, or tooling noise.
	/// </summary>
	/// <param name="f">The candidate file gathered from the project tree.</param>
	/// <returns>True when the file should be left out of the report.</returns>
	public bool IsExcluded(FileInfo f) =>
		ExcludedFileNames.Contains(f.Name) ||
		ExcludedFileSuffixes.Any(suffix => f.Name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) ||
		ExcludedPathSegments.Any(seg => f.FullName.Contains(seg));
}