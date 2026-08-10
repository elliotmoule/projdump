using projdump.Engine.Core;

namespace projdump.Engine.Modes;

// Exclude-list, not keep-list (most C# API logic falls under FileRole.Other); Test is excluded via --exclude-tests only.
public sealed class WebApiMode : IDumpMode
{
    public string ModeKey => "webapi";

    static readonly HashSet<FileRole> ExcludedRoles = [FileRole.Component, FileRole.Style];

    public ProjectAnalysis Apply(ProjectAnalysis analysis)
    {
        var allFiles = Filter(analysis.AllFiles);
        var codeFiles = Filter(analysis.CodeFiles);
        var configFiles = Filter(analysis.ConfigFiles);
        var readmeFiles = Filter(analysis.ReadmeFiles);
        var projFiles = Filter(analysis.ProjFiles);

        return analysis.WithFiles(allFiles, codeFiles, configFiles, readmeFiles, projFiles);
    }

    static List<FileEntry> Filter(IReadOnlyList<FileEntry> files) =>
        [.. files.Where(f => !ExcludedRoles.Contains(f.Role))];
}