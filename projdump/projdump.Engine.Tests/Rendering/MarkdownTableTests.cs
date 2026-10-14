using projdump.Engine.Rendering;

namespace projdump.Engine.Tests.Rendering;

[TestFixture]
public class MarkdownTableTests
{
	static readonly string NewLine = Environment.NewLine;

	static MarkdownTableColumn Column(string header, MarkdownColumnAlignment alignment = MarkdownColumnAlignment.Left) =>
		new(header, alignment);

	[Test]
	public void ToMarkdown_TwoColumnsOneRow_WritesHeaderAlignmentAndRow()
	{
		var table = new MarkdownTable(Column("Extension"), Column("Count")).AddRow(".cs", "3");

		Assert.That(table.ToMarkdown(), Is.EqualTo(
			$"| Extension | Count |{NewLine}| :--- | :--- |{NewLine}| .cs | 3 |{NewLine}"));
	}

	[Test]
	public void ToMarkdown_RightAlignedColumn_UsesRightAlignmentMarker()
	{
		var table = new MarkdownTable(Column("Section"), Column("Tokens", MarkdownColumnAlignment.Right));

		Assert.That(table.ToMarkdown(), Does.Contain("| :--- | ---: |"));
	}

	[Test]
	public void AddRow_TooFewCells_PadsWithEmptyCells()
	{
		var table = new MarkdownTable(Column("A"), Column("B"), Column("C")).AddRow("1");

		Assert.That(table.ToMarkdown(), Does.Contain("| 1 |  |  |"));
	}

	[Test]
	public void AddRow_TooManyCells_DropsExtras()
	{
		var table = new MarkdownTable(Column("A")).AddRow("1", "2");

		Assert.That(table.ToMarkdown(), Does.EndWith($"| 1 |{NewLine}"));
	}

	[Test]
	public void AddRow_CellContainingPipe_EscapesIt()
	{
		var table = new MarkdownTable(Column("Path")).AddRow("a|b");

		Assert.That(table.ToMarkdown(), Does.Contain(@"| a\|b |"));
	}
}