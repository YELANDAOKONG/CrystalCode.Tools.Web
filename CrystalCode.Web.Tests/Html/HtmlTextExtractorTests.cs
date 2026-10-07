using CrystalCode.Web.Html;

namespace CrystalCode.Web.Tests.Html;

public sealed class HtmlTextExtractorTests
{
    [Fact]
    public void ExtractsVisibleTextWithoutSeparators()
    {
        var text = HtmlTextExtractor.ExtractText(
            "<h1>Title</h1><p>Hello <b>world</b> and <em>friends</em>.</p>");

        Assert.Equal("TitleHello world and friends.", text);
    }

    [Fact]
    public void SkipsScriptAndStyleElements()
    {
        var text = HtmlTextExtractor.ExtractText(
            "<script>var x = 1;</script><style>p{color:red}</style><p>visible</p>");

        Assert.Equal("visible", text);
    }

    [Fact]
    public void KeepsPreformattedText()
    {
        var text = HtmlTextExtractor.ExtractText("<pre><code>a &lt; b &amp;&amp; c</code></pre>");

        Assert.Equal("a < b && c", text);
    }

    [Fact]
    public void CollapsesWhitespace()
    {
        var text = HtmlTextExtractor.ExtractText("<p>  spaced   out\n  text </p>");

        Assert.Equal("spaced out text", text);
    }
}
