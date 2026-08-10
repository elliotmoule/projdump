using projdump.Engine.Analyzers.Vue;
using projdump.Engine.Core;
using projdump.Engine.Tests.Core.TestDoubles;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Analyzers.Vue;

[TestFixture]
public class VueFileClassifierTests
{
    static readonly ITestFileDetector NeverTest = new FakeTestFileDetector(false);
    static readonly ITestFileDetector AlwaysTest = new FakeTestFileDetector(true);

    [TestCase("main.js")]
    [TestCase("main.ts")]
    [TestCase("App.vue")]
    public void CodeFilePriority_ReturnsZero_ForEntryPoints(string fileName)
    {
        Assert.That(VueFileClassifier.CodeFilePriority(new FileInfo(fileName)), Is.EqualTo(0));
    }

    [Test]
    public void CodeFilePriority_ReturnsOne_ForRouterFolder()
    {
        var path = FakePaths.Combine("src", "router", "index.ts");
        Assert.That(VueFileClassifier.CodeFilePriority(new FileInfo(path)), Is.EqualTo(1));
    }

    [Test]
    public void CodeFilePriority_ReturnsOne_ForStoresFolder()
    {
        var path = FakePaths.Combine("src", "stores", "user.ts");
        Assert.That(VueFileClassifier.CodeFilePriority(new FileInfo(path)), Is.EqualTo(1));
    }

    [Test]
    public void CodeFilePriority_ReturnsTwo_ForTypesFile()
    {
        Assert.That(VueFileClassifier.CodeFilePriority(new FileInfo("OrderTypes.ts")), Is.EqualTo(2));
    }

    [Test]
    public void CodeFilePriority_ReturnsThree_ForFileDirectlyInComponentsFolder()
    {
        // Regression test: components/composables previously only matched a
        // nested subfolder, not a file placed directly in the folder.
        var path = FakePaths.Combine("src", "components", "OrderList.vue");
        Assert.That(VueFileClassifier.CodeFilePriority(new FileInfo(path)), Is.EqualTo(3));
    }

    [Test]
    public void CodeFilePriority_ReturnsThree_ForNestedComponentsSubfolder()
    {
        var path = FakePaths.Combine("src", "components", "forms", "OrderForm.vue");
        Assert.That(VueFileClassifier.CodeFilePriority(new FileInfo(path)), Is.EqualTo(3));
    }

    [Test]
    public void CodeFilePriority_ReturnsFour_ForFileDirectlyInComposablesFolder()
    {
        var path = FakePaths.Combine("src", "composables", "useOrders.ts");
        Assert.That(VueFileClassifier.CodeFilePriority(new FileInfo(path)), Is.EqualTo(4));
    }

    [Test]
    public void CodeFilePriority_ReturnsFive_ForEverythingElse()
    {
        var path = FakePaths.Combine("src", "utils", "format.ts");
        Assert.That(VueFileClassifier.CodeFilePriority(new FileInfo(path)), Is.EqualTo(5));
    }

    [Test]
    public void AssignRole_ReturnsTest_WhenDetectorSaysSo()
    {
        Assert.That(VueFileClassifier.AssignRole(new FileInfo("OrderList.spec.ts"), AlwaysTest), Is.EqualTo(FileRole.Test));
    }

    [Test]
    public void AssignRole_ReturnsBuild_ForPackageJson()
    {
        Assert.That(VueFileClassifier.AssignRole(new FileInfo("package.json"), NeverTest), Is.EqualTo(FileRole.Build));
    }

    [Test]
    public void AssignRole_ReturnsDoc_ForMarkdownFiles()
    {
        Assert.That(VueFileClassifier.AssignRole(new FileInfo("README.md"), NeverTest), Is.EqualTo(FileRole.Doc));
    }

    [Test]
    public void AssignRole_ReturnsConfig_ForViteConfig()
    {
        Assert.That(VueFileClassifier.AssignRole(new FileInfo("vite.config.ts"), NeverTest), Is.EqualTo(FileRole.Config));
    }

    [Test]
    public void AssignRole_ReturnsEntryPoint_ForMainTs()
    {
        Assert.That(VueFileClassifier.AssignRole(new FileInfo("main.ts"), NeverTest), Is.EqualTo(FileRole.EntryPoint));
    }

    [Test]
    public void AssignRole_ReturnsComponent_ForVueFile()
    {
        Assert.That(VueFileClassifier.AssignRole(new FileInfo("OrderList.vue"), NeverTest), Is.EqualTo(FileRole.Component));
    }

    [TestCase("site.css")]
    [TestCase("site.scss")]
    [TestCase("site.sass")]
    [TestCase("site.less")]
    public void AssignRole_ReturnsStyle_ForStylesheetFiles(string fileName)
    {
        Assert.That(VueFileClassifier.AssignRole(new FileInfo(fileName), NeverTest), Is.EqualTo(FileRole.Style));
    }

    [Test]
    public void AssignRole_ReturnsOther_ForUnclassifiedFile()
    {
        Assert.That(VueFileClassifier.AssignRole(new FileInfo("useOrders.ts"), NeverTest), Is.EqualTo(FileRole.Other));
    }

    [TestCase("App.vue", true)]
    [TestCase("main.ts", true)]
    [TestCase("site.css", true)]
    [TestCase("package.json", false)]
    [TestCase("README.md", false)]
    public void IsCodeFile_MatchesExpectedExtensions(string fileName, bool expected)
    {
        Assert.That(VueFileClassifier.IsCodeFile(new FileInfo(fileName)), Is.EqualTo(expected));
    }

    [TestCase("vite.config.ts", true)]
    [TestCase("tsconfig.json", true)]
    [TestCase(".env", true)]
    [TestCase("main.ts", false)]
    public void IsConfigFile_MatchesKnownConfigFiles(string fileName, bool expected)
    {
        Assert.That(VueFileClassifier.IsConfigFile(new FileInfo(fileName)), Is.EqualTo(expected));
    }
}