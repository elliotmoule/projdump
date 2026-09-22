using projdump.Engine.Core;

namespace projdump.Engine.Analyzers.ReactNative;

// Instance rather than static: classification varies per project (routing layout,
// entry-point anchoring), so the per-analysis context is held as state.
sealed class ReactNativeFileClassifier
{
	static readonly string[] RootEntryPointNames = ["index.js", "index.ts", "index.tsx", "App.js", "App.jsx", "App.ts", "App.tsx"];

	static readonly HashSet<string> ConfigFileNames = new(StringComparer.OrdinalIgnoreCase)
	{
        // JS tooling
        "metro.config.js",
		"metro.config.ts",
		"babel.config.js",
		"react-native.config.js",
		"jest.config.js",
		"jest.config.ts",
		"tsconfig.json",
		"tailwind.config.js",
		".eslintrc.json",
		".eslintrc.js",
		".prettierrc",
		".env",
		".env.local",
		".env.development",
		".env.production",

        // Expo
        "app.json",
		"app.config.js",
		"app.config.ts",
		"eas.json",

        // Native project configuration - declares permissions, bundle IDs, and native deps.
        "Podfile",
		"Info.plist",
		"AndroidManifest.xml",
		"build.gradle",
		"settings.gradle",
		"gradle.properties",
	};

	static readonly HashSet<string> CodeExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".js", ".jsx", ".ts", ".tsx",
		".css", ".scss", ".sass",

        // Hand-written native source that RN projects genuinely carry.
        ".kt", ".java", ".swift", ".m", ".mm", ".h", ".gradle",
	};

	static readonly HashSet<string> AssetExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg", ".ico", ".bmp", ".avif",
		".ttf", ".otf", ".woff", ".woff2",
		".mp3", ".mp4", ".lottie",
	};

	// Segment matches are anchored on both sides so "components" doesn't also hit "my-components-lib".
	static readonly string[] NavigationAndStateSegments = ["navigation", "navigators", "store", "stores", "contexts", "providers"];
	static readonly string[] ScreenAndComponentSegments = ["screens", "components", "hooks", "views"];
	static readonly string[] TypeSegments = ["types", "models"];

	readonly DirectoryInfo _projectRoot;

	// Null when the project isn't Expo Router; otherwise the folder holding the route tree.
	readonly DirectoryInfo? _routingRoot;

	public ReactNativeFileClassifier(DirectoryInfo projectRoot, DirectoryInfo? routingRoot = null)
	{
		_projectRoot = projectRoot;
		_routingRoot = routingRoot;
	}

	public static bool IsCodeFile(FileInfo f) => CodeExtensions.Contains(f.Extension);

	public static bool IsConfigFile(FileInfo f) => ConfigFileNames.Contains(f.Name);

	// Expo Router has no root App.tsx - the route tree's own _layout is the app shell.
	bool IsRoutingRootLayout(FileInfo f) =>
		_routingRoot != null &&
		Path.GetFileNameWithoutExtension(f.Name).Equals("_layout", StringComparison.OrdinalIgnoreCase) &&
		string.Equals(f.DirectoryName, _routingRoot.FullName, StringComparison.OrdinalIgnoreCase);

	bool IsRouteFile(FileInfo f) =>
		_routingRoot != null &&
		f.FullName.StartsWith(_routingRoot.FullName + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Ranks a code file so the report leads with the files that explain the app's shape.
	/// </summary>
	/// <param name="f">The code file being ordered.</param>
	/// <returns>A sort key from 0 (entry points) to 5 (native platform source).</returns>
	public int CodeFilePriority(FileInfo f)
	{
		if (IsEntryPoint(f) || IsRoutingRootLayout(f))
			return 0;

		// Nested layouts define the navigator shape, so they lead their sibling routes.
		if (IsRouteFile(f))
			return Path.GetFileNameWithoutExtension(f.Name).Equals("_layout", StringComparison.OrdinalIgnoreCase) ? 1 : 2;

		if (ContainsFolder(f, NavigationAndStateSegments))
			return 2;

		if (ContainsFolder(f, TypeSegments) || f.Name.EndsWith("Types.ts", StringComparison.OrdinalIgnoreCase))
			return 3;

		if (ContainsFolder(f, ScreenAndComponentSegments))
			return 4;

		// Native source is rarely the interesting part of an RN codebase, so it sinks below the JS.
		if (IsNativeSource(f))
			return 6;

		return 5;
	}

	/// <summary>
	/// Assigns the semantic role used by report modes to filter files.
	/// </summary>
	/// <param name="f">The file being classified.</param>
	/// <param name="testFileDetector">Detector deciding whether the file belongs to the test suite.</param>
	/// <returns>The role describing what the file contributes to the project.</returns>
	public FileRole AssignRole(FileInfo f, ITestFileDetector testFileDetector)
	{
		if (testFileDetector.IsTestFile(f))
			return FileRole.Test;

		if (f.Name.Equals("package.json", StringComparison.OrdinalIgnoreCase))
			return FileRole.Build;

		if (f.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
			return FileRole.Doc;

		if (ScriptFileClassifier.IsScriptFile(f))
			return FileRole.Script;

		if (SqlFileClassifier.IsSqlFile(f))
			return FileRole.Sql;

		if (IsConfigFile(f))
			return FileRole.Config;

		if (IsEntryPoint(f) || IsRoutingRootLayout(f))
			return FileRole.EntryPoint;

		if (IsRouteFile(f) || ContainsFolder(f, NavigationAndStateSegments))
			return FileRole.ApiSurface;

		if (f.Extension.Equals(".tsx", StringComparison.OrdinalIgnoreCase) ||
			f.Extension.Equals(".jsx", StringComparison.OrdinalIgnoreCase))
			return FileRole.Component;

		if (f.Extension.Equals(".css", StringComparison.OrdinalIgnoreCase) ||
			f.Extension.Equals(".scss", StringComparison.OrdinalIgnoreCase) ||
			f.Extension.Equals(".sass", StringComparison.OrdinalIgnoreCase))
			return FileRole.Style;

		if (AssetExtensions.Contains(f.Extension))
			return FileRole.Asset;

		return FileRole.Other;
	}

	// index.ts is also the standard barrel-file name, so only a root-level one is
	// the app's actual entry point.
	bool IsEntryPoint(FileInfo f) =>
		RootEntryPointNames.Contains(f.Name, StringComparer.OrdinalIgnoreCase) &&
		string.Equals(f.DirectoryName, _projectRoot.FullName, StringComparison.OrdinalIgnoreCase);

	static bool IsNativeSource(FileInfo f) =>
		f.Extension is ".kt" or ".java" or ".swift" or ".m" or ".mm" or ".h" or ".gradle";

	static bool ContainsFolder(FileInfo f, string[] folderNames) =>
		folderNames.Any(name => f.FullName.Contains(
			$"{Path.DirectorySeparatorChar}{name}{Path.DirectorySeparatorChar}",
			StringComparison.OrdinalIgnoreCase));
}