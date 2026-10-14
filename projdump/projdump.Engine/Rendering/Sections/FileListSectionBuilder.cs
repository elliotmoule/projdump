using projdump.Engine.Shared;

namespace projdump.Engine.Rendering.Sections;

// One configurable builder covers Configuration, Scripts, Database and App Code, which differ only in name and files.
sealed class FileListSectionBuilder(
	ReportFileBlockFactory blockFactory,
	string sectionName,
	Func<ReportRenderRequest, IReadOnlyList<FileInfo>> selectFiles,
	bool renderWhenEmpty = false) : IReportSectionBuilder
{
	/// <summary>
	/// Builds a heading followed by one fenced block per selected file.
	/// </summary>
	/// <param name="request">The files and flags being rendered.</param>
	/// <returns>The section, or null when it has no files and isn't set to render empty.</returns>
	public ReportSection? Build(ReportRenderRequest request)
	{
		var files = selectFiles(request);
		if (files.Count == 0 && !renderWhenEmpty)
			return null;

		var blocks = files
			.Select(file => blockFactory.Create(file, request.GetRelativePath(file), FormatHelpers.GetMarkdownLanguage(file.Extension), request.Slim))
			.ToList();

		return ReportSection.WithHeading(sectionName, files: blocks);
	}
}