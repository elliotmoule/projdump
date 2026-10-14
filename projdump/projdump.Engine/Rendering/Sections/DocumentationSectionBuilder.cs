namespace projdump.Engine.Rendering.Sections;

sealed class DocumentationSectionBuilder(ReportFileBlockFactory blockFactory) : IReportSectionBuilder
{
	const string ExternalReadmeNote = "> **Sourced from outside the project tree.** Found by searching parent directories for a README; it is not included in the file listing or extension counts above.";

	/// <summary>
	/// Builds one unfenced block per documentation file.
	/// </summary>
	/// <param name="request">The files and flags being rendered.</param>
	/// <returns>The Documentation section, or null when there are no documentation files.</returns>
	public ReportSection? Build(ReportRenderRequest request)
	{
		if (request.ReadmeFiles.Count == 0)
			return null;

		var blocks = request.ReadmeFiles.Select(file => CreateBlock(request, file)).ToList();
		return ReportSection.WithHeading(ReportSectionNames.Documentation, files: blocks);
	}

	/// <summary>
	/// Builds a documentation block, flagging a README pulled in from an ancestor directory.
	/// </summary>
	/// <param name="request">The files and flags being rendered.</param>
	/// <param name="file">The documentation file.</param>
	/// <returns>The block, showing the full path and a note when the file sits outside the project.</returns>
	ReportFileBlock CreateBlock(ReportRenderRequest request, FileInfo file)
	{
		string relativePath = request.GetRelativePath(file);

		// A relative path that climbs out of the root means the file came from an ancestor directory.
		bool isOutsideProject = relativePath.StartsWith("..", StringComparison.Ordinal);

		return isOutsideProject
			? blockFactory.Create(file, file.FullName, fenceLanguage: null, request.Slim, ExternalReadmeNote)
			: blockFactory.Create(file, relativePath, fenceLanguage: null, request.Slim);
	}
}