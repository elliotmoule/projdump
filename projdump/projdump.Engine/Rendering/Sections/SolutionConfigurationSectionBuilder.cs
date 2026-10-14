namespace projdump.Engine.Rendering.Sections;

sealed class SolutionConfigurationSectionBuilder(ReportFileBlockFactory blockFactory) : IReportSectionBuilder
{
	/// <summary>
	/// Builds a block holding the solution file itself.
	/// </summary>
	/// <param name="request">The files and flags being rendered.</param>
	/// <returns>The Solution Configuration section, or null when the input isn't a solution.</returns>
	public ReportSection? Build(ReportRenderRequest request)
	{
		if (!request.IsSolution)
			return null;

		// .sln is a bespoke text format; only .slnx is XML.
		string fenceLanguage = request.Extension == ".slnx" ? "xml" : "text";
		var block = blockFactory.Create(request.InputFileInfo, request.InputFileInfo.Name, fenceLanguage, request.Slim);

		return ReportSection.WithHeading(ReportSectionNames.SolutionConfiguration, files: [block]);
	}
}