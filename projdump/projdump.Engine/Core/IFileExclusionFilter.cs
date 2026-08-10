namespace projdump.Engine.Core;

// A single exclusion rule; analyzers compose only the filters relevant to their stack.
interface IFileExclusionFilter
{
    bool IsExcluded(FileInfo f);
}