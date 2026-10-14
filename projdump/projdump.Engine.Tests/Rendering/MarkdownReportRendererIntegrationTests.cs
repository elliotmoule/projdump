using projdump.Engine.Rendering;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Rendering;

[TestFixture]
[Category("Integration")]
public class MarkdownReportRendererIntegrationTests
{
	static ReportRenderRequest BuildRequest(
		TempProjectDirectory temp,
		bool isSolution,
		bool slim = false,
		bool excludeTests = false,
		string? scopeDir = null,
		IReadOnlyList<string>? excludeDirs = null,
		bool includeReadme = true,
		bool includeConfig = true,
		bool includeScript = false,
		bool includeSql = false,
		bool includeStats = false)
	{
		string csprojPath = temp.WriteFile("MyApp.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
		string programPath = temp.WriteFile("Program.cs", "var builder = WebApplication.CreateBuilder(args);");
		string configPath = temp.WriteFile("appsettings.json", "{ \"Key\": \"Value\" }");
		string readmePath = temp.WriteFile("README.md", "# MyApp\n\nSample readme content.");
		string scriptPath = temp.WriteFile("deploy.ps1", "Write-Host 'deploying'");
		string sqlPath = temp.WriteFile("schema.sql", "CREATE TABLE Orders (Id INT PRIMARY KEY);");

		var inputFileInfo = new FileInfo(csprojPath);
		var allFiles = new List<FileInfo> { new(csprojPath), new(programPath), new(configPath), new(readmePath) };

		return new ReportRenderRequest
		{
			ProjectName = "MyApp",
			InputFileInfo = inputFileInfo,
			RootDir = temp.RootDirectoryInfo,
			IsSolution = isSolution,
			Extension = ".csproj",
			Slim = slim,
			ExcludeTests = excludeTests,
			IncludeStats = includeStats,
			ScopeDir = scopeDir,
			ExcludeDirs = excludeDirs ?? [],
			AllFiles = allFiles,
			CodeFiles = [new FileInfo(programPath)],
			ConfigFiles = includeConfig ? [new FileInfo(configPath)] : [],
			ScriptFiles = includeScript ? [new FileInfo(scriptPath)] : [],
			SqlFiles = includeSql ? [new FileInfo(sqlPath)] : [],
			ReadmeFiles = includeReadme ? [new FileInfo(readmePath)] : [],
			ProjFiles = [new FileInfo(csprojPath)],
		};
	}

	static RenderedReport Render(ReportRenderRequest request)
		=> ReportRendererFactory.Create().Render(request);

	[Test]
	public void Render_HeaderShowsProjectName_AndKind()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false);

		string output = Render(request).Markdown;

