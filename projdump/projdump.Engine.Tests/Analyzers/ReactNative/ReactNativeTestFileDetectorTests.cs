using projdump.Engine.Analyzers.ReactNative;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Analyzers.ReactNative;

[TestFixture]
public class ReactNativeTestFileDetectorTests
{
	readonly ReactNativeTestFileDetector _detector = new();

	[TestCase("__tests__")]
	[TestCase("__mocks__")]
	[TestCase("__snapshots__")]
	[TestCase("e2e")]
	public void IsTestFile_ReturnsTrue_ForTestFolders(string folder)
	{
		var path = FakePaths.Combine("src", folder, "Thing.tsx");
		Assert.That(_detector.IsTestFile(new FileInfo(path)), Is.True);
	}

	[TestCase("HomeScreen.test.tsx")]
	[TestCase("useAuth.spec.ts")]
	[TestCase("login.e2e.ts")]
	public void IsTestFile_ReturnsTrue_ForTestNamingConventions(string fileName)
	{
		Assert.That(_detector.IsTestFile(new FileInfo(fileName)), Is.True);
	}

	[TestCase("jest.setup.ts")]
	[TestCase("setupTests.js")]
	[TestCase("detox.config.js")]
	public void IsTestFile_ReturnsTrue_ForTestHarnessSetup(string fileName)
	{
		Assert.That(_detector.IsTestFile(new FileInfo(fileName)), Is.True);
	}

	[Test]
	public void IsTestFile_ReturnsFalse_ForJestConfig()
	{
		// jest.config.js describes project setup, so it's Config - not part of the suite.
		Assert.That(_detector.IsTestFile(new FileInfo("jest.config.js")), Is.False);
	}

	[Test]
	public void IsTestFile_ReturnsFalse_ForRegularSourceFile()
	{
		var path = FakePaths.Combine("src", "screens", "HomeScreen.tsx");
		Assert.That(_detector.IsTestFile(new FileInfo(path)), Is.False);
	}
}