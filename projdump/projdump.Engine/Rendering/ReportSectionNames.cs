namespace projdump.Engine.Rendering;

// Shared by section builders, the renderer factory and Chunk 3's manifest, which picks sections by name.
public static class ReportSectionNames
{
	public const string AppCode = "App Code";
	public const string Configuration = "Configuration";
	public const string Database = "Database";
	public const string Documentation = "Documentation";
	public const string Flags = "Flags";
	public const string ProjectDependencies = "Project Dependencies";
	public const string ProjectStructure = "Project Structure";
	public const string ProjectSummary = "Project Summary";
	public const string Scripts = "Scripts";
	public const string SolutionConfiguration = "Solution Configuration";
	public const string TokenStatistics = "Token Statistics";
}