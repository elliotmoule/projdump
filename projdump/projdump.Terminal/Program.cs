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
        RunOptions? options = TryUseSavedCommand() ?? PromptForOptions();
        if (options == null) return;

        bool success = Execute(options);

        if (success)
            OfferToSaveCommand(options);
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

    internal static RunOptions? TryUseSavedCommand()
    {
        var history = CommandHistoryStore.Load(CommandHistoryFilePathOverride);
        if (history.Count == 0)
            return null;

        if (!PromptYesNo("Use a recently executed command? [y/N]: "))
            return null;

        var ordered = history.OrderByDescending(c => c.SavedAt).ToList();

        Console.WriteLine();
        for (int i = 0; i < ordered.Count; i++)
            Console.WriteLine($"  {i + 1}. [{ordered[i].SavedAt.ToLocalTime():yyyy-MM-dd HH:mm}] {DescribeCommand(ordered[i])}");
        Console.WriteLine();

        string choice = Prompt($"Pick a command (1-{ordered.Count}, blank = start fresh): ");
        if (!int.TryParse(choice, out int index) || index < 1 || index > ordered.Count)
            return null;

        Console.WriteLine();
        return ToRunOptions(ordered[index - 1]);
    }

    static string DescribeCommand(SavedCommand cmd)
    {
        var parts = new List<string> { cmd.InputPath };
        if (cmd.CustomOutputPath != null) parts.Add($"output={cmd.CustomOutputPath}");
        if (cmd.Slim) parts.Add("slim");
        if (cmd.ExcludeTests) parts.Add("exclude-tests");
        if (cmd.ScopeDir != null) parts.Add($"scope={cmd.ScopeDir}");
        if (cmd.ExcludeDirs.Count > 0) parts.Add($"exclude-dir={string.Join(",", cmd.ExcludeDirs)}");
        if (cmd.TypeArg != null) parts.Add($"type={cmd.TypeArg}");
        if (cmd.ModeArg != null) parts.Add($"mode={cmd.ModeArg}");
        return string.Join(" | ", parts);
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

    internal static void OfferToSaveCommand(RunOptions options)
    {
        if (!PromptYesNo("Save this command for next time? [y/N]: "))
            return;

        CommandHistoryStore.Save(ToSavedCommand(options), CommandHistoryFilePathOverride);
        Console.WriteLine("Saved.");
    }

    internal static RunOptions? PromptForOptions()
    {
        Console.WriteLine("projdump interactive mode (run with --help to see the non-interactive flags)");
        Console.WriteLine();

        string inputPath = Prompt("Path to .sln/.slnx/.csproj, a directory, or a Vue project directory: ").Trim('"');
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            PrintError("No path provided.");
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

    internal static bool Execute(RunOptions options)
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
            return false;
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

        return true;
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