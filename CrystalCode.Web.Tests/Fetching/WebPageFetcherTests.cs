using System.Net;

using CrystalCode.Web.Fetching;

namespace CrystalCode.Web.Tests.Fetching;

public sealed class WebPageFetcherTests
{
    [Fact]
    public async Task ConvertsHtmlToMarkdownByDefault()
    {
        var handler = new FakeHttpMessageHandler().Respond(
            _ => TestResponses.Html("<h1>Title</h1><p>Body</p>"));

        var text = await FetchAsync(handler, """{"url":"https://example.com/"}""");

        Assert.Equal("# Title\n\nBody", text);
    }

    [Fact]
    public async Task ExtractsPlainText()
    {
        var handler = new FakeHttpMessageHandler().Respond(
            _ => TestResponses.Html("<h1>Title</h1><p>Body</p>"));

        var text = await FetchAsync(
            handler,
            """{"url":"https://example.com/","format":"text"}""");

        Assert.Equal("TitleBody", text);
    }

    [Fact]
    public async Task ReturnsRawHtml()
    {
        const string page = "<h1>Title</h1><p>Body</p>";
        var handler = new FakeHttpMessageHandler().Respond(_ => TestResponses.Html(page));

        var text = await FetchAsync(
            handler,
            """{"url":"https://example.com/","format":"html"}""");

        Assert.Equal(page, text);
    }

    [Fact]
    public async Task ReturnsTheBodyWhenTheContentTypeIsNotHtml()
    {
        var handler = new FakeHttpMessageHandler().Respond(_ => TestResponses.Text("plain body"));

        var text = await FetchAsync(
            handler,
            """{"url":"https://example.com/","format":"markdown"}""");

        Assert.Equal("plain body", text);
    }

    [Fact]
    public async Task RejectsImageContent()
    {
        var handler = new FakeHttpMessageHandler().Respond(
            _ => TestResponses.Text("binary", "image/png"));

        var exception = await Assert.ThrowsAsync<WebToolException>(
            () => FetchAsync(handler, """{"url":"https://example.com/"}"""));

        Assert.Equal("Unsupported fetched image content type: image/png.", exception.Message);
    }

    [Fact]
    public async Task RejectsFileContent()
    {
        var handler = new FakeHttpMessageHandler().Respond(
            _ => TestResponses.Text("binary", "application/pdf"));

        var exception = await Assert.ThrowsAsync<WebToolException>(
            () => FetchAsync(handler, """{"url":"https://example.com/"}"""));

        Assert.Equal("Unsupported fetched file content type: application/pdf.", exception.Message);
    }

    [Fact]
    public async Task AllowsSvgContent()
    {
        const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>";
        var handler = new FakeHttpMessageHandler().Respond(
            _ => TestResponses.Text(svg, "image/svg+xml"));

        var text = await FetchAsync(handler, """{"url":"https://example.com/"}""");

        Assert.Equal(svg, text);
    }

    [Fact]
    public async Task RetriesACloudflareChallenge()
    {
        var handler = new FakeHttpMessageHandler()
            .Respond(_ => TestResponses.CloudflareChallenge())
            .Respond(_ => TestResponses.Html("<p>ok</p>"));

        var text = await FetchAsync(handler, """{"url":"https://example.com/"}""");

        Assert.Equal("ok", text);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("Chrome", handler.Requests[0].Header("User-Agent"), StringComparison.Ordinal);
        Assert.Equal("opencode", handler.Requests[1].Header("User-Agent"));
    }

    [Fact]
    public async Task ReportsAnHttpStatusFailure()
    {
        var handler = new FakeHttpMessageHandler().Respond(
            _ => TestResponses.Status(HttpStatusCode.NotFound));

        var exception = await Assert.ThrowsAsync<WebToolException>(
            () => FetchAsync(handler, """{"url":"https://example.com/"}"""));

        Assert.Equal("Page fetch failed with HTTP status 404.", exception.Message);
    }

    [Fact]
    public async Task ReportsANetworkFailure()
    {
        var handler = new FakeHttpMessageHandler().Respond(
            _ => throw new HttpRequestException("boom"));

        var exception = await Assert.ThrowsAsync<WebToolException>(
            () => FetchAsync(handler, """{"url":"https://example.com/"}"""));

        Assert.Equal("Network request failed or timed out.", exception.Message);
    }

    [Fact]
    public async Task TruncatesLongText()
    {
        var handler = new FakeHttpMessageHandler().Respond(
            _ => TestResponses.Html("<p>abcdefghijklmnopqrstuvwxyz</p>"));

        var text = await FetchAsync(
            handler,
            """{"url":"https://example.com/","maxCharacters":10}""");

        Assert.Equal("abcdefghij\n[truncated to 10 characters]", text);
    }

    [Fact]
    public async Task SendsTheFormatAcceptHeader()
    {
        var handler = new FakeHttpMessageHandler().Respond(_ => TestResponses.Html("<p>x</p>"));

        await FetchAsync(handler, """{"url":"https://example.com/","format":"markdown"}""");

        Assert.Contains("text/markdown", handler.Requests[0].Header("Accept"), StringComparison.Ordinal);
    }

    private static async Task<string> FetchAsync(
        FakeHttpMessageHandler handler,
        string argumentsJson)
    {
        Assert.True(FetchArguments.TryRead(argumentsJson, out var request, out var error), error);
        using var http = new HttpClient(handler);
        return await WebPageFetcher.FetchAsync(http, request, CancellationToken.None);
    }
}
