namespace projdump.Engine.Core;

// Semantic role used by Modes to filter files, independent of project type.
public enum FileRole
{
    EntryPoint,
    ApiSurface,
    Model,
    Config,
    Doc,
    Component,
    Style,
    Test,
    Build,
    Other,
}