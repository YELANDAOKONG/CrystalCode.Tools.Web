using CrystalCode.Web.Html;

namespace CrystalCode.Web.Tests.Html;

public sealed class HtmlTokenizerTests
{
    [Fact]
    public void ReadsTagsAndDecodesHref()
    {
        var tokens = HtmlTokenizer.Tokenize(
            "<a HREF=\"https://x?a=1&amp;b=2\" class=\"y\">t</a>");

        Assert.Equal(3, tokens.Count);
        Assert.Equal(HtmlTokenKind.StartTag, tokens[0].Kind);
        Assert.Equal("a", tokens[0].Name);
        Assert.Equal("https://x?a=1&b=2", tokens[0].Href);
        Assert.Equal(HtmlTokenKind.Text, tokens[1].Kind);
        Assert.Equal("t", tokens[1].Text);
        Assert.Equal(HtmlTokenKind.EndTag, tokens[2].Kind);
        Assert.Equal("a", tokens[2].Name);
    }

    [Fact]
    public void ReadsUnquotedHref()
    {
        var tokens = HtmlTokenizer.Tokenize("<a href=https://x/y>t</a>");

        Assert.Equal("https://x/y", tokens[0].Href);
    }

    [Fact]
    public void KeepsALoneLessThanAsText()
    {
        var tokens = HtmlTokenizer.Tokenize("1 < 2 and 3 > 2");

        Assert.Equal("1 < 2 and 3 > 2", string.Concat(tokens.Select(token => token.Text)));
    }

    [Fact]
    public void DropsCommentsAndDeclarations()
    {
        var tokens = HtmlTokenizer.Tokenize("<!doctype html><!-- note --><p>x</p>");

        Assert.Equal(
            [HtmlTokenKind.StartTag, HtmlTokenKind.Text, HtmlTokenKind.EndTag],
            tokens.Select(token => token.Kind));
        Assert.Equal("x", tokens[1].Text);
    }
}
