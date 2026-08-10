using projdump.Engine.Analyzers.CSharp;
using projdump.Engine.Core;
using projdump.Engine.Tests.Core.TestDoubles;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Analyzers.CSharp;

[TestFixture]
public class CSharpFileClassifierTests
{
    static readonly ITestFileDetector NeverTest = new FakeTestFileDetector(false);
    static readonly ITestFileDetector AlwaysTest = new FakeTestFileDetector(true);

    [TestCase("Program.cs", 0)]
    [TestCase("Startup.cs", 0)]
    [TestCase("IOrderService.cs", 1)]
    [TestCase("PaymentInterface.cs", 1)]
    [TestCase("OrderModel.cs", 2)]
    [TestCase("OrderDto.cs", 2)]
    [TestCase("StatusEnum.cs", 2)]
    [TestCase("AppConstants.cs", 3)]
    [TestCase("StringExtensions.cs", 4)]
    [TestCase("DateHelper.cs", 4)]
    [TestCase("OrderService.cs", 5)]
    public void CodeFilePriority_OrdersByFileNameConvention(string fileName, int expectedPriority)
    {
        Assert.That(CSharpFileClassifier.CodeFilePriority(new FileInfo(fileName)), Is.EqualTo(expectedPriority));
    }

    [Test]
    public void AssignRole_ReturnsTest_WhenDetectorSaysSo()
    {
        var role = CSharpFileClassifier.AssignRole(new FileInfo("OrderServiceTests.cs"), AlwaysTest);

        Assert.That(role, Is.EqualTo(FileRole.Test));
    }

    [TestCase("MyApp.csproj")]
    [TestCase("MyApp.sln")]
    [TestCase("MyApp.slnx")]
    public void AssignRole_ReturnsBuild_ForProjectAndSolutionFiles(string fileName)
    {
        Assert.That(CSharpFileClassifier.AssignRole(new FileInfo(fileName), NeverTest), Is.EqualTo(FileRole.Build));
    }

    [Test]
    public void AssignRole_ReturnsDoc_ForMarkdownFiles()
    {
        Assert.That(CSharpFileClassifier.AssignRole(new FileInfo("README.md"), NeverTest), Is.EqualTo(FileRole.Doc));
    }

    [Test]
    public void AssignRole_ReturnsConfig_ForAppSettings()
    {
        Assert.That(CSharpFileClassifier.AssignRole(new FileInfo("appsettings.json"), NeverTest), Is.EqualTo(FileRole.Config));
    }

    [Test]
    public void AssignRole_ReturnsEntryPoint_ForProgramCs()
    {
        Assert.That(CSharpFileClassifier.AssignRole(new FileInfo("Program.cs"), NeverTest), Is.EqualTo(FileRole.EntryPoint));
    }

    [Test]
    public void AssignRole_ReturnsApiSurface_ForControllerFile()
    {
        Assert.That(CSharpFileClassifier.AssignRole(new FileInfo("OrdersController.cs"), NeverTest), Is.EqualTo(FileRole.ApiSurface));
    }

    [Test]
    public void AssignRole_ReturnsApiSurface_ForFileDirectlyInControllersFolder()
    {
        var path = FakePaths.Combine("src", "Controllers", "Weird.cs");
        Assert.That(CSharpFileClassifier.AssignRole(new FileInfo(path), NeverTest), Is.EqualTo(FileRole.ApiSurface));
    }

    [Test]
    public void AssignRole_ReturnsModel_ForDtoFile()
    {
        Assert.That(CSharpFileClassifier.AssignRole(new FileInfo("OrderDto.cs"), NeverTest), Is.EqualTo(FileRole.Model));
    }

    [Test]
    public void AssignRole_ReturnsComponent_ForXamlFile()
    {
        Assert.That(CSharpFileClassifier.AssignRole(new FileInfo("MainWindow.xaml"), NeverTest), Is.EqualTo(FileRole.Component));
    }

    [Test]
    public void AssignRole_ReturnsStyle_ForCssFile()
    {
        Assert.That(CSharpFileClassifier.AssignRole(new FileInfo("site.css"), NeverTest), Is.EqualTo(FileRole.Style));
    }

    [Test]
    public void AssignRole_ReturnsOther_ForUnclassifiedCsFile()
    {
        Assert.That(CSharpFileClassifier.AssignRole(new FileInfo("OrderService.cs"), NeverTest), Is.EqualTo(FileRole.Other));
    }

    [TestCase("Program.cs", true)]
    [TestCase("site.css", true)]
    [TestCase("appsettings.json", false)]
    [TestCase("README.md", false)]
    public void IsCodeFile_MatchesExpectedExtensions(string fileName, bool expected)
    {
        Assert.That(CSharpFileClassifier.IsCodeFile(new FileInfo(fileName)), Is.EqualTo(expected));
    }

    [TestCase("appsettings.json", true)]
    [TestCase("appsettings.Development.json", true)]
    [TestCase("web.config", true)]
    [TestCase("launchSettings.json", true)]
    [TestCase("Program.cs", false)]
    public void IsConfigFile_MatchesKnownConfigFiles(string fileName, bool expected)
    {
        Assert.That(CSharpFileClassifier.IsConfigFile(new FileInfo(fileName)), Is.EqualTo(expected));
    }
}