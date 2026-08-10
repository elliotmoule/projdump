namespace projdump.Engine.Core.Filters;

// Minified vendor assets are noise regardless of stack - a single shared
// filter every analyzer composes, so a rule like this can't quietly end up
// scoped to only one stack the way it previously did with Vue only.
sealed class MinifiedAssetExclusionFilter : IFileExclusionFilter
{
    static readonly string[] ExcludedFileSuffixes = [".min.js", ".min.css"];

    public bool IsExcluded(FileInfo f) => ExcludedFileSuffixes.Any(suffix => f.Name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
}