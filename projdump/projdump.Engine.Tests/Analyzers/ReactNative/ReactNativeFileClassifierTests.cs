using projdump.Engine.Analyzers.ReactNative;
using projdump.Engine.Core;
using projdump.Engine.Tests.Core.TestDoubles;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Analyzers.ReactNative;

[TestFixture]
public class ReactNativeFileClassifierTests
{
	static readonly ITestFileDetector NeverTest = new FakeTestFileDetector(false);
	static readonly ITestFileDetector AlwaysTest = new FakeTestFileDetector(true);

	// Every path below is built under this root so entry-point anchoring is meaningful.
	static readonly DirectoryInfo ProjectRoot = new(FakePaths.Combine("MyApp"));

	// Two classifiers: one plain React Native project, one Expo Router project.
	static readonly ReactNativeFileClassifier Classifier = new(ProjectRoot);
	static readonly DirectoryInfo RoutingRoot = new(FakePaths.Combine("MyApp", "src", "app"));
	static readonly ReactNativeFileClassifier RouterClassifier = new(ProjectRoot, RoutingRoot);

	static FileInfo RootFile(string fileName) => new(FakePaths.Combine("MyApp", fileName));

	static FileInfo NestedFile(params string[] segments) => new(FakePaths.Combine(["MyApp", .. segments]));

	// --- CodeFilePriority: plain React Native ---

	[TestCase("index.js")]
	[TestCase("index.ts")]
	[TestCase("App.tsx")]
	public void CodeFilePriority_ReturnsZero_ForRootEntryPoints(string fileName)
	{
		Assert.That(Classifier.CodeFilePriority(RootFile(fileName)), Is.EqualTo(0));
	}

	[TestCase("navigation")]
	[TestCase("navigators")]
	[TestCase("store")]
	[TestCase("stores")]
	[TestCase("contexts")]
	[TestCase("providers")]
	public void CodeFilePriority_ReturnsTwo_ForNavigationAndStateFolders(string folder)
	{
		Assert.That(Classifier.CodeFilePriority(NestedFile("src", folder, "RootNavigator.tsx")), Is.EqualTo(2));
	}

	[Test]
	public void CodeFilePriority_ReturnsThree_ForTypesFolder()
	{
		Assert.That(Classifier.CodeFilePriority(NestedFile("src", "types", "order.ts")), Is.EqualTo(3));
	}

	[Test]
	public void CodeFilePriority_ReturnsThree_ForTypesFileNamingConvention()
	{
		Assert.That(Classifier.CodeFilePriority(NestedFile("src", "OrderTypes.ts")), Is.EqualTo(3));
	}

	[TestCase("screens")]
	[TestCase("components")]
	[TestCase("hooks")]
	[TestCase("views")]
	public void CodeFilePriority_ReturnsFour_ForScreenAndComponentFolders(string folder)
	{
		Assert.That(Classifier.CodeFilePriority(NestedFile("src", folder, "HomeScreen.tsx")), Is.EqualTo(4));
	}

	[Test]
	public void CodeFilePriority_ReturnsFour_ForBarrelFileInComponentsFolder()
	{
		// Regression: index.ts is the standard barrel name, so only a root-level
		// one counts as an entry point.
		Assert.That(Classifier.CodeFilePriority(NestedFile("src", "components", "index.ts")), Is.EqualTo(4));
	}

	[Test]
	public void CodeFilePriority_ReturnsTwo_ForBarrelFileInNavigationFolder()
	{
		// The case that first exposed the entry-point bug.
		Assert.That(Classifier.CodeFilePriority(NestedFile("src", "navigation", "index.ts")), Is.EqualTo(2));
	}

	[Test]
	public void CodeFilePriority_ReturnsFive_ForUncategorisedCode()
	{
		Assert.That(Classifier.CodeFilePriority(NestedFile("src", "utils", "formatDate.ts")), Is.EqualTo(5));
	}

	[Test]
	public void CodeFilePriority_ReturnsSix_ForNativeSource()
	{
		// Native source sinks below plain JS - it's usually generated boilerplate.
		Assert.That(Classifier.CodeFilePriority(NestedFile("android", "app", "src", "MainActivity.kt")), Is.EqualTo(6));
	}

	// --- CodeFilePriority: Expo Router ---

	[Test]
	public void CodeFilePriority_ReturnsZero_ForRoutingRootLayout()
	{
		// Expo Router projects have no root App.tsx - this layout is the app shell.
		Assert.That(RouterClassifier.CodeFilePriority(NestedFile("src", "app", "_layout.tsx")), Is.EqualTo(0));
	}

	[Test]
	public void CodeFilePriority_ReturnsOne_ForNestedLayout()
	{
		Assert.That(RouterClassifier.CodeFilePriority(NestedFile("src", "app", "(tabs)", "_layout.tsx")), Is.EqualTo(1));
	}

	[Test]
	public void CodeFilePriority_ReturnsTwo_ForRouteFile()
	{
		Assert.That(RouterClassifier.CodeFilePriority(NestedFile("src", "app", "login.tsx")), Is.EqualTo(2));
	}

	[Test]
	public void CodeFilePriority_ReturnsTwo_ForNestedRouteFile()
	{
		Assert.That(RouterClassifier.CodeFilePriority(NestedFile("src", "app", "confirmemail", "[code].tsx")), Is.EqualTo(2));
	}

	[Test]
	public void CodeFilePriority_ReturnsFour_ForAppFolderFile_WhenNotExpoRouter()
	{
		// Without a routing root, src/app is just another folder - and "app" isn't
		// a screen/component segment, so this lands on the uncategorised default.
		Assert.That(Classifier.CodeFilePriority(NestedFile("src", "app", "login.tsx")), Is.EqualTo(5));
	}

