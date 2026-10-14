using System.Text;

namespace projdump.Engine.Rendering;

sealed class MarkdownTable(params MarkdownTableColumn[] columns)
{
	readonly List<string[]> _rows = [];

	/// <summary>
	/// Adds a row, padding missing cells and dropping extras so a mismatch never breaks the table.
	/// </summary>
	/// <param name="cells">Cell values in column order.</param>
	/// <returns>This table, for chaining.</returns>
	public MarkdownTable AddRow(params string[] cells)
	{
		string[] fittedCells = [.. cells.Take(columns.Length).Concat(Enumerable.Repeat("", Math.Max(0, columns.Length - cells.Length)))];
		_rows.Add([.. fittedCells.Select(EscapePipes)]);
		return this;
	}

	// An unescaped pipe would start a new cell mid-value.
	static string EscapePipes(string cell) => cell.Replace("|", "\\|");

	static string FormatAlignment(MarkdownTableColumn column) =>
		column.Alignment == MarkdownColumnAlignment.Right ? "---:" : ":---";

	static string FormatRow(IEnumerable<string> cells) => $"| {string.Join(" | ", cells)} |";

	/// <summary>
	/// Writes the header, alignment row and data rows.
	/// </summary>
	/// <returns>The table, each line ending with a newline.</returns>
	public string ToMarkdown()
	{
		StringBuilder sb = new();
		sb.AppendLine(FormatRow(columns.Select(column => EscapePipes(column.Header))));
		sb.AppendLine(FormatRow(columns.Select(FormatAlignment)));
		foreach (var row in _rows)
			sb.AppendLine(FormatRow(row));
		return sb.ToString();
	}
}