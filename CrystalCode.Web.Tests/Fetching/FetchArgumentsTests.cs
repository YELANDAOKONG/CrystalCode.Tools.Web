using CrystalCode.Web.Fetching;

namespace CrystalCode.Web.Tests.Fetching;

public sealed class FetchArgumentsTests
{
    [Fact]
    public void ReadsDefaults()
    {
        Assert.True(
            FetchArguments.TryRead("""{"url":"https://example.com/a"}""", out var request, out var error),
            error);
        Assert.Equal("https://example.com/a", request.Url.ToString());
        Assert.Equal(PageFormat.Markdown, request.Format);
        Assert.Equal(30, request.TimeoutSeconds);
        Assert.Null(request.MaxCharacters);
    }

    [Fact]
    public void ReadsEveryArgument()
    {
        var json = """
            {"url":"https://example.com/a","format":"html","timeout":90,"maxCharacters":1234}
            """;

        Assert.True(FetchArguments.TryRead(json, out var request, out var error), error);
        Assert.Equal(PageFormat.Html, request.Format);
        Assert.Equal(90, request.TimeoutSeconds);
        Assert.Equal(1234, request.MaxCharacters);
    }

    [Fact]
    public void RejectsAMissingUrl()
    {
        Assert.False(FetchArguments.TryRead("{}", out _, out var error));
        Assert.Equal("Argument 'url' must be a non-empty string.", error);
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("not a url")]
    [InlineData("example.com")]
    public void RejectsANonHttpUrl(string url)
    {
        var json = $$"""{"url":"{{url}}"}""";

        Assert.False(FetchArguments.TryRead(json, out _, out var error));
        Assert.Equal("URL must use http:// or https://.", error);
    }

    [Fact]
    public void RejectsUrlCredentials()
    {
        Assert.False(
            FetchArguments.TryRead(
                """{"url":"https://user:pass@example.com/"}""",
                out _,
                out var error));
        Assert.Equal("URL credentials are not allowed.", error);
    }

    [Fact]
    public void RejectsAnUnknownProperty()
    {
        Assert.False(
            FetchArguments.TryRead(
                """{"url":"https://example.com/","raw":true}""",
                out _,
                out var error));
        Assert.Equal("Tool arguments must contain only url, format, timeout, and maxCharacters.", error);
    }

    [Fact]
    public void RejectsABadFormat()
    {
        Assert.False(
            FetchArguments.TryRead(
                """{"url":"https://example.com/","format":"xml"}""",
                out _,
                out var error));
        Assert.Equal("Argument 'format' must be 'text', 'markdown', or 'html'.", error);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("121")]
    public void RejectsATimeoutOutsideTheRange(string value)
    {
        var json = $$"""{"url":"https://example.com/","timeout":{{value}}}""";

        Assert.False(FetchArguments.TryRead(json, out _, out var error));
        Assert.Equal("Argument 'timeout' must be an integer between 1 and 120.", error);
    }

    [Fact]
    public void RejectsMaxCharactersOutsideTheRange()
    {
        Assert.False(
            FetchArguments.TryRead(
                """{"url":"https://example.com/","maxCharacters":0}""",
                out _,
                out var error));
        Assert.Equal("Argument 'maxCharacters' must be an integer between 1 and 100000.", error);
    }
}
