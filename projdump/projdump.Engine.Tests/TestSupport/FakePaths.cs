namespace projdump.Engine.Tests.TestSupport;

// FileInfo resolves a relative path against the current working directory,
// which for a test runner is typically a build output path like
// .../bin/Debug/net10.0/ - itself containing "bin", "Debug", etc. Any
// exclusion-filter test built on a relative path silently inherits whatever
// happens to be in that CWD. This anchors every fixture path under the
// system temp directory instead, so results only ever depend on the path
// the test actually wrote, never on where the test happened to run from.
static class FakePaths
{
    public static string Combine(params string[] segments) =>
        Path.Combine([Path.GetTempPath(), "projdump-fake-fixture", .. segments]);
}