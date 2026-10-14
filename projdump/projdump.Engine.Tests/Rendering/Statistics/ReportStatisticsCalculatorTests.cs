using projdump.Engine.Measurement;
using projdump.Engine.Rendering;
using projdump.Engine.Rendering.Statistics;

namespace projdump.Engine.Tests.Rendering.Statistics;

[TestFixture]
public class ReportStatisticsCalculatorTests
{
	readonly ReportStatisticsCalculator _calculator = new(new CharacterHeuristicTokenEstimator());

	[Test]
	public void Calculate_FileBlock_MeasuresItsMarkdown()
	{
		var section = new ReportSection("App Code", "", [new("Program.cs", "12345678")]);

		var statistics = _calculator.Calculate([section]);

		Assert.That(statistics.Files.Single().Tokens, Is.EqualTo(2));
	}

	[Test]
	public void Calculate_FileBlock_RecordsItsSectionAndPath()
	{
		var section = new ReportSection("Configuration", "", [new("appsettings.json", "{}")]);

		var file = _calculator.Calculate([section]).Files.Single();

		using (Assert.EnterMultipleScope())
		{
			Assert.That(file.SectionName, Is.EqualTo("Configuration"));
			Assert.That(file.RelativePath, Is.EqualTo("appsettings.json"));
		}
	}

	[Test]
	public void Calculate_SectionWithIntroAndFiles_MeasuresThemTogether()
	{
		var section = new ReportSection("App Code", "1234", [new("Program.cs", "12345678")]);

		var sectionCount = _calculator.Calculate([section]).Sections.Single();

		using (Assert.EnterMultipleScope())
		{
			Assert.That(sectionCount.Tokens, Is.EqualTo(3));
			Assert.That(sectionCount.FileCount, Is.EqualTo(1));
		}
	}

	[Test]
	public void Calculate_SectionWithoutFiles_HasZeroFileCount()
	{
		var section = ReportSection.WithHeading("Project Summary", "table");

		Assert.That(_calculator.Calculate([section]).Sections.Single().FileCount, Is.EqualTo(0));
	}

	[Test]
	public void Calculate_SectionsOutOfAlphabeticalOrder_KeepsRenderOrder()
	{
		var sections = new[] { ReportSection.WithHeading("Project Summary"), ReportSection.WithHeading("App Code") };

		var names = _calculator.Calculate(sections).Sections.Select(section => section.SectionName);

		Assert.That(names, Is.EqualTo(new[] { "Project Summary", "App Code" }));
	}

	[Test]
	public void Calculate_Always_CarriesTheEstimatorDescription()
	{
		Assert.That(_calculator.Calculate([]).EstimationMethod, Is.EqualTo("character count ÷ 4"));
	}
}