namespace projdump.Engine.Core;

sealed class CompositeTestFileDetector : ITestFileDetector
{
    readonly IReadOnlyList<ITestFileDetector> _detectors;

    public CompositeTestFileDetector(params ITestFileDetector[] detectors)
    {
        _detectors = detectors;
    }

    public bool IsTestFile(FileInfo f) => _detectors.Any(detector => detector.IsTestFile(f));
}