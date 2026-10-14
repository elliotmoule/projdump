using projdump.Engine.Rendering;
using projdump.Engine.Rendering.Statistics;
using projdump.Terminal.Tests.TestSupport;

namespace projdump.Terminal.Tests;

[TestFixture]
public class StatisticsConsoleWriterTests
{
	static RenderedReport CreateReport(IReadOnlyList<FileTokenCount>? files = null)
	{
		var statistics = new ReportStatistics(
			"character count ÷ 4",
			[new("Project Summary", 0, 40), new("App Code", 1, 1200)],
			files ?? [new("Program.cs", "App Code", 1200)]);

		return new RenderedReport("markdown", 1300, statistics, [], []);
	}

	static string CaptureWrite(RenderedReport report)
	{
		using var console = new ConsoleCapture();
		StatisticsConsoleWriter.Write("MyApp", report);
		return console.Output;
	}

	[Test]
	public void Write_Statistics_PrintsProjectMethodAndWholeReportTotal()
	{
		string output = CaptureWrite(CreateReport());

		using (Assert.EnterMultipleScope())
		{
			Assert.That(output, Does.Contain("Token statistics for MyApp (character count ÷ 4)"));
			Assert.That(output, Does.Contain($"Whole report: ~{1300:N0}"));
		}
	}

	[Test]
	public void Write_SectionWithoutFiles_ShowsDashForFileCount()
	{
		string summaryLine = CaptureWrite(CreateReport())
			.Split(Environment.NewLine)
			.Single(line => line.TrimStart().StartsWith("Project Summary", StringComparison.Ordinal));

		Assert.That(summaryLine, Does.Contain(" -  "));
	}

	[Test]
	public void Write_Statistics_ListsContentTotalAndLargestFiles()
	{
		string output = CaptureWrite(CreateReport());

		using (Assert.EnterMultipleScope())
		{
			Assert.That(output, Does.Contain("Content total"));
			Assert.That(output, Does.Contain($"Largest {ReportStatistics.LargestFileCount} files:"));
			Assert.That(output, Does.Contain("Program.cs"));
		}
	}

	[Test]
	public void Write_NoFiles_OmitsLargestFilesList()
	{
		Assert.That(CaptureWrite(CreateReport(files: [])), Does.Not.Contain("Largest"));
	}
}