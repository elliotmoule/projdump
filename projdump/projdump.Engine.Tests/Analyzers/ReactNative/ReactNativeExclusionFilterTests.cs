using projdump.Engine.Analyzers.ReactNative;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Analyzers.ReactNative;

[TestFixture]
public class ReactNativeExclusionFilterTests
{
	readonly ReactNativeExclusionFilter _filter = new();

	[Test]
	public void IsExcluded_ReturnsTrue_ForNodeModulesFolder()
	{
		var path = FakePaths.Combine("MyApp", "node_modules", "react-native", "index.js");
		Assert.That(_filter.IsExcluded(new FileInfo(path)), Is.True);
	}

	[TestCase("package-lock.json")]
	[TestCase("yarn.lock")]
	[TestCase("Podfile.lock")]
	[TestCase("local.properties")]
	public void IsExcluded_ReturnsTrue_ForLockAndMachineSpecificFiles(string fileName)
	{
		Assert.That(_filter.IsExcluded(new FileInfo(fileName)), Is.True);
	}

	[Test]
	public void IsExcluded_ReturnsTrue_ForAndroidBuildOutput()
	{
		var path = FakePaths.Combine("MyApp", "android", "app", "build", "generated", "Thing.java");
		Assert.That(_filter.IsExcluded(new FileInfo(path)), Is.True);
	}

	[Test]
	public void IsExcluded_ReturnsTrue_ForIosPods()
	{
		var path = FakePaths.Combine("MyApp", "ios", "Pods", "Firebase", "Firebase.h");
		Assert.That(_filter.IsExcluded(new FileInfo(path)), Is.True);
	}

	[Test]
	public void IsExcluded_ReturnsTrue_ForXcodeProjectMetadata()
	{
		// .xcodeproj is a directory, so everything inside it is generated project state.
		var path = FakePaths.Combine("MyApp", "ios", "MyApp.xcodeproj", "project.pbxproj");
		Assert.That(_filter.IsExcluded(new FileInfo(path)), Is.True);
	}

	[Test]
	public void IsExcluded_ReturnsTrue_ForExpoCache()
	{
		var path = FakePaths.Combine("MyApp", ".expo", "settings.json");
		Assert.That(_filter.IsExcluded(new FileInfo(path)), Is.True);
	}

	[Test]
	public void IsExcluded_ReturnsTrue_ForPackagerBundle()
	{
		Assert.That(_filter.IsExcluded(new FileInfo("index.android.bundle")), Is.True);
	}

	[Test]
	public void IsExcluded_ReturnsTrue_ForIconComposerBundle()
	{
		var path = FakePaths.Combine("MyApp", "assets", "expo.icon", "Assets", "grid.png");
		Assert.That(_filter.IsExcluded(new FileInfo(path)), Is.True);
	}

	[TestCase("reset-project.js")]
	[TestCase(".tsc-output.txt")]
	public void IsExcluded_ReturnsTrue_ForTemplateBoilerplateAndToolOutput(string fileName)
	{
		Assert.That(_filter.IsExcluded(new FileInfo(fileName)), Is.True);
	}

	[Test]
	public void IsExcluded_ReturnsFalse_ForHandWrittenNativeSource()
	{
		// Deliberate: native source survives, only build output is dropped.
		var path = FakePaths.Combine("MyApp", "android", "app", "src", "main", "MainActivity.kt");
		Assert.That(_filter.IsExcluded(new FileInfo(path)), Is.False);
	}

	[Test]
	public void IsExcluded_ReturnsFalse_ForBuildGradle()
	{
		var path = FakePaths.Combine("MyApp", "android", "app", "build.gradle");
		Assert.That(_filter.IsExcluded(new FileInfo(path)), Is.False);
	}

	[Test]
	public void IsExcluded_ReturnsFalse_ForRegularSourceFile()
	{
		var path = FakePaths.Combine("src", "screens", "HomeScreen.tsx");
		Assert.That(_filter.IsExcluded(new FileInfo(path)), Is.False);
	}
}