namespace projdump.Terminal.Tests;

[TestFixture]
public class ProgramSanitizeForFileNameTests
{
    [Test]
    public void SanitizeForFileName_LeavesNormalNameUnchanged()
    {
        Assert.That(Program.SanitizeForFileName("MyApp"), Is.EqualTo("MyApp"));
    }

    [Test]
    public void SanitizeForFileName_ReplacesInvalidCharacters()
    {
        // '/' is invalid in a filename on every platform - safe to test cross-platform.
        string result = Program.SanitizeForFileName("@myorg/my-app");

        Assert.That(result, Does.Not.Contain("/"));
        Assert.That(result, Is.EqualTo("@myorg-my-app"));
    }

    [Test]
    public void SanitizeForFileName_EmptyInput_FallsBackToProject()
    {
        Assert.That(Program.SanitizeForFileName(""), Is.EqualTo("project"));
    }

    [Test]
    public void SanitizeForFileName_WhitespaceOnlyInput_FallsBackToProject()
    {
        Assert.That(Program.SanitizeForFileName("   "), Is.EqualTo("project"));
    }
}