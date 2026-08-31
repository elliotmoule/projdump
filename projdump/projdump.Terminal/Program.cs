using projdump.Engine.Analyzers.CSharp;
using projdump.Engine.Analyzers.Vue;
using projdump.Engine.Core;
using projdump.Engine.Modes;
using projdump.Engine.Rendering;
using projdump.Shared;

class Program
{
    internal sealed record RunOptions(
        string InputPath,
        string? CustomOutputPath,
        bool Slim,
        bool ExcludeTests,
        string? ScopeDir,
        string? TypeArg,
        string? ModeArg,
        IReadOnlyList<string> ExcludeDirs);

	internal enum RecentMenuAction { RunSelected, EnterNewCommand, Quit }

	internal sealed record RecentMenuResult(RecentMenuAction Action, RunOptions? Options);

	internal sealed record ExecutionResult(bool Success, string? SolutionName, string? ProjectName)
	{
		public static readonly ExecutionResult Failed = new(false, null, null);
	}

	// Test-only override point - if set, used instead of resolving the real Desktop folder.
	internal static string? DefaultOutputDirectoryOverride;

    // Test-only override point - if set, used instead of the real %APPDATA% history file.
    internal static string? CommandHistoryFilePathOverride;

    static void PrintUsage()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Usage: projdump <path> [output-path] [options]");
        Console.WriteLine("       projdump                          (no args = interactive mode)");
        Console.WriteLine();
        Console.WriteLine("Supported input:");
        Console.WriteLine("  .sln, .slnx, .csproj              C# solution or project");
        Console.WriteLine("  <directory>                        C# project (auto-discovers a top-level .sln/.slnx),");
        Console.WriteLine("                                     or a Vue project (auto-detected via a 'vue' dependency)");
        Console.WriteLine("  package.json                       Vue project");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --slim             Omit file contents; list filenames and sizes only");
        Console.WriteLine("  --exclude-tests    Exclude test projects and test files");
        Console.WriteLine("  --scope <dir>      Restrict to a subdirectory (relative to project root)");
        Console.WriteLine("  --exclude-dir <name>      Exclude a directory by name, anywhere in the tree (repeatable)");
        Console.WriteLine("  --type <csharp|vue>       Force project type (auto-detected by default)");
        Console.WriteLine("  --mode <default|webapi>   Report focus mode (default: full dump)");
        Console.WriteLine("  --help, -h         Show this help");
        Console.WriteLine();
        Console.WriteLine("If no output path is given, the report is written to your Desktop.");
        Console.WriteLine("Interactive mode will also offer to reuse or save commands between runs.");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  projdump MyApp.sln");
        Console.WriteLine("  projdump MyApp.sln output/context.md --slim");
        Console.WriteLine("  projdump MyApp.sln --exclude-tests --scope src/MyApp.Api");
        Console.WriteLine("  projdump MyApp.Api.csproj --mode webapi");
        Console.WriteLine("  projdump MyApp.Api.csproj --mode webapi --exclude-dir wwwroot");
        Console.WriteLine("  projdump ./frontend");
        Console.WriteLine("  projdump ./MySolutionFolder");
        Console.ResetColor();
    }

    static void PrintError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {message}");
        Console.ResetColor();
    }

    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            RunInteractive();
            return;
        }

        RunOptions? options = ParseArgs(args);
        if (options == null) return;
        Execute(options);
    }

	static void RunInteractive()
	{
		Console.WriteLine("projdump interactive mode (run with --help to see the non-interactive flags)");
		Console.WriteLine();

		while (true)
		{
			RecentMenuResult menu = ShowRecentCommands();
			if (menu.Action == RecentMenuAction.Quit)
				return;

			bool reusedFromHistory = menu.Action == RecentMenuAction.RunSelected;

			RunOptions? options = reusedFromHistory ? menu.Options : PromptForOptions();
			if (options == null)
				return; // A blank path at the fresh-command prompt is the way out when history is empty.

			ExecutionResult result = Execute(options);
			if (result.Success)
				RecordCommandUse(options, result, offerToSave: !reusedFromHistory);

			Console.WriteLine();
		}
	}

	internal static RunOptions? ParseArgs(string[] args)
    {
        bool slim = false;
        bool excludeTests = false;
        string? scopeDir = null;
        string? typeArg = null;
        string? modeArg = null;
        var excludeDirs = new List<string>();

        var positional = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slim":
                    slim = true;
                    break;
                case "--exclude-tests":
                    excludeTests = true;
                    break;
                case "--scope":
                    if (i + 1 >= args.Length) { PrintError("--scope requires a directory argument."); return null; }
                    scopeDir = args[++i];
                    break;
                case "--exclude-dir":
                    if (i + 1 >= args.Length) { PrintError("--exclude-dir requires a directory name."); return null; }
                    excludeDirs.Add(args[++i]);
                    break;
                case "--type":
                    if (i + 1 >= args.Length) { PrintError("--type requires a value."); return null; }
                    typeArg = args[++i];
                    break;
                case "--mode":
                    if (i + 1 >= args.Length) { PrintError("--mode requires a value."); return null; }
                    modeArg = args[++i];
                    break;
                case "--help":
                case "-h":
                    PrintUsage();
                    return null;
                default:
                    positional.Add(args[i]);
                    break;
            }
        }

        if (positional.Count == 0)
        {
            PrintUsage();
            return null;
        }

        return new RunOptions(
            InputPath: positional[0],
            CustomOutputPath: positional.Count > 1 ? positional[1] : null,
            Slim: slim,
            ExcludeTests: excludeTests,
            ScopeDir: scopeDir,
            TypeArg: typeArg,
            ModeArg: modeArg,
            ExcludeDirs: excludeDirs);
    }

	/// <summary>
	/// Lists recently used commands and asks which one to run.
	/// </summary>
	/// <returns>The chosen command, or a request to enter a new one or quit.</returns>
	internal static RecentMenuResult ShowRecentCommands()
	{
		var history = CommandHistoryStore.Load(CommandHistoryFilePathOverride);
		if (history.Count == 0)
			return new RecentMenuResult(RecentMenuAction.EnterNewCommand, null);

		Console.WriteLine("Recently used commands:");
		Console.WriteLine();
		for (int position = 1; position <= history.Count; position++)
			WriteRecentCommandLine(position, history[position - 1]);
		Console.WriteLine();

		string choice = Prompt($"Pick a command (1-{history.Count}, blank = new command, q = quit): ");
		Console.WriteLine();

		if (choice.Equals("q", StringComparison.OrdinalIgnoreCase))
			return new RecentMenuResult(RecentMenuAction.Quit, null);

		return int.TryParse(choice, out int index) && index >= 1 && index <= history.Count
			? new RecentMenuResult(RecentMenuAction.RunSelected, ToRunOptions(history[index - 1]))
			: new RecentMenuResult(RecentMenuAction.EnterNewCommand, null);
	}

	static void WriteRecentCommandLine(int position, SavedCommand command)
	{
		var (solutionName, projectName) = ResolveMenuNames(command);

		WriteColored($"  {position}. ", ConsoleColor.Red);
		Console.Write($"[{command.SavedAt.ToLocalTime():yyyy-MM-dd HH:mm}] ");

		if (solutionName != null)
			WriteColored(solutionName, ConsoleColor.DarkYellow);
		if (solutionName != null && projectName != null)
			Console.Write("/");
		if (projectName != null)
			WriteColored(projectName, ConsoleColor.Yellow);

		string flags = FormatFlags(command);
		if (flags.Length > 0)
		{
			Console.Write(" | ");
			WriteColored(flags, ConsoleColor.Green);
		}

		if (command.CustomOutputPath != null)
		{
			Console.Write(" | ");
			WriteColored(command.CustomOutputPath, ConsoleColor.Cyan);
		}

		Console.WriteLine();
	}

	static void WriteColored(string text, ConsoleColor color)
	{
		Console.ForegroundColor = color;
		Console.Write(text);
		Console.ResetColor();
	}

	static string FormatFlags(SavedCommand command)
	{
		var flags = new List<string>();
		if (command.Slim) flags.Add("--slim");
		if (command.ExcludeTests) flags.Add("--exclude-tests");
		if (command.ScopeDir != null) flags.Add($"--scope {command.ScopeDir}");
		foreach (string excludedDir in command.ExcludeDirs) flags.Add($"--exclude-dir {excludedDir}");
		if (command.TypeArg != null) flags.Add($"--type {command.TypeArg}");
		if (command.ModeArg != null) flags.Add($"--mode {command.ModeArg}");
		return string.Join(" ", flags);
	}

	static (string? SolutionName, string? ProjectName) ResolveMenuNames(SavedCommand command)
	{
		if (command.SolutionName != null || command.ProjectName != null)
			return (command.SolutionName, command.ProjectName);

		// History entries saved before display names were stored - fall back to the input path.
		string trimmedPath = command.InputPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		string fallbackName = Path.GetFileNameWithoutExtension(trimmedPath);
		if (string.IsNullOrEmpty(fallbackName))
			fallbackName = trimmedPath;

		bool looksLikeSolution = trimmedPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
								 || trimmedPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase);

		return looksLikeSolution ? (fallbackName, null) : (null, fallbackName);
	}

	internal static SavedCommand ToSavedCommand(RunOptions options) => new(
        DateTimeOffset.Now,
        options.InputPath,
        options.CustomOutputPath,
        options.Slim,
        options.ExcludeTests,
        options.ScopeDir,
        options.TypeArg,
        options.ModeArg,
        options.ExcludeDirs);

    internal static RunOptions ToRunOptions(SavedCommand cmd) => new(
        cmd.InputPath,
        cmd.CustomOutputPath,
        cmd.Slim,
        cmd.ExcludeTests,
        cmd.ScopeDir,
        cmd.TypeArg,
        cmd.ModeArg,
        cmd.ExcludeDirs);

	/// <summary>
	/// Promotes a command to the top of the recently used list, asking first only when it is
	/// genuinely new. Reused and already-stored commands are promoted silently.
	/// </summary>
	/// <param name="options">The command that was just run.</param>
	/// <param name="result">The execution result carrying the resolved display names.</param>
	/// <param name="offerToSave">False when the command came straight from the recent list.</param>
	internal static void RecordCommandUse(RunOptions options, ExecutionResult result, bool offerToSave)
	{
		SavedCommand command = ToSavedCommand(options, result.SolutionName, result.ProjectName);
		bool alreadyInHistory = CommandHistoryStore.FindMatch(command, CommandHistoryFilePathOverride) != null;

		if (offerToSave && !alreadyInHistory && !PromptYesNo("Save this command for next time? [y/N]: "))
			return;

		CommandHistoryStore.RecordUse(command, CommandHistoryFilePathOverride);
	}

	internal static SavedCommand ToSavedCommand(RunOptions options, string? solutionName = null, string? projectName = null) => new(
		DateTimeOffset.Now,
		options.InputPath,
		options.CustomOutputPath,
		options.Slim,
		options.ExcludeTests,
		options.ScopeDir,
		options.TypeArg,
		options.ModeArg,
		options.ExcludeDirs,
		solutionName,
		projectName);

	internal static RunOptions? PromptForOptions()
    {
        string inputPath = Prompt("Path to .sln/.slnx/.csproj, a directory, or a Vue project directory: ").Trim('"');
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            return null;
        }

        bool isSolutionInput = inputPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
                                inputPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase);

        string? customOutputPath = OrNull(Prompt("Output path (blank = your Desktop): "));
        bool slim = PromptYesNo("Slim mode - omit file contents? [y/N]: ");
        bool excludeTests = PromptYesNo("Exclude test files? [y/N]: ");
        string? scopeDir = OrNull(Prompt("Scope to a subdirectory (blank = whole project): "));
        string? excludeDirsInput = OrNull(Prompt("Exclude directories, comma-separated e.g. wwwroot,docs (blank = none): "));
        var excludeDirs = excludeDirsInput == null
            ? []
            : excludeDirsInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        string? typeArg = OrNull(Prompt("Project type - csharp/vue (blank = auto-detect): "));

        string? modeArg;
        if (isSolutionInput)
        {
            Console.WriteLine("Solution detected - mode applies per-project, so skipping that question.");
            modeArg = null;
        }
        else
        {
            modeArg = OrNull(Prompt("Mode - default/webapi (blank = default): "));
        }

        Console.WriteLine();

        return new RunOptions(inputPath, customOutputPath, slim, excludeTests, scopeDir, typeArg, modeArg, excludeDirs);
    }

    static string Prompt(string label)
    {
        Console.Write(label);
        return (Console.ReadLine() ?? "").Trim();
    }

    static bool PromptYesNo(string label)
    {
        string answer = Prompt(label);
        return answer.Equals("y", StringComparison.OrdinalIgnoreCase) || answer.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    static string? OrNull(string value) => value.Length > 0 ? value : null;

    internal static ExecutionResult Execute(RunOptions options)
    {
        string modeKey = options.ModeArg ?? "default";

        var registry = new ProjectTypeRegistry([new CSharpAnalyzer(), new VueProjectAnalyzer()]);

        ProjectAnalysis analysis;
        try
        {
            var analyzer = registry.Resolve(options.InputPath, options.TypeArg);
            ProjectTypeRegistry.ValidateMode(analyzer, modeKey);

            var analysisOptions = new ProjectAnalysisOptions { ExcludeTests = options.ExcludeTests, ScopeDir = options.ScopeDir, ExcludeDirs = options.ExcludeDirs };
            analysis = analyzer.Analyze(options.InputPath, analysisOptions);

            IDumpMode mode = modeKey switch
            {
                "default" => new DefaultMode(),
                "webapi" => new WebApiMode(),
                _ => throw new ProjectAnalysisException($"Mode '{modeKey}' has no implementation yet."),
            };
            analysis = mode.Apply(analysis);
        }
		catch (ProjectAnalysisException ex)
		{
			PrintError(ex.Message);
			return ExecutionResult.Failed;
		}

		string modeSuffix = options.Slim ? "-slim" : "";
        string projectKind = analysis.IsSolution ? "app-solution" : "app-project";
        string outputFileName = $"{SanitizeForFileName(analysis.ProjectName)}-{projectKind}{modeSuffix}.md";

        string outputPath;
        if (options.CustomOutputPath != null)
        {
            bool looksLikeDir = string.IsNullOrEmpty(Path.GetExtension(options.CustomOutputPath))
                                || options.CustomOutputPath.EndsWith(Path.DirectorySeparatorChar)
                                || options.CustomOutputPath.EndsWith(Path.AltDirectorySeparatorChar);

            outputPath = looksLikeDir
                ? Path.Combine(options.CustomOutputPath, outputFileName)
                : options.CustomOutputPath;

            string? outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(outputDir);
        }
        else
        {
            outputPath = Path.Combine(ResolveDefaultOutputDirectory(analysis.RootDir), outputFileName);
        }

        var renderRequest = new ReportRenderRequest
        {
            InputFileInfo = analysis.InputFileInfo,
            RootDir = analysis.RootDir,
            IsSolution = analysis.IsSolution,
            Extension = analysis.Extension,
            Slim = options.Slim,
            ExcludeTests = options.ExcludeTests,
            ScopeDir = options.ScopeDir,
            ExcludeDirs = options.ExcludeDirs,
            AllFiles = [.. analysis.AllFiles.Select(e => e.File)],
            CodeFiles = [.. analysis.CodeFiles.Select(e => e.File)],
            ConfigFiles = [.. analysis.ConfigFiles.Select(e => e.File)],
            ReadmeFiles = [.. analysis.ReadmeFiles.Select(e => e.File)],
            ProjFiles = [.. analysis.ProjFiles.Select(e => e.File)],
        };

        var (output, estimatedTokens) = MarkdownReportRenderer.Render(renderRequest);

        File.WriteAllText(outputPath, output);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Success! Context generated at: {outputPath}");
        Console.Write($"Estimated tokens: ~{estimatedTokens:N0}");
        if (options.Slim) Console.Write("  (slim mode — run without --slim for full file contents)");
        Console.WriteLine();
        Console.ResetColor();

		var (solutionName, projectName) = ResolveDisplayNames(analysis);
		return new ExecutionResult(true, solutionName, projectName);
	}

	// Vue has no solution concept, so the folder holding package.json stands in for one.
	// InputFileInfo is used rather than RootDir because --scope reassigns RootDir.
	static (string? SolutionName, string? ProjectName) ResolveDisplayNames(ProjectAnalysis analysis)
	{
		if (analysis.IsSolution)
			return (analysis.ProjectName, null);

		bool isVueProject = analysis.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase);
		if (isVueProject)
		{
			string? containingFolderName = analysis.InputFileInfo.Directory?.Name;
			bool nameMatchesFolder = string.Equals(containingFolderName, analysis.ProjectName, StringComparison.OrdinalIgnoreCase);

			return nameMatchesFolder
				? (containingFolderName, null)
				: (containingFolderName, analysis.ProjectName);
		}

		return (FindOwningSolutionName(analysis.InputFileInfo.Directory), analysis.ProjectName);
	}

	/// <summary>
	/// Looks for the solution a project belongs to by searching its ancestor directories.
	/// </summary>
	/// <param name="projectDir">The directory containing the project file.</param>
	/// <returns>The solution file name without its extension, or null when none is found nearby.</returns>
	static string? FindOwningSolutionName(DirectoryInfo? projectDir)
	{
		DirectoryInfo? currentDir = projectDir;

		for (int level = 0; level <= AncestorReadmeLocator.MaxSearchDepth && currentDir != null; level++)
		{
			string? solutionName = FindSolutionNameInDirectory(currentDir);
			if (solutionName != null)
				return solutionName;

			// Matches the readme walk: a repository root is as far up as a project's context goes.
			if (IsRepositoryRoot(currentDir))
				return null;

			currentDir = currentDir.Parent;
		}

		return null;
	}

	static string? FindSolutionNameInDirectory(DirectoryInfo dir)
	{
		if (!dir.Exists)
			return null;

		try
		{
			FileInfo? solutionFile = dir
				.EnumerateFiles("*.sln*", SearchOption.TopDirectoryOnly)
				.Where(file => file.Extension.Equals(".sln", StringComparison.OrdinalIgnoreCase)
							|| file.Extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase))
				.OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
				.FirstOrDefault();

			return solutionFile == null ? null : Path.GetFileNameWithoutExtension(solutionFile.Name);
		}
		catch (UnauthorizedAccessException)
		{
			return null;
		}
	}

	// Worktrees and submodules use a .git file rather than a directory, so both count.
	static bool IsRepositoryRoot(DirectoryInfo dir)
	{
		string gitPath = Path.Combine(dir.FullName, ".git");
		return Directory.Exists(gitPath) || File.Exists(gitPath);
	}

	// Falls back to the project's own root directory if the Desktop can't be
	// resolved (e.g. no Desktop special folder on the current platform).
	static string ResolveDefaultOutputDirectory(DirectoryInfo projectRootDir)
    {
        if (DefaultOutputDirectoryOverride != null)
            return DefaultOutputDirectoryOverride;

        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        return string.IsNullOrEmpty(desktopPath) ? projectRootDir.FullName : desktopPath;
    }

    internal static string SanitizeForFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string([.. name.Select(c => invalid.Contains(c) ? '-' : c)]);
        return string.IsNullOrWhiteSpace(sanitized) ? "project" : sanitized;
    }
}