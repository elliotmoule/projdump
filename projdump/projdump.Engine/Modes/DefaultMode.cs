using projdump.Engine.Core;

namespace projdump.Engine.Modes;

public sealed class DefaultMode : IDumpMode
{
    public string ModeKey => "default";

    public ProjectAnalysis Apply(ProjectAnalysis analysis) => analysis;
}