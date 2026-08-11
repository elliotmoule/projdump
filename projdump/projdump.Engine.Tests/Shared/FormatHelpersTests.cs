using projdump.Engine.Shared;

namespace projdump.Engine.Tests.Shared;

[TestFixture]
public class FormatHelpersTests
{
    [TestCase(0, "0 B")]
    [TestCase(512, "512 B")]
    [TestCase(1023, "1023 B")]
    [TestCase(1024, "1.0 KB")]
    [TestCase(1536, "1.5 KB")]
    [TestCase(1024 * 1024, "1.0 MB")]
    [TestCase(5 * 1024 * 1024, "5.0 MB")]
    public void FormatFileSize_ReturnsExpectedString(long bytes, string expected)
    {
        Assert.That(FormatHelpers.FormatFileSize(bytes), Is.EqualTo(expected));
    }

    [TestCase(".cs", "csharp")]
    [TestCase(".CS", "csharp")]
    [TestCase(".xaml", "xml")]
    [TestCase(".csproj", "xml")]
    [TestCase(".slnx", "xml")]
    [TestCase(".cshtml", "razor")]
    [TestCase(".css", "css")]
    [TestCase(".scss", "scss")]
    [TestCase(".sass", "sass")]
    [TestCase(".less", "less")]
    [TestCase(".js", "javascript")]
    [TestCase(".jsx", "jsx")]
    [TestCase(".ts", "typescript")]
    [TestCase(".tsx", "tsx")]
    [TestCase(".vue", "vue")]
    [TestCase(".json", "json")]
    [TestCase(".yml", "yaml")]
    [TestCase(".yaml", "yaml")]
    [TestCase(".unknown", "text")]
    public void GetMarkdownLanguage_ReturnsExpectedLanguage(string extension, string expected)
    {
        Assert.That(FormatHelpers.GetMarkdownLanguage(extension), Is.EqualTo(expected));
    }
}