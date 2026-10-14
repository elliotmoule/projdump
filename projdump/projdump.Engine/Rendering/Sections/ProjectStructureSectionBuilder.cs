using System.Text;

namespace projdump.Engine.Rendering.Sections;

sealed class ProjectStructureSectionBuilder : IReportSectionBuilder
{
	/// <summary>
	/// Builds the file tree as a fenced list of root-relative paths.
	/// </summary>
	/// <param name="request">The files and flags being rendered.</param>
	/// <returns>The Project Structure section.</returns>
	public ReportSection? Build(ReportRenderRequest request)
	{
		StringBuilder body = new();
		body.AppendLine("```text");
		foreach (var file in request.AllFiles)
			body.AppendLine(request.GetRelativePath(file));
		body.AppendLine("```");
		body.AppendLine();

		return ReportSection.WithHeading(ReportSectionNames.ProjectStructure, body.ToString());
	}
}