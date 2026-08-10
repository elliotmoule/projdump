using projdump.Engine.Core;

namespace projdump.Engine.Tests.TestSupport;

static class ProjectAnalysisFactory
{
    public static ProjectAnalysis Create(params FileEntry[] allFiles) => new()
    {
        InputFileInfo = new FileInfo("MyApp.csproj"),
        RootDir = new DirectoryInfo("MyApp"),
        IsSolution = false,
        Extension = ".csproj",
        ProjectName = "MyApp",
        AllFiles = allFiles,
        CodeFiles = allFiles,
        ConfigFiles = [],
        ReadmeFiles = [],
        ProjFiles = [],
    };

    public static FileEntry Entry(string fileName, FileRole role) => new() { File = new FileInfo(fileName), Role = role };
}