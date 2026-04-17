using System.Text;

class Program
{
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

    static readonly string[] ExcludedFileSuffixes = [".designer.cs", ".g.cs", ".g.i.cs", ".min.js", ".min.css"];

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

    static readonly string[] EntryPointNames = ["Program.cs", "Startup.cs", "App.xaml.cs"];

    // This provides the priority for what files should be higher in the doc.
    static int CodeFilePriority(FileInfo f)
    {
        if (EntryPointNames.Contains(f.Name, StringComparer.OrdinalIgnoreCase)) return 0;
        if (f.Name.StartsWith('I') && char.IsUpper(f.Name.Length > 1 ? f.Name[1] : ' ')) return 1; // interfaces
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

    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Usage: projdump <path-to-solution-or-project> [output-path]");
            Console.WriteLine("Supported formats: .sln, .slnx, .csproj");
            Console.ResetColor();
            return;
        }

        string inputPath = args[0];
        string? customOutputPath = args.Length > 1 ? args[1] : null;
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

        bool isSolution = extension == ".sln" || extension == ".slnx";
        string outputFileName = isSolution ? "app-solution.md" : "app-project.md";

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

        // Get all the files.
        var allFiles = rootDir.GetFiles("*.*", SearchOption.AllDirectories)
            .Where(f =>
                !ExcludedPathSegments.Any(seg => f.FullName.Contains(seg)) &&
                !ExcludedFileNames.Contains(f.Name) &&
                !ExcludedFileSuffixes.Any(suffix => f.Name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            )
            .OrderBy(f => f.DirectoryName)
            .ThenBy(f => f.Name)
            .ToList();

        // Categorise
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

        // Build the md output
        StringBuilder sb = new();

        // Header
        sb.AppendLine($"# {inputFileInfo.Name} - {(isSolution ? "App Solution" : "App Project")}");
        sb.AppendLine();

        // Token estimate placeholder (filled in at the end)
        const string tokenPlaceholderLine = "%%TOKEN_ESTIMATE%%";
        sb.AppendLine(tokenPlaceholderLine);
        sb.AppendLine();

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
            sb.AppendLine($"```{slnLang}");
            sb.AppendLine(File.ReadAllText(inputPath).Trim());
            sb.AppendLine("```");
            sb.AppendLine();
        }

        // Project Dependencies
        sb.AppendLine("## Project Dependencies");
        foreach (var proj in projFiles)
        {
            sb.AppendLine($"### {proj.Name}");
            sb.AppendLine("```xml");
            sb.AppendLine(File.ReadAllText(proj.FullName).Trim());
            sb.AppendLine("```");
        }
        sb.AppendLine();

        // Configuration Files
        if (configFiles.Count > 0)
        {
            sb.AppendLine("## Configuration");
            foreach (var file in configFiles)
            {
                string relativePath = Path.GetRelativePath(rootDir.FullName, file.FullName);
                string lang = GetMarkdownLanguage(file.Extension);
                sb.AppendLine($"### {file.Name}");
                sb.AppendLine($"**Path:** `{relativePath}`");
                sb.AppendLine();
                sb.AppendLine($"```{lang}");
                sb.AppendLine(File.ReadAllText(file.FullName).Trim());
                sb.AppendLine("```");
                sb.AppendLine();
            }
        }

        // App Code
        sb.AppendLine("## App Code");
        sb.AppendLine();
        foreach (var file in codeFiles)
        {
            string relativePath = Path.GetRelativePath(rootDir.FullName, file.FullName);
            string lang = GetMarkdownLanguage(file.Extension);
            sb.AppendLine($"### {file.Name}");
            sb.AppendLine($"**Path:** `{relativePath}`");
            sb.AppendLine();
            sb.AppendLine($"```{lang}");
            sb.AppendLine(File.ReadAllText(file.FullName).Trim());
            sb.AppendLine("```");
            sb.AppendLine();
        }

        // Token estimate (rough heuristic: GPT/Claude tokenisers average ~4 chars per token for code)
        string output = sb.ToString();
        int estimatedTokens = (int)Math.Ceiling(output.Length / 4.0);
        string tokenLine = $"> **Estimated tokens:** ~{estimatedTokens:N0}  _(character count ÷ 4 — treat as a rough guide)_";
        output = output.Replace(tokenPlaceholderLine, tokenLine);

        File.WriteAllText(outputPath, output);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Success! Context generated at: {outputPath}");
        Console.WriteLine($"Estimated tokens: ~{estimatedTokens:N0}");
        Console.ResetColor();
    }

    // Helper for translating file extensions
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
}