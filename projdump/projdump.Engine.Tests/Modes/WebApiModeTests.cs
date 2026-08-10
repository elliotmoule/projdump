using projdump.Engine.Core;
using projdump.Engine.Modes;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Modes;

[TestFixture]
public class WebApiModeTests
{
    [Test]
    public void Apply_DropsComponentStyleAndAssetFiles()
    {
        var analysis = ProjectAnalysisFactory.Create(
            ProjectAnalysisFactory.Entry("Program.cs", FileRole.EntryPoint),
            ProjectAnalysisFactory.Entry("OrdersController.cs", FileRole.ApiSurface),
            ProjectAnalysisFactory.Entry("MainWindow.xaml", FileRole.Component),
            ProjectAnalysisFactory.Entry("site.css", FileRole.Style),
            ProjectAnalysisFactory.Entry("logo.png", FileRole.Asset));

        var result = new WebApiMode().Apply(analysis);

        var remainingNames = result.AllFiles.Select(f => f.File.Name).ToList();
        Assert.That(remainingNames, Is.EquivalentTo(["Program.cs", "OrdersController.cs"]));
    }

    [Test]
    public void Apply_KeepsTestFiles_RegardlessOfMode()
    {
        // --exclude-tests is the only thing that should ever drop Test files.
        var analysis = ProjectAnalysisFactory.Create(
            ProjectAnalysisFactory.Entry("OrderServiceTests.cs", FileRole.Test));

        var result = new WebApiMode().Apply(analysis);

        Assert.That(result.AllFiles, Has.Count.EqualTo(1));
    }

    [Test]
    public void Apply_KeepsOtherRoleFiles_LikeServicesAndInterfaces()
    {
        // Exclude-list, not keep-list: most real API logic falls under Other.
        var analysis = ProjectAnalysisFactory.Create(
            ProjectAnalysisFactory.Entry("OrderService.cs", FileRole.Other));

        var result = new WebApiMode().Apply(analysis);

        Assert.That(result.AllFiles, Has.Count.EqualTo(1));
    }

    [Test]
    public void ModeKey_IsWebapi()
    {
        Assert.That(new WebApiMode().ModeKey, Is.EqualTo("webapi"));
    }
}