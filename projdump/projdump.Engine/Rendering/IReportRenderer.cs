namespace projdump.Engine.Rendering;

public interface IReportRenderer
{
	/// <summary>
	/// Builds the full report for an analysed project.
	/// </summary>
	/// <param name="request">The files and flags to render.</param>
	/// <returns>The report, its estimated token count, statistics, and the sections it was assembled from.</returns>
	RenderedReport Render(ReportRenderRequest request);
}