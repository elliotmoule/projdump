using System.Text;

class Program
{
    #region Exclusions
    static readonly HashSet<string> ExcludedFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "AssemblyInfo.cs",
        "GlobalUsings.cs",
        "GlobalUsings.g.cs",
    };

    static readonly string[] ExcludedPathSegments =
    [
        $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}.vs{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}.vscode{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}",
    ];

    // Files whose names end with these suffixes are likely auto-generated
    static readonly string[] ExcludedFileSuffixes = [".designer.cs", ".g.cs", ".g.i.cs", ".min.js", ".min.css"];

    // Folder name segments that indicate a test project
    static readonly string[] TestPathSegments =
    [
        $"{Path.DirectorySeparatorChar}Tests{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}Test{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}Specs{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}UnitTests{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}IntegrationTests{Path.DirectorySeparatorChar}",
    ];
    #endregion

    #region Config files
    // Config files to capture
    static readonly HashSet<string> ConfigFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "appsettings.json",
        "appsettings.Development.json",
        "appsettings.Production.json",
        "appsettings.Staging.json",
        "web.config",
        "app.config",
        "launchSettings.json",
        ".env.example",
        ".env.template",
        "dockerfile",
        "docker-compose.yml",
        "docker-compose.yaml",
    };
    #endregion

    // Code file ordering — lower index = higher priority (appears earlier)
    static readonly string[] EntryPointNames = ["Program.cs", "Startup.cs", "App.xaml.cs"];

    static int CodeFilePriority(FileInfo f)
    {
        if (EntryPointNames.Contains(f.Name, StringComparer.OrdinalIgnoreCase)) return 0;
        if (f.Name.StartsWith('I') && char.IsUpper(f.Name.Length > 1 ? f.Name[1] : ' ')) return 1; // IFoo interfaces
        if (f.Name.EndsWith("Interface.cs", StringComparison.OrdinalIgnoreCase)) return 1;
        if (f.Name.EndsWith("Model.cs", StringComparison.OrdinalIgnoreCase)) return 2;
        if (f.Name.EndsWith("Models.cs", StringComparison.OrdinalIgnoreCase)) return 2;
        if (f.Name.EndsWith("Entity.cs", StringComparison.OrdinalIgnoreCase)) return 2;
        if (f.Name.EndsWith("Dto.cs", StringComparison.OrdinalIgnoreCase)) return 2;
        if (f.Name.EndsWith("Enum.cs", StringComparison.OrdinalIgnoreCase)) return 2;
        if (f.Name.EndsWith("Enums.cs", StringComparison.OrdinalIgnoreCase)) return 2;
        if (f.Name.EndsWith("Constants.cs", StringComparison.OrdinalIgnoreCase)) return 3;
        if (f.Name.EndsWith("Extension.cs", StringComparison.OrdinalIgnoreCase)) return 4;
        if (f.Name.EndsWith("Extensions.cs", StringComparison.OrdinalIgnoreCase)) return 4;
        if (f.Name.EndsWith("Helper.cs", StringComparison.OrdinalIgnoreCase)) return 4;
        if (f.Name.EndsWith("Helpers.cs", StringComparison.OrdinalIgnoreCase)) return 4;
        return 5; // everything else
    }

    #region Helpers
    static bool IsTestFile(FileInfo f) =>
    TestPathSegments.Any(seg => f.FullName.Contains(seg)) ||
    f.Name.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase) ||
    f.Name.EndsWith("Test.cs", StringComparison.OrdinalIgnoreCase) ||
    f.Name.EndsWith("Spec.cs", StringComparison.OrdinalIgnoreCase) ||
    f.Name.EndsWith("Specs.cs", StringComparison.OrdinalIgnoreCase) ||
    (f.Extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase) && (
        f.Name.Contains("Test", StringComparison.OrdinalIgnoreCase) ||
        f.Name.Contains("Spec", StringComparison.OrdinalIgnoreCase)
    ));

    static string FormatFileSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024):F1} MB",
    };

    static string GetMarkdownLanguage(string extension) => extension.ToLower() switch
    {
        ".cs" => "csharp",
        ".xaml" or ".csproj" or ".slnx" => "xml",
        ".xml" or ".config" or ".app" => "xml",
        ".cshtml" => "razor",
        ".css" => "css",
        ".js" => "javascript",
        ".ts" => "typescript",
        ".json" => "json",
        ".yml" or ".yaml" => "yaml",
        _ => "text"
    };

    static void PrintUsage()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Usage: projdump <path-to-solution-or-project> [output-path] [options]");
        Console.WriteLine();
        Console.WriteLine("Supported input formats: .sln, .slnx, .csproj");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --slim             Omit file contents; list filenames and sizes only");
        Console.WriteLine("  --exclude-tests    Exclude test projects and test files");
        Console.WriteLine("  --scope <dir>      Restrict to a subdirectory (relative to solution/project root)");
        Console.WriteLine("  --help, -h         Show this help");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  projdump MyApp.sln");
        Console.WriteLine("  projdump MyApp.sln output/context.md --slim");
        Console.WriteLine("  projdump MyApp.sln --exclude-tests --scope src/MyApp.Api");
        Console.ResetColor();
    }
    #endregion

    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return;
        }

        // Positional: first non-flag arg = input path, second = output path
        // Flags: --slim, --exclude-tests, --scope <relative-dir>
        bool slim = false;
        bool excludeTests = false;
        string? scopeDir = null;

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
                    if (i + 1 >= args.Length)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error: --scope requires a directory argument.");
                        Console.ResetColor();
                        return;
                    }
                    scopeDir = args[++i];
                    break;
                case "--help":
                case "-h":
                    PrintUsage();
                    return;
                default:
                    positional.Add(args[i]);
                    break;
            }
        }

        if (positional.Count == 0)
        {
            PrintUsage();
            return;
        }

        string inputPath = positional[0];
        string? customOutputPath = positional.Count > 1 ? positional[1] : null;
        string extension = Path.GetExtension(inputPath).ToLower();
        bool isValidExtension = extension == ".sln" || extension == ".slnx" || extension == ".csproj";

        if (!File.Exists(inputPath) || !isValidExtension)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: '{inputPath}' is not a valid or existing .sln, .slnx, or .csproj file.");
            Console.ResetColor();
            return;
        }

        FileInfo inputFileInfo = new(inputPath);
        DirectoryInfo rootDir = inputFileInfo.Directory!;
        if (rootDir == null) return;

        // Apply --scope: restrict file discovery to a subdirectory
        if (scopeDir != null)
        {
            string scopedPath = Path.GetFullPath(Path.Combine(rootDir.FullName, scopeDir));
            if (!Directory.Exists(scopedPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: --scope directory '{scopeDir}' does not exist under '{rootDir.FullName}'.");
                Console.ResetColor();
                return;
            }
            rootDir = new DirectoryInfo(scopedPath);
        }

        bool isSolution = extension == ".sln" || extension == ".slnx";
        string modeSuffix = slim ? "-slim" : "";
        string outputFileName = isSolution ? $"app-solution{modeSuffix}.md" : $"app-project{modeSuffix}.md";

        // Resolve output path
        string outputPath;
        if (customOutputPath != null)
        {
            bool looksLikeDir = string.IsNullOrEmpty(Path.GetExtension(customOutputPath))
                                || customOutputPath.EndsWith(Path.DirectorySeparatorChar)
                                || customOutputPath.EndsWith(Path.AltDirectorySeparatorChar);

            outputPath = looksLikeDir
                ? Path.Combine(customOutputPath, outputFileName)
                : customOutputPath;

            string? outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(outputDir);
        }
        else
        {
            outputPath = Path.Combine(rootDir.FullName, outputFileName);
        }

        // Gather all files
        var allFiles = rootDir.GetFiles("*.*", SearchOption.AllDirectories)
            .Where(f =>
                !ExcludedPathSegments.Any(seg => f.FullName.Contains(seg)) &&
                !ExcludedFileNames.Contains(f.Name) &&
                !ExcludedFileSuffixes.Any(suffix => f.Name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) &&
                !(excludeTests && IsTestFile(f))
            )
            .OrderBy(f => f.DirectoryName)
            .ThenBy(f => f.Name)
            .ToList();

        // Categorise files
        var codeExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".cs", ".xaml", ".cshtml", ".css", ".js", ".ts" };
        var configExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".json", ".xml", ".config", ".yml", ".yaml", ".env" };

        var codeFiles = allFiles
            .Where(f => codeExtensions.Contains(f.Extension))
            .OrderBy(CodeFilePriority)
            .ThenBy(f => f.DirectoryName)
            .ThenBy(f => f.Name)
            .ToList();

        var configFiles = allFiles
            .Where(f => ConfigFileNames.Contains(f.Name) ||
                        (configExtensions.Contains(f.Extension) && f.Name.StartsWith("appsettings", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var readmeFiles = allFiles
            .Where(f => f.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var projFiles = isSolution
            ? allFiles.Where(f => f.Extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase)).ToList()
            : [inputFileInfo];

        // Build md output
        StringBuilder sb = new();

        // Header
        string modeLabel = slim ? " (slim)" : "";
        sb.AppendLine($"# {inputFileInfo.Name} - {(isSolution ? "App Solution" : "App Project")}{modeLabel}");
        sb.AppendLine();

        if (slim)
        {
            sb.AppendLine("> **Slim mode:** file contents are omitted. Each entry shows the file name, path, and size.");
            sb.AppendLine();
        }

        // Token estimate placeholder — filled in at the end
        const string tokenPlaceholderLine = "%%TOKEN_ESTIMATE%%";
        sb.AppendLine(tokenPlaceholderLine);
        sb.AppendLine();

        // Active flags note
        var activeFlags = new List<string>();
        if (slim) activeFlags.Add("`--slim`");
        if (excludeTests) activeFlags.Add("`--exclude-tests`");
        if (scopeDir != null) activeFlags.Add($"`--scope {scopeDir}`");
        if (activeFlags.Count > 0)
        {
            sb.AppendLine($"> **Flags:** {string.Join(", ", activeFlags)}");
            sb.AppendLine();
        }

        // File Summary Table
        sb.AppendLine("## Project Summary");
        var stats = allFiles
            .GroupBy(f => f.Extension.ToLower())
            .Select(g => new { Ext = string.IsNullOrEmpty(g.Key) ? "No Ext" : g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count);

        sb.AppendLine("| File Extension | Count |");
        sb.AppendLine("| :--- | :--- |");
        foreach (var stat in stats)
            sb.AppendLine($"| {stat.Ext} | {stat.Count} |");
        sb.AppendLine();

        // File Structure
        sb.AppendLine("## Project Structure");
        sb.AppendLine("```text");
        foreach (var file in allFiles)
            sb.AppendLine(Path.GetRelativePath(rootDir.FullName, file.FullName));
        sb.AppendLine("```");
        sb.AppendLine();

        // README / Documentation
        if (readmeFiles.Count > 0)
        {
            sb.AppendLine("## Documentation");
            foreach (var file in readmeFiles)
            {
                string relativePath = Path.GetRelativePath(rootDir.FullName, file.FullName);
                sb.AppendLine($"### {file.Name}");
                sb.AppendLine($"**Path:** `{relativePath}`");
                sb.AppendLine();
                if (slim)
                    sb.AppendLine($"_File size: {FormatFileSize(file.Length)}_");
                else
                    sb.AppendLine(File.ReadAllText(file.FullName).Trim());
                sb.AppendLine();
            }
        }

        // Solution Configuration
        if (isSolution)
        {
            sb.AppendLine("## Solution Configuration");
            string slnLang = extension == ".slnx" ? "xml" : "text";
            sb.AppendLine($"### {inputFileInfo.Name}");
            sb.AppendLine($"**Path:** `{inputFileInfo.Name}`");
            sb.AppendLine();
            if (slim)
            {
                sb.AppendLine($"_File size: {FormatFileSize(inputFileInfo.Length)}_");
            }
            else
            {
                sb.AppendLine($"```{slnLang}");
                sb.AppendLine(File.ReadAllText(inputPath).Trim());
                sb.AppendLine("```");
            }
            sb.AppendLine();
        }

        // Project Dependencies
        sb.AppendLine("## Project Dependencies");
        foreach (var proj in projFiles)
        {
            string relativePath = Path.GetRelativePath(rootDir.FullName, proj.FullName);
            sb.AppendLine($"### {proj.Name}");
            sb.AppendLine($"**Path:** `{relativePath}`");
            sb.AppendLine();
            if (slim)
            {
                sb.AppendLine($"_File size: {FormatFileSize(proj.Length)}_");
            }
            else
            {
                sb.AppendLine("```xml");
                sb.AppendLine(File.ReadAllText(proj.FullName).Trim());
                sb.AppendLine("```");
            }
        }
        sb.AppendLine();

        // Configuration Files
        if (configFiles.Count > 0)
        {
            sb.AppendLine("## Configuration");
            foreach (var file in configFiles)
            {
                string relativePath = Path.GetRelativePath(rootDir.FullName, file.FullName);
                sb.AppendLine($"### {file.Name}");
                sb.AppendLine($"**Path:** `{relativePath}`");
                sb.AppendLine();
                if (slim)
                {
                    sb.AppendLine($"_File size: {FormatFileSize(file.Length)}_");
                }
                else
                {
                    string lang = GetMarkdownLanguage(file.Extension);
                    sb.AppendLine($"```{lang}");
                    sb.AppendLine(File.ReadAllText(file.FullName).Trim());
                    sb.AppendLine("```");
                }
                sb.AppendLine();
            }
        }

        // App Code
        sb.AppendLine("## App Code");
        sb.AppendLine();
        foreach (var file in codeFiles)
        {
            string relativePath = Path.GetRelativePath(rootDir.FullName, file.FullName);
            sb.AppendLine($"### {file.Name}");
            sb.AppendLine($"**Path:** `{relativePath}`");
            sb.AppendLine();
            if (slim)
            {
                sb.AppendLine($"_File size: {FormatFileSize(file.Length)}_");
            }
            else
            {
                string lang = GetMarkdownLanguage(file.Extension);
                sb.AppendLine($"```{lang}");
                sb.AppendLine(File.ReadAllText(file.FullName).Trim());
                sb.AppendLine("```");
            }
            sb.AppendLine();
        }

        // Token estimate (Rough heuristic: GPT/Claude tokenisers average ~4 chars per token for code)
        string output = sb.ToString();
        int estimatedTokens = (int)Math.Ceiling(output.Length / 4.0);
        string tokenLine = $"> **Estimated tokens:** ~{estimatedTokens:N0}  _(character count ÷ 4 — treat as a rough guide)_";
        output = output.Replace(tokenPlaceholderLine, tokenLine);

        File.WriteAllText(outputPath, output);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Success! Context generated at: {outputPath}");
        Console.Write($"Estimated tokens: ~{estimatedTokens:N0}");
        if (slim) Console.Write("  (slim mode — run without --slim for full file contents)");
        Console.WriteLine();
        Console.ResetColor();
    }
}