		Assert.That(output, Does.StartWith("# MyApp - App Project"));
	}

	[Test]
	public void Render_SlimMode_AddsModeLabelAndNote()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false, slim: true);

		string output = Render(request).Markdown;

		Assert.That(output, Does.Contain("App Project (slim)"));
		Assert.That(output, Does.Contain("**Slim mode:** file contents are omitted"));
	}

	[Test]
	public void Render_SlimMode_ShowsFileSizeInsteadOfContent()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false, slim: true);

		string output = Render(request).Markdown;

		Assert.That(output, Does.Contain("_File size:"));
		Assert.That(output, Does.Not.Contain("WebApplication.CreateBuilder"));
	}

	[Test]
	public void Render_NonSlimMode_IncludesRealFileContent()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false, slim: false);

		string output = Render(request).Markdown;

		Assert.That(output, Does.Contain("WebApplication.CreateBuilder"));
		Assert.That(output, Does.Contain("Sample readme content."));
	}

	[Test]
	public void Render_TokenLine_ShowsTheReturnedEstimate()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false);

		var report = Render(request);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(report.Markdown, Does.Contain($"**Estimated tokens:** ~{report.EstimatedTokens:N0}"));
			Assert.That(report.EstimatedTokens, Is.GreaterThan(0));
		}
	}

	[Test]
	public void Render_TokenLine_NamesTheEstimationMethodWithoutAnEmDash()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false);

		string tokenLine = Render(request).Markdown
			.Split(Environment.NewLine)
			.Single(line => line.StartsWith("> **Estimated tokens:**", StringComparison.Ordinal));

		using (Assert.EnterMultipleScope())
		{
			Assert.That(tokenLine, Does.Contain("character count ÷ 4"));
			Assert.That(tokenLine, Does.Not.Contain("\u2014"));
		}
	}

	[Test]
	public void Render_FileContentContainingATokenLine_LeavesTheContentUntouched()
	{
		// Regression: the header used to be filled in with output.Replace, which also rewrote matching text inside dumped files.
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false);
		const string embeddedTokenLine = "> **Estimated tokens:** ~1  _(character count ÷ 4, treat as a rough guide)_";
		File.WriteAllText(temp.GetFullPath("Program.cs"), embeddedTokenLine);

		string output = Render(request).Markdown;

		Assert.That(output, Does.Contain(embeddedTokenLine));
	}

	[Test]
	public void Render_FlagsNote_ListsActiveFlags()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false, slim: true, excludeTests: true, scopeDir: "src", excludeDirs: ["wwwroot", "docs"]);

		string output = Render(request).Markdown;

		Assert.That(output, Does.Contain("**Flags:**"));
		Assert.That(output, Does.Contain("`--slim`"));
		Assert.That(output, Does.Contain("`--exclude-tests`"));
		Assert.That(output, Does.Contain("`--scope src`"));
		Assert.That(output, Does.Contain("`--exclude-dir wwwroot`"));
		Assert.That(output, Does.Contain("`--exclude-dir docs`"));
	}

	[Test]
	public void Render_NoFlagsSet_OmitsFlagsNote()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false);

		string output = Render(request).Markdown;

		Assert.That(output, Does.Not.Contain("**Flags:**"));
	}

	[Test]
	public void Render_IncludeStatsOff_OmitsStatisticsBlock()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false);

		string output = Render(request).Markdown;

		Assert.That(output, Does.Not.Contain("## Token Statistics"));
	}

	[Test]
	public void Render_IncludeStatsOn_PlacesStatisticsBlockBeforeProjectSummary()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false, includeStats: true);

		string output = Render(request).Markdown;

		Assert.That(output.IndexOf("## Token Statistics", StringComparison.Ordinal), Is.GreaterThan(-1));
		Assert.That(output.IndexOf("## Token Statistics", StringComparison.Ordinal), Is.LessThan(output.IndexOf("## Project Summary", StringComparison.Ordinal)));
	}

	[Test]
	public void Render_IncludeStatsOn_ListsFlagInFlagsNote()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false, includeStats: true);

		string output = Render(request).Markdown;

		Assert.That(output, Does.Contain("`--include-stats`"));
	}

	[Test]
	public void Render_StandardProject_RecordsEachRenderedFileUnderItsSection()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false);

		var fileSections = Render(request).Statistics.Files.ToDictionary(file => file.RelativePath, file => file.SectionName);

		Assert.That(fileSections, Is.EquivalentTo(new Dictionary<string, string>
		{
			["README.md"] = "Documentation",
			["MyApp.csproj"] = "Project Dependencies",
			["appsettings.json"] = "Configuration",
			["Program.cs"] = "App Code",
		}));
	}

	[Test]
	public void Render_StandardProject_RecordsSectionsInRenderOrder()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false);

		var sectionNames = Render(request).Statistics.Sections.Select(section => section.SectionName);

		Assert.That(sectionNames, Is.EqualTo(new[]
		{
			"Project Summary", "Project Structure", "Documentation", "Project Dependencies", "Configuration", "App Code",
		}));
	}

	[Test]
	public void Render_ProjectSummary_CountsFilesByExtension()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false);

		string output = Render(request).Markdown;

		Assert.That(output, Does.Contain("| .cs | 1 |"));
		Assert.That(output, Does.Contain("| .csproj | 1 |"));
		Assert.That(output, Does.Contain("| .json | 1 |"));
		Assert.That(output, Does.Contain("| .md | 1 |"));
	}

	[Test]
	public void Render_ProjectStructure_ListsPathsRelativeToRoot()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false);

		string output = Render(request).Markdown;

		Assert.That(output, Does.Contain("## Project Structure"));
		Assert.That(output, Does.Contain("Program.cs"));
		Assert.That(output, Does.Not.Contain(temp.RootPath)); // must be relative, not absolute
	}

	[Test]
	public void Render_SolutionConfiguration_OnlyAppearsWhenIsSolution()
	{
		using var temp = new TempProjectDirectory();
		var solutionRequest = BuildRequest(temp, isSolution: true);
		var projectRequest = BuildRequest(temp, isSolution: false);

		string solutionOutput = Render(solutionRequest).Markdown;
		string projectOutput = Render(projectRequest).Markdown;

		using (Assert.EnterMultipleScope())
		{
			Assert.That(solutionOutput, Does.Contain("## Solution Configuration"));
			Assert.That(projectOutput, Does.Not.Contain("## Solution Configuration"));
		}
	}

	[Test]
	public void Render_ProjectDependencies_IncludesCsprojContentAsXml()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false);

		string output = Render(request).Markdown;

		Assert.That(output, Does.Contain("## Project Dependencies"));
		Assert.That(output, Does.Contain("```xml"));
		Assert.That(output, Does.Contain("Microsoft.NET.Sdk"));
	}

	[Test]
	public void Render_ProjectDependencies_FencesPackageJsonAsJson()
	{
		using var temp = new TempProjectDirectory();
		string packageJsonPath = temp.WriteFile("package.json", """{ "name": "bookwings" }""");

		var request = new ReportRenderRequest
		{
			InputFileInfo = new FileInfo(packageJsonPath),
			RootDir = temp.RootDirectoryInfo,
			IsSolution = false,
			Extension = ".json",
			ProjectName = "bookwings",
			Slim = false,
			ExcludeTests = false,
			AllFiles = [new FileInfo(packageJsonPath)],
			CodeFiles = [],
			ConfigFiles = [],
			ScriptFiles = [],
			SqlFiles = [],
			ReadmeFiles = [],
			ProjFiles = [new FileInfo(packageJsonPath)],
		};

		string output = Render(request).Markdown;

		Assert.That(output, Does.Contain("```json"));
	}

	[Test]
	public void Render_Configuration_UsesCorrectLanguageForJson()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false);

		string output = Render(request).Markdown;

		Assert.That(output, Does.Contain("## Configuration"));
		Assert.That(output, Does.Contain("```json"));
	}

	[Test]
	public void Render_DatabaseSection_AppearsWithSqlFenceWhenSqlFilesPresent()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false, includeSql: true);

		string output = Render(request).Markdown;

		using (Assert.EnterMultipleScope())
		{
			Assert.That(output, Does.Contain("## Database"));
			Assert.That(output, Does.Contain("### schema.sql"));
			Assert.That(output, Does.Contain("```sql"));
			Assert.That(output, Does.Contain("CREATE TABLE Orders"));
		}
	}

	[Test]
	public void Render_NoSqlFiles_OmitsDatabaseSection()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false);

		string output = Render(request).Markdown;

		Assert.That(output, Does.Not.Contain("## Database"));
	}

	[Test]
	public void Render_DatabaseSection_SitsBetweenScriptsAndAppCode()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false, includeScript: true, includeSql: true);

		string output = Render(request).Markdown;

		Assert.That(output.IndexOf("## Scripts", StringComparison.Ordinal), Is.LessThan(output.IndexOf("## Database", StringComparison.Ordinal)));
		Assert.That(output.IndexOf("## Database", StringComparison.Ordinal), Is.LessThan(output.IndexOf("## App Code", StringComparison.Ordinal)));
	}

	[Test]
	public void Render_AppCode_UsesCorrectLanguageForCs()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false);

		string output = Render(request).Markdown;

		Assert.That(output, Does.Contain("## App Code"));
		Assert.That(output, Does.Contain("```csharp"));
	}

	[Test]
	public void Render_NoReadmeFiles_OmitsDocumentationSection()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false, includeReadme: false);

		string output = Render(request).Markdown;

		Assert.That(output, Does.Not.Contain("## Documentation"));
	}

	[Test]
	public void Render_NoConfigFiles_OmitsConfigurationSection()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false, includeConfig: false);

		string output = Render(request).Markdown;

		Assert.That(output, Does.Not.Contain("## Configuration"));
	}

	[Test]
	public void Render_ExternalReadme_ShowsFullPathAndSourcedFromOutsideNote()
	{
		using var temp = new TempProjectDirectory();

		// The README sits a level above RootDir, as the ancestor search would leave it.
		string externalReadmePath = temp.WriteFile("README.md", "# repo readme");

		string csprojPath = temp.WriteFile(Path.Combine("src", "MyApp.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
		string programPath = temp.WriteFile(Path.Combine("src", "Program.cs"), "var builder = WebApplication.CreateBuilder(args);");

		var request = new ReportRenderRequest
		{
			ProjectName = "MyApp",
			InputFileInfo = new FileInfo(csprojPath),
			RootDir = new DirectoryInfo(temp.GetFullPath("src")),
			IsSolution = false,
			Extension = ".csproj",
			Slim = false,
			ExcludeTests = false,
			ScopeDir = null,
			ExcludeDirs = [],
			AllFiles = [new FileInfo(csprojPath), new FileInfo(programPath)],
			CodeFiles = [new FileInfo(programPath)],
			ConfigFiles = [],
			ScriptFiles = [],
			SqlFiles = [],
			ReadmeFiles = [new FileInfo(externalReadmePath)],
			ProjFiles = [new FileInfo(csprojPath)],
		};

		string output = Render(request).Markdown;

		using (Assert.EnterMultipleScope())
		{
			Assert.That(output, Does.Contain("Sourced from outside the project tree"));
			Assert.That(output, Does.Contain(externalReadmePath));
			Assert.That(output, Does.Contain("repo readme"));
		}
	}

	[Test]
	public void Render_InTreeReadme_HasNoSourcedFromOutsideNote()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false);

		string output = Render(request).Markdown;

		Assert.That(output, Does.Not.Contain("Sourced from outside"));
	}

	[Test]
	public void Render_HeaderUsesProjectName_NotTheInputFileName()
	{
		// Regression: JS projects take package.json as input, so the file name
		// is never the project name.
		using var temp = new TempProjectDirectory();
		string packageJsonPath = temp.WriteFile("package.json", """{ "name": "bookwings" }""");

		var request = new ReportRenderRequest
		{
			InputFileInfo = new FileInfo(packageJsonPath),
			RootDir = new DirectoryInfo(temp.RootPath),
			IsSolution = false,
			Extension = ".json",
			ProjectName = "bookwings",
			Slim = false,
			ExcludeTests = false,
			ScopeDir = null,
			ExcludeDirs = [],
			AllFiles = [new FileInfo(packageJsonPath)],
			CodeFiles = [],
			ConfigFiles = [],
			ScriptFiles = [],
			SqlFiles = [],
			ReadmeFiles = [],
			ProjFiles = [new FileInfo(packageJsonPath)],
		};

		string output = Render(request).Markdown;

		Assert.That(output, Does.StartWith("# bookwings - App Project"));
	}

	[Test]
	public void Render_Scripts_UsesPowershellFenceAndSitsBetweenConfigAndCode()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false, includeScript: true);

		string output = Render(request).Markdown;

		using (Assert.EnterMultipleScope())
		{
			Assert.That(output, Does.Contain("## Scripts"));
			Assert.That(output, Does.Contain("```powershell"));
			Assert.That(output.IndexOf("## Scripts", StringComparison.Ordinal), Is.GreaterThan(output.IndexOf("## Configuration", StringComparison.Ordinal)));
			Assert.That(output.IndexOf("## Scripts", StringComparison.Ordinal), Is.LessThan(output.IndexOf("## App Code", StringComparison.Ordinal)));
		}
	}

	[Test]
	public void Render_NoScriptFiles_OmitsScriptsSection()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false);

		string output = Render(request).Markdown;

		Assert.That(output, Does.Not.Contain("## Scripts"));
	}
	[Test]
	public void Render_IncludeStatsOn_ExcludesPreambleFromStatistics()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false, slim: true, includeStats: true);

		var sectionNames = Render(request).Statistics.Sections.Select(section => section.SectionName).ToList();

		using (Assert.EnterMultipleScope())
		{
			Assert.That(sectionNames, Does.Not.Contain(ReportSectionNames.Flags));
			Assert.That(sectionNames, Does.Not.Contain(ReportSectionNames.TokenStatistics));
		}
	}

	[Test]
	public void Render_IncludeStatsOn_ExposesPreambleAndContentSectionsSeparately()
	{
		using var temp = new TempProjectDirectory();
		var request = BuildRequest(temp, isSolution: false, includeStats: true);

		var report = Render(request);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(report.PreambleSections.Select(section => section.Name), Is.EqualTo(new[] { ReportSectionNames.Flags, ReportSectionNames.TokenStatistics }));
			Assert.That(report.ContentSections.Select(section => section.Name), Does.Contain(ReportSectionNames.AppCode));
		}
	}
}