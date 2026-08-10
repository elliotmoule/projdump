using projdump.Engine.Analyzers.CSharp;
using projdump.Engine.Analyzers.Vue;
using projdump.Engine.Core;
using projdump.Engine.Modes;
using projdump.Engine.Rendering;

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

    static void PrintUsage()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Usage: projdump <path> [output-path] [options]");
        Console.WriteLine("       projdump                          (no args = interactive mode)");
        Console.WriteLine();
        Console.WriteLine("Supported input:");
        Console.WriteLine("  .sln, .slnx, .csproj              C# solution or project");
        Console.WriteLine("  <directory> or package.json        Vue project (auto-detected via a 'vue' dependency)");
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
        Console.WriteLine("Examples:");
        Console.WriteLine("  projdump MyApp.sln");
        Console.WriteLine("  projdump MyApp.sln output/context.md --slim");
        Console.WriteLine("  projdump MyApp.sln --exclude-tests --scope src/MyApp.Api");
        Console.WriteLine("  projdump MyApp.Api.csproj --mode webapi");
        Console.WriteLine("  projdump MyApp.Api.csproj --mode webapi --exclude-dir wwwroot");
        Console.WriteLine("  projdump ./frontend");
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
        RunOptions? options = args.Length == 0 ? PromptForOptions() : ParseArgs(args);
        if (options == null) return;
        Execute(options);
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

    static RunOptions? PromptForOptions()
    {
        Console.WriteLine("projdump interactive mode (run with --help to see the non-interactive flags)");
        Console.WriteLine();

        string inputPath = Prompt("Path to .sln/.slnx/.csproj, or a Vue project directory: ").Trim('"');
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            PrintError("No path provided.");
            return null;
        }

        string? customOutputPath = OrNull(Prompt("Output path (blank = alongside the project): "));
        bool slim = PromptYesNo("Slim mode - omit file contents? [y/N]: ");
        bool excludeTests = PromptYesNo("Exclude test files? [y/N]: ");
        string? scopeDir = OrNull(Prompt("Scope to a subdirectory (blank = whole project): "));
        string? excludeDirsInput = OrNull(Prompt("Exclude directories, comma-separated e.g. wwwroot,docs (blank = none): "));
        var excludeDirs = excludeDirsInput == null
            ? []
            : excludeDirsInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        string? typeArg = OrNull(Prompt("Project type - csharp/vue (blank = auto-detect): "));
        string? modeArg = OrNull(Prompt("Mode - default/webapi (blank = default): "));

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

    internal static void Execute(RunOptions options)
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
            return;
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
            outputPath = Path.Combine(analysis.RootDir.FullName, outputFileName);
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
    }

    internal static string SanitizeForFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string([.. name.Select(c => invalid.Contains(c) ? '-' : c)]);
        return string.IsNullOrWhiteSpace(sanitized) ? "project" : sanitized;
    }
}