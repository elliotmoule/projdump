using projdump.Engine.Core;

namespace projdump.Engine.Modes;

public interface IDumpMode
{
    string ModeKey { get; }
    ProjectAnalysis Apply(ProjectAnalysis analysis);
}