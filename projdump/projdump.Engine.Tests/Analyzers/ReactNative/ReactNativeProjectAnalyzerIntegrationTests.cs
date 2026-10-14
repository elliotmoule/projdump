using projdump.Engine.Analyzers.ReactNative;
using projdump.Engine.Core;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Analyzers.ReactNative;

[TestFixture]
[Category("Integration")]
public class ReactNativeProjectAnalyzerIntegrationTests
{
	const string ReactNativePackageJson = """
        { "name": "my-rn-app", "dependencies": { "react": "18.2.0", "react-native": "0.74.1" } }
        """;

	static void BuildBasicProject(TempProjectDirectory temp)
	{
		temp.WriteFile("package.json", ReactNativePackageJson);
		temp.WriteFile("index.js", "// entry point");
		temp.WriteFile("App.tsx", "// root component");
		temp.WriteFile("metro.config.js", "module.exports = {};");
		temp.WriteFile("README.md", "# my-rn-app");
		temp.WriteFile(Path.Combine("src", "screens", "HomeScreen.tsx"), "// screen");
		temp.WriteFile(Path.Combine("src", "navigation", "RootNavigator.tsx"), "// navigator");
		temp.WriteFile(Path.Combine("node_modules", "react-native", "index.js"), "junk");
		temp.WriteFile(Path.Combine("android", "app", "build", "generated", "Thing.java"), "junk");
		temp.WriteFile(Path.Combine("ios", "Pods", "Firebase", "Firebase.h"), "junk");
	}

	[Test]
	public void Analyze_UsesPackageJsonNameAsProjectName()
	{
		using var temp = new TempProjectDirectory();
		BuildBasicProject(temp);

		var analysis = new ReactNativeProjectAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions());

