using projdump.Engine.Core;

namespace projdump.Engine.Tests.Core.TestDoubles;

sealed class FakeExclusionFilter(bool excludeAll) : IFileExclusionFilter
{
    public bool IsExcluded(FileInfo f) => excludeAll;
}