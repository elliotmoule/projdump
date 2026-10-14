using projdump.Engine.Shared;
using System.Text;

namespace projdump.Engine.Rendering;

// Instance rather than static: Chunk 2's content transformers will be injected here.
sealed class ReportFileBlockFactory
{
	/// <summary>
	/// Writes a file's trimmed content, fenced unless no language is given.
	/// </summary>
	/// <param name="sb">The block being built.</param>
	/// <param name="file">The file to read.</param>
	/// <param name="fenceLanguage">The code fence language, or null to write the content unfenced.</param>
	static void AppendContent(StringBuilder sb, FileInfo file, string? fenceLanguage)
	{
		string content = File.ReadAllText(file.FullName).Trim();
		if (fenceLanguage == null)
		{
			sb.AppendLine(content);
			return;
		}

		sb.AppendLine($"```{fenceLanguage}");
		sb.AppendLine(content);
		sb.AppendLine("```");
	}

	/// <summary>
	/// Builds one file entry: heading, path line, optional note, then content or the slim size line.
	/// </summary>
	/// <param name="file">The file being rendered.</param>
	/// <param name="displayPath">The path shown in the entry and recorded for stats.</param>
	/// <param name="fenceLanguage">The code fence language, or null to write the content unfenced.</param>
	/// <param name="slim">True to show the file size instead of its content.</param>
	/// <param name="note">An optional note shown above the content.</param>
	/// <returns>The block, ending with a blank line.</returns>
	public ReportFileBlock Create(FileInfo file, string displayPath, string? fenceLanguage, bool slim, string? note = null)
	{
		StringBuilder sb = new();
		sb.AppendLine($"### {file.Name}");
		sb.AppendLine($"**Path:** `{displayPath}`");
		sb.AppendLine();

		if (note != null)
		{
			sb.AppendLine(note);
			sb.AppendLine();
		}

		if (slim)
			sb.AppendLine($"_File size: {FormatHelpers.FormatFileSize(file.Length)}_");
		else
			AppendContent(sb, file, fenceLanguage);

		sb.AppendLine();
		return new ReportFileBlock(displayPath, sb.ToString());
	}
}