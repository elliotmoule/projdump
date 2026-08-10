namespace projdump.Engine.Core;

sealed class CompositeExclusionFilter : IFileExclusionFilter
{
    readonly IReadOnlyList<IFileExclusionFilter> _filters;

    public CompositeExclusionFilter(params IFileExclusionFilter[] filters)
    {
        _filters = filters;
    }

    public bool IsExcluded(FileInfo f) => _filters.Any(filter => filter.IsExcluded(f));
}