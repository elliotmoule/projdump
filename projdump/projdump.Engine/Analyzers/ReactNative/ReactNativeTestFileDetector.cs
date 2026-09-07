using projdump.Engine.Core;

namespace projdump.Engine.Analyzers.ReactNative;

sealed class ReactNativeTestFileDetector : ITestFileDetector
{
	static readonly string[] TestPathSegments =
	[
		$"{Path.DirectorySeparatorChar}__tests__{Path.DirectorySeparatorChar}",
		$"{Path.DirectorySeparatorChar}__mocks__{Path.DirectorySeparatorChar}",
		$"{Path.DirectorySeparatorChar}__snapshots__{Path.DirectorySeparatorChar}",

        // Detox and Maestro both keep device-level suites here.
        $"{Path.DirectorySeparatorChar}e2e{Path.DirectorySeparatorChar}",
	];

	static readonly string[] TestFileNameFragments = [".test.", ".spec.", ".e2e."];

	static readonly HashSet<string> TestSupportFileNames = new(StringComparer.OrdinalIgnoreCase)
	{
		"jest.setup.js",
		"jest.setup.ts",
		"setupTests.js",
		"setupTests.ts",
		"detox.config.js",
	};

	/// <summary>
	/// Determines whether a file is a test, mock, snapshot, or test-harness setup file.
	/// </summary>
	/// <param name="f">The candidate file gathered from the project tree.</param>
	/// <returns>True when the file belongs to the test suite rather than the app.</returns>
	public bool IsTestFile(FileInfo f) =>
		TestPathSegments.Any(seg => f.FullName.Contains(seg)) ||
		TestFileNameFragments.Any(fragment => f.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase)) ||
		TestSupportFileNames.Contains(f.Name);
}