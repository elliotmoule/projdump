using projdump.Engine.Core;

namespace projdump.Engine.Analyzers.CSharp;

static class CSharpFileClassifier
{
    static readonly string[] EntryPointNames = ["Program.cs", "Startup.cs", "App.xaml.cs"];

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

    static readonly HashSet<string> ConfigExtensions = new(StringComparer.OrdinalIgnoreCase) { ".json", ".xml", ".config", ".yml", ".yaml", ".env" };
    static readonly HashSet<string> CodeExtensions = new(StringComparer.OrdinalIgnoreCase) { ".cs", ".xaml", ".cshtml", ".css", ".js", ".ts" };

    public static bool IsCodeFile(FileInfo f) => CodeExtensions.Contains(f.Extension);

    public static bool IsConfigFile(FileInfo f) =>
        ConfigFileNames.Contains(f.Name) ||
        (ConfigExtensions.Contains(f.Extension) && f.Name.StartsWith("appsettings", StringComparison.OrdinalIgnoreCase));

    // Lower index = higher priority (appears earlier).
    public static int CodeFilePriority(FileInfo f)
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

    // ApiSurface is a filename/folder heuristic, not attribute-parsed - not a guarantee.
    public static FileRole AssignRole(FileInfo f, ITestFileDetector testFileDetector)
    {
        if (testFileDetector.IsTestFile(f))
            return FileRole.Test;

        if (f.Extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase) ||
            f.Extension.Equals(".sln", StringComparison.OrdinalIgnoreCase) ||
            f.Extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase))
            return FileRole.Build;

        if (f.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
            return FileRole.Doc;

        if (IsConfigFile(f))
            return FileRole.Config;

        if (EntryPointNames.Contains(f.Name, StringComparer.OrdinalIgnoreCase))
            return FileRole.EntryPoint;

        if (f.Extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
        {
            bool inControllersFolder = f.DirectoryName != null && (
                f.DirectoryName.Contains($"{Path.DirectorySeparatorChar}Controllers{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                f.DirectoryName.EndsWith($"{Path.DirectorySeparatorChar}Controllers", StringComparison.OrdinalIgnoreCase));
            bool inEndpointsFolder = f.DirectoryName != null && (
                f.DirectoryName.Contains($"{Path.DirectorySeparatorChar}Endpoints{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                f.DirectoryName.EndsWith($"{Path.DirectorySeparatorChar}Endpoints", StringComparison.OrdinalIgnoreCase));

            if (inControllersFolder || inEndpointsFolder ||
                f.Name.EndsWith("Controller.cs", StringComparison.OrdinalIgnoreCase) ||
                f.Name.EndsWith("Endpoint.cs", StringComparison.OrdinalIgnoreCase) ||
                f.Name.EndsWith("Endpoints.cs", StringComparison.OrdinalIgnoreCase))
                return FileRole.ApiSurface;

            if (f.Name.EndsWith("Model.cs", StringComparison.OrdinalIgnoreCase) ||
                f.Name.EndsWith("Models.cs", StringComparison.OrdinalIgnoreCase) ||
                f.Name.EndsWith("Entity.cs", StringComparison.OrdinalIgnoreCase) ||
                f.Name.EndsWith("Dto.cs", StringComparison.OrdinalIgnoreCase) ||
                f.Name.EndsWith("Enum.cs", StringComparison.OrdinalIgnoreCase) ||
                f.Name.EndsWith("Enums.cs", StringComparison.OrdinalIgnoreCase))
                return FileRole.Model;

            return FileRole.Other;
        }

        if (f.Extension.Equals(".xaml", StringComparison.OrdinalIgnoreCase) || f.Extension.Equals(".cshtml", StringComparison.OrdinalIgnoreCase))
            return FileRole.Component;

        if (f.Extension.Equals(".css", StringComparison.OrdinalIgnoreCase))
            return FileRole.Style;

        return FileRole.Other;
    }
}