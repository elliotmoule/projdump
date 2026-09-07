using projdump.Engine.Analyzers.ReactNative;

namespace projdump.Engine.Tests.Analyzers.ReactNative;

[TestFixture]
public class ReactNativeProjectAnalyzerTests
{
	readonly ReactNativeProjectAnalyzer _analyzer = new();

	[Test]
	public void CanHandle_ReturnsFalse_ForNonExistentPath()
	{
		Assert.That(_analyzer.CanHandle(Path.Combine("definitely", "not", "a", "real", "path")), Is.False);
	}

	[Test]
	public void TypeKey_IsReactNative()
	{
		Assert.That(_analyzer.TypeKey, Is.EqualTo("react-native"));
	}

	[Test]
	public void DisplayName_IsReactNative()
	{
		Assert.That(_analyzer.DisplayName, Is.EqualTo("React Native"));
	}

	[Test]
	public void SupportedModes_IsDefaultOnly()
	{
		Assert.That(_analyzer.SupportedModes, Is.EquivalentTo(["default"]));
	}

	// CanHandle against a real package.json, and Analyze() itself, both need
	// real files on disk - covered in the integration fixture instead.
}