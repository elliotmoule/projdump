namespace projdump.Engine.Core;

// Same composition idea as IFileExclusionFilter, for test-file detection.
interface ITestFileDetector
{
    bool IsTestFile(FileInfo f);
}