	// --- AssignRole: general ---

	[Test]
	public void AssignRole_ReturnsTest_WhenDetectorSaysSo()
	{
		Assert.That(Classifier.AssignRole(NestedFile("src", "HomeScreen.tsx"), AlwaysTest), Is.EqualTo(FileRole.Test));
	}

	[Test]
	public void AssignRole_ReturnsBuild_ForPackageJson()
	{
		Assert.That(Classifier.AssignRole(RootFile("package.json"), NeverTest), Is.EqualTo(FileRole.Build));
	}

	[Test]
	public void AssignRole_ReturnsDoc_ForMarkdown()
	{
		Assert.That(Classifier.AssignRole(RootFile("README.md"), NeverTest), Is.EqualTo(FileRole.Doc));
	}

	[TestCase("metro.config.js")]
	[TestCase("babel.config.js")]
	[TestCase("app.json")]
	[TestCase("eas.json")]
	[TestCase("jest.config.js")]
	public void AssignRole_ReturnsConfig_ForJavaScriptTooling(string fileName)
	{
		Assert.That(Classifier.AssignRole(RootFile(fileName), NeverTest), Is.EqualTo(FileRole.Config));
	}

	[TestCase("Podfile")]
	[TestCase("Info.plist")]
	[TestCase("AndroidManifest.xml")]
	[TestCase("build.gradle")]
	public void AssignRole_ReturnsConfig_ForNativeProjectConfiguration(string fileName)
	{
		Assert.That(Classifier.AssignRole(NestedFile("android", fileName), NeverTest), Is.EqualTo(FileRole.Config));
	}

	[TestCase("index.js")]
	[TestCase("App.tsx")]
	public void AssignRole_ReturnsEntryPoint_ForRootEntryPoints(string fileName)
	{
		Assert.That(Classifier.AssignRole(RootFile(fileName), NeverTest), Is.EqualTo(FileRole.EntryPoint));
	}

	[Test]
	public void AssignRole_ReturnsComponent_ForBarrelFileOutsideTheRoot()
	{
		// Same anchoring rule as CodeFilePriority - a nested index.tsx is not an entry point.
		Assert.That(Classifier.AssignRole(NestedFile("src", "components", "index.tsx"), NeverTest), Is.EqualTo(FileRole.Component));
	}

	[Test]
	public void AssignRole_ReturnsApiSurface_ForNavigationFolder()
	{
		Assert.That(Classifier.AssignRole(NestedFile("src", "navigation", "RootNavigator.tsx"), NeverTest), Is.EqualTo(FileRole.ApiSurface));
	}

	[TestCase("Button.tsx")]
	[TestCase("Button.jsx")]
	public void AssignRole_ReturnsComponent_ForJsxFiles(string fileName)
	{
		Assert.That(Classifier.AssignRole(NestedFile("src", "components", fileName), NeverTest), Is.EqualTo(FileRole.Component));
	}

	[Test]
	public void AssignRole_ReturnsOther_ForPlainTypeScript()
	{
		// Only .tsx/.jsx are components - plain .ts is logic.
		Assert.That(Classifier.AssignRole(NestedFile("src", "utils", "formatDate.ts"), NeverTest), Is.EqualTo(FileRole.Other));
	}

	[Test]
	public void AssignRole_ReturnsStyle_ForCss()
	{
		Assert.That(Classifier.AssignRole(NestedFile("src", "theme.css"), NeverTest), Is.EqualTo(FileRole.Style));
	}

	[TestCase("Inter.ttf")]
	[TestCase("logo.png")]
	public void AssignRole_ReturnsAsset_ForFontsAndImages(string fileName)
	{
		Assert.That(Classifier.AssignRole(NestedFile("assets", fileName), NeverTest), Is.EqualTo(FileRole.Asset));
	}

	// --- AssignRole: Expo Router ---

	[Test]
	public void AssignRole_ReturnsEntryPoint_ForRoutingRootLayout()
	{
		Assert.That(RouterClassifier.AssignRole(NestedFile("src", "app", "_layout.tsx"), NeverTest), Is.EqualTo(FileRole.EntryPoint));
	}

	[Test]
	public void AssignRole_ReturnsApiSurface_ForRouteFile()
	{
		Assert.That(RouterClassifier.AssignRole(NestedFile("src", "app", "login.tsx"), NeverTest), Is.EqualTo(FileRole.ApiSurface));
	}

	[Test]
	public void AssignRole_ReturnsApiSurface_ForNestedLayout()
	{
		Assert.That(RouterClassifier.AssignRole(NestedFile("src", "app", "(tabs)", "_layout.tsx"), NeverTest), Is.EqualTo(FileRole.ApiSurface));
	}

	[Test]
	public void AssignRole_ReturnsComponent_ForAppFolderFile_WhenNotExpoRouter()
	{
		// Without a routing root, src/app is just another folder.
		Assert.That(Classifier.AssignRole(NestedFile("src", "app", "login.tsx"), NeverTest), Is.EqualTo(FileRole.Component));
	}

	// --- Static helpers ---

	[Test]
	public void IsCodeFile_ReturnsTrue_ForNativeSource()
	{
		Assert.That(ReactNativeFileClassifier.IsCodeFile(NestedFile("ios", "AppDelegate.swift")), Is.True);
	}

	[Test]
	public void IsCodeFile_ReturnsFalse_ForImage()
	{
		Assert.That(ReactNativeFileClassifier.IsCodeFile(NestedFile("assets", "logo.png")), Is.False);
	}
}