namespace projdump.Engine.Shared;

static class FormatHelpers
{
    public static string FormatFileSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024):F1} MB",
    };

    public static string GetMarkdownLanguage(string extension) => extension.ToLower() switch
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