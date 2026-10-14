using projdump.Engine.Rendering;

namespace projdump.Engine.Tests.Rendering;

[TestFixture]
public class ReportSectionTests
{
	static readonly string NewLine = Environment.NewLine;

	[Test]
	public void ToMarkdown_IntroAndFiles_ConcatenatesInOrder()
	{
		var section = new ReportSection("App Code", "intro;", [new("a.cs", "first;"), new("b.cs", "second;")]);

		Assert.That(section.ToMarkdown(), Is.EqualTo("intro;first;second;"));
	}

	[Test]
	public void WithHeading_NameAndBody_OpensWithHeadingThenBody()
	{
		var section = ReportSection.WithHeading("Project Summary", "body");

		Assert.That(section.IntroMarkdown, Is.EqualTo($"## Project Summary{NewLine}{NewLine}body"));
	}

	[Test]
	public void WithHeading_NoFiles_DefaultsToEmptyList()
	{
		Assert.That(ReportSection.WithHeading("Project Structure").Files, Is.Empty);
	}
}