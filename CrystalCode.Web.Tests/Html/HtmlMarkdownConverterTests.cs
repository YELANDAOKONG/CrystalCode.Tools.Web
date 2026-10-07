using CrystalCode.Web.Html;

namespace CrystalCode.Web.Tests.Html;

public sealed class HtmlMarkdownConverterTests
{
    [Fact]
    public void ConvertsHeadingsAndEmphasis()
    {
        var markdown = HtmlMarkdownConverter.Convert(
            "<h1>Title</h1><p>Hello <b>world</b> and <em>friends</em>.</p>");

        Assert.Equal("# Title\n\nHello **world** and *friends*.", markdown);
    }

    [Fact]
    public void BreaksBlockElements()
    {
        var markdown = HtmlMarkdownConverter.Convert("<div>one</div><div>two</div>");

        Assert.Equal("one\n\ntwo", markdown);
    }

    [Fact]
    public void RendersListItems()
    {
        var markdown = HtmlMarkdownConverter.Convert("<ul><li>alpha</li><li>beta</li></ul>");

        Assert.Equal("- alpha\n\n- beta", markdown);
    }

    [Fact]
    public void RendersLinks()
    {
        var markdown = HtmlMarkdownConverter.Convert(
            "<p>See <a href=\"https://example.com/x\">the docs</a> now.</p>");

        Assert.Equal("See [the docs](https://example.com/x) now.", markdown);
    }

    [Fact]
    public void RendersALinkWithoutHrefAsPlainBrackets()
    {
        var markdown = HtmlMarkdownConverter.Convert("<p>no href <a>plain</a></p>");

        Assert.Equal("no href [plain]", markdown);
    }

    [Fact]
    public void SkipsScriptAndStyleElements()
    {
        var markdown = HtmlMarkdownConverter.Convert(
            "<script>var x = 1;</script><style>p{color:red}</style><p>visible</p>");

        Assert.Equal("visible", markdown);
    }

    [Fact]
    public void KeepsPreformattedText()
    {
        var markdown = HtmlMarkdownConverter.Convert(
            "<pre><code>a &lt; b &amp;&amp; c</code></pre>");

        Assert.Equal("```\na < b && c\n```", markdown);
    }

    [Fact]
    public void RendersLineBreaks()
    {
        var markdown = HtmlMarkdownConverter.Convert("<p>line one<br>line two</p>");

        Assert.Equal("line one\nline two", markdown);
    }

    [Fact]
    public void RendersHorizontalRules()
    {
        var markdown = HtmlMarkdownConverter.Convert("<hr><p>after</p>");

        Assert.Equal("---\n\nafter", markdown);
    }

    [Fact]
    public void DecodesEntities()
    {
        var markdown = HtmlMarkdownConverter.Convert(
            "<p>A &amp; B &mdash; C &quot;quoted&quot; 'x'</p>");

        Assert.Equal("A & B — C \"quoted\" 'x'", markdown);
    }

    [Fact]
    public void KeepsTableCellsWithoutSeparators()
    {
        var markdown = HtmlMarkdownConverter.Convert(
            "<table><tr><td>cell 1</td><td>cell 2</td></tr></table>");

        Assert.Equal("cell 1cell 2", markdown);
    }

    [Fact]
    public void CollapsesWhitespace()
    {
        var markdown = HtmlMarkdownConverter.Convert("<p>  spaced   out\n  text </p>");

        Assert.Equal("spaced out text", markdown);
    }
}
