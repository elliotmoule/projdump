using projdump.Engine.Core;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Core;

[TestFixture]
public class ScriptFileClassifierTests
{
	// FakePaths with no segments gives the fixture root the other paths hang off.
	static readonly DirectoryInfo RootDir = new(FakePaths.Combine());

	[TestCase("deploy.ps1")]
	[TestCase("module.psm1")]
	[TestCase("manifest.psd1")]
	[TestCase("build.bat")]
	[TestCase("run.cmd")]
	[TestCase("setup.sh")]
	[TestCase("install.bash")]
	[TestCase("profile.zsh")]
	public void IsScriptFile_ReturnsTrue_ForKnownScriptExtensions(string fileName)
	{
		Assert.That(ScriptFileClassifier.IsScriptFile(new FileInfo(fileName)), Is.True);
	}

	[TestCase("DEPLOY.PS1")]
	[TestCase("Build.Bat")]
	public void IsScriptFile_IgnoresCase(string fileName)
	{
		Assert.That(ScriptFileClassifier.IsScriptFile(new FileInfo(fileName)), Is.True);
	}

	[TestCase("Program.cs")]
	[TestCase("appsettings.json")]
	[TestCase("README.md")]
	[TestCase("Dockerfile")]
	public void IsScriptFile_ReturnsFalse_ForNonScripts(string fileName)
	{
		Assert.That(ScriptFileClassifier.IsScriptFile(new FileInfo(fileName)), Is.False);
	}

	[Test]
	public void ScriptFilePriority_ReturnsZero_ForScriptDirectlyInRoot()
	{
		var file = new FileInfo(FakePaths.Combine("deploy.ps1"));

		Assert.That(ScriptFileClassifier.ScriptFilePriority(file, RootDir), Is.EqualTo(0));
	}

	[TestCase("scripts")]
	[TestCase("build")]
	[TestCase("tools")]
	[TestCase("deploy")]
	[TestCase("ci")]
	public void ScriptFilePriority_ReturnsOne_ForScriptInConventionalFolder(string folderName)
	{
		var file = new FileInfo(FakePaths.Combine(folderName, "publish.ps1"));

		Assert.That(ScriptFileClassifier.ScriptFilePriority(file, RootDir), Is.EqualTo(1));
	}

	[Test]
	public void ScriptFilePriority_ReturnsOne_ForNestedScriptFolder()
	{
		var file = new FileInfo(FakePaths.Combine("src", "tools", "migrate.sh"));

		Assert.That(ScriptFileClassifier.ScriptFilePriority(file, RootDir), Is.EqualTo(1));
	}

	[Test]
	public void ScriptFilePriority_ReturnsTwo_ForScriptElsewhereInTree()
	{
		var file = new FileInfo(FakePaths.Combine("src", "Api", "seed.ps1"));

		Assert.That(ScriptFileClassifier.ScriptFilePriority(file, RootDir), Is.EqualTo(2));
	}
}