		Assert.That(analysis.ProjectName, Is.EqualTo("my-rn-app"));
	}

	[Test]
	public void Analyze_ExcludesDependencyAndBuildOutput()
	{
		using var temp = new TempProjectDirectory();
		BuildBasicProject(temp);

		var analysis = new ReactNativeProjectAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions());
		var fileNames = analysis.AllFiles.Select(f => f.File.Name).ToList();

		using (Assert.EnterMultipleScope())
		{
			Assert.That(fileNames, Does.Contain("HomeScreen.tsx"));
			Assert.That(fileNames, Does.Not.Contain("Thing.java"));
			Assert.That(fileNames, Does.Not.Contain("Firebase.h"));
		}
	}

	[Test]
	public void Analyze_KeepsHandWrittenNativeSource()
	{
		// The counterpart to the exclusion test above - android/ isn't dropped wholesale.
		using var temp = new TempProjectDirectory();
		BuildBasicProject(temp);
		temp.WriteFile(Path.Combine("android", "app", "src", "main", "MainActivity.kt"), "// activity");
		temp.WriteFile(Path.Combine("android", "app", "build.gradle"), "// gradle");

		var analysis = new ReactNativeProjectAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions());
		var fileNames = analysis.AllFiles.Select(f => f.File.Name).ToList();

		using (Assert.EnterMultipleScope())
		{
			Assert.That(fileNames, Does.Contain("MainActivity.kt"));
			Assert.That(fileNames, Does.Contain("build.gradle"));
		}
	}

	[Test]
	public void Analyze_OrdersEntryPointsBeforeScreens()
	{
		using var temp = new TempProjectDirectory();
		BuildBasicProject(temp);

		var analysis = new ReactNativeProjectAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions());
		var codeFileNames = analysis.CodeFiles.Select(f => f.File.Name).ToList();

		Assert.That(codeFileNames.IndexOf("App.tsx"), Is.LessThan(codeFileNames.IndexOf("HomeScreen.tsx")));
	}

	[Test]
	public void Analyze_PicksUpMetroConfigAsConfiguration()
	{
		using var temp = new TempProjectDirectory();
		BuildBasicProject(temp);

		var analysis = new ReactNativeProjectAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions());

		Assert.That(analysis.ConfigFiles.Select(f => f.File.Name), Does.Contain("metro.config.js"));
	}

	[Test]
	public void Analyze_ExcludeTests_DropsTestFiles()
	{
		using var temp = new TempProjectDirectory();
		BuildBasicProject(temp);
		temp.WriteFile(Path.Combine("src", "__tests__", "HomeScreen.test.tsx"), "// test");

		var analysis = new ReactNativeProjectAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions { ExcludeTests = true });

		Assert.That(analysis.AllFiles.Select(f => f.File.Name), Does.Not.Contain("HomeScreen.test.tsx"));
	}

	[Test]
	public void Analyze_ScopeDir_RestrictsToSubdirectory()
	{
		using var temp = new TempProjectDirectory();
		BuildBasicProject(temp);

		var analysis = new ReactNativeProjectAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions { ScopeDir = Path.Combine("src", "screens") });

		using (Assert.EnterMultipleScope())
		{
			// Project name is resolved before --scope moves the root.
			Assert.That(analysis.ProjectName, Is.EqualTo("my-rn-app"));
			Assert.That(analysis.AllFiles.Select(f => f.File.Name), Does.Contain("HomeScreen.tsx"));
			Assert.That(analysis.AllFiles.Select(f => f.File.Name), Does.Not.Contain("index.js"));
		}
	}

	[Test]
	public void Analyze_WithoutTheSwitch_DoesNotPullInTheRepositoryReadme()
	{
		using var temp = new TempProjectDirectory();
		Directory.CreateDirectory(temp.GetFullPath(".git"));
		temp.WriteFile("README.md", "# repo readme");
		temp.WriteFile(Path.Combine("mobile", "package.json"), ReactNativePackageJson);
		temp.WriteFile(Path.Combine("mobile", "index.js"), "// entry point");

		var analysis = new ReactNativeProjectAnalyzer().Analyze(temp.GetFullPath("mobile"), new ProjectAnalysisOptions());

		Assert.That(analysis.ReadmeFiles, Is.Empty);
	}

	[Test]
	public void Analyze_WithTheSwitch_PullsInTheRepositoryReadme()
	{
		using var temp = new TempProjectDirectory();
		Directory.CreateDirectory(temp.GetFullPath(".git"));
		temp.WriteFile("README.md", "# repo readme");
		temp.WriteFile(Path.Combine("mobile", "package.json"), ReactNativePackageJson);
		temp.WriteFile(Path.Combine("mobile", "index.js"), "// entry point");

		var analysis = new ReactNativeProjectAnalyzer().Analyze(temp.GetFullPath("mobile"), new ProjectAnalysisOptions { SearchForReadme = true });

		Assert.That(analysis.ReadmeFiles.Select(f => f.File.Name), Does.Contain("README.md"));
	}

	[Test]
	public void CanHandle_ReturnsTrue_ForDirectoryDeclaringReactNative()
	{
		using var temp = new TempProjectDirectory();
		temp.WriteFile("package.json", ReactNativePackageJson);

		Assert.That(new ReactNativeProjectAnalyzer().CanHandle(temp.RootPath), Is.True);
	}

	[Test]
	public void CanHandle_ReturnsFalse_ForReactWebProject()
	{
		// react-dom rather than react-native is the whole distinction between the two stacks.
		using var temp = new TempProjectDirectory();
		temp.WriteFile("package.json", """{ "name": "web-app", "dependencies": { "react": "18.2.0", "react-dom": "18.2.0" } }""");

		Assert.That(new ReactNativeProjectAnalyzer().CanHandle(temp.RootPath), Is.False);
	}

	[Test]
	public void CanHandle_ReturnsFalse_WhenOnlyAReactNativeLibraryIsDeclared()
	{
		// "react-native-reanimated" must not satisfy the exact-name check.
		using var temp = new TempProjectDirectory();
		temp.WriteFile("package.json", """{ "name": "web-app", "dependencies": { "react-native-reanimated": "3.0.0" } }""");

		Assert.That(new ReactNativeProjectAnalyzer().CanHandle(temp.RootPath), Is.False);
	}

	[Test]
	public void Analyze_ThrowsForDirectoryWithoutPackageJson()
	{
		using var temp = new TempProjectDirectory();

		Assert.Throws<ProjectAnalysisException>(() =>
			new ReactNativeProjectAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions()));
	}

	[Test]
	public void Analyze_ThrowsWhenPackageJsonDoesNotDeclareReactNative()
	{
		using var temp = new TempProjectDirectory();
		temp.WriteFile("package.json", """{ "name": "plain-node-app" }""");

		Assert.Throws<ProjectAnalysisException>(() =>
			new ReactNativeProjectAnalyzer().Analyze(temp.RootPath, new ProjectAnalysisOptions()));
	}
}