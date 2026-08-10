namespace projdump.Engine.Core;

public sealed class FileEntry
{
    public required FileInfo File { get; init; }
    public required FileRole Role { get; init; }
}