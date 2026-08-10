using projdump.Engine.Core;

namespace projdump.Engine.Tests.Core.TestDoubles;

sealed class FakeTestFileDetector(bool isTest) : ITestFileDetector
{
    public bool IsTestFile(FileInfo f) => isTest;
}