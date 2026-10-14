namespace projdump.Engine.Rendering.Sections;

// One implementation per content section; the renderer runs them in the order the factory registers them.
interface IReportSectionBuilder
{
	/// <summary>
	/// Builds this section from the render request.
	/// </summary>
	/// <param name="request">The files and flags being rendered.</param>
	/// <returns>The section, or null when it has nothing to show.</returns>
	ReportSection? Build(ReportRenderRequest request);
}