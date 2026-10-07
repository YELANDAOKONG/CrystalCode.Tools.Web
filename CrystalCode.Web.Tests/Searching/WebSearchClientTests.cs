using System.Net;

using CrystalCode.Web.Searching;

namespace CrystalCode.Web.Tests.Searching;

public sealed class WebSearchClientTests
{
    private const string FoundBody = """{"result":{"content":[{"type":"text","text":"x"}]}}""";

    [Fact]
    public async Task ReturnsTheProviderText()
    {
        var handler = new FakeHttpMessageHandler().Respond(
            """
            event: message
            data: {"jsonrpc":"2.0","id":1,"result":{"content":[{"type":"text","text":"hello world"}]}}
            """);

        var text = await SearchAsync(handler, """{"query":"crystal"}""");

        Assert.Equal("hello world", text);
    }

    [Fact]
    public async Task PostsToTheParallelEndpointByDefault()
    {
        var handler = new FakeHttpMessageHandler().Respond(FoundBody);

        await SearchAsync(handler, """{"query":"crystal"}""");

        var request = Assert.Single(handler.Requests);
        Assert.Equal("POST", request.Method);
        Assert.Equal("https://search.parallel.ai/mcp", request.Uri!.ToString());
        Assert.Contains("\"name\":\"web_search\"", request.Body, StringComparison.Ordinal);
        Assert.Contains("text/event-stream", request.Header("Accept"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddsABearerTokenForParallel()
    {
        var handler = new FakeHttpMessageHandler().Respond(FoundBody);
        var environment = TestEnvironment.From(("PARALLEL_API_KEY", "secret"));

        await SearchAsync(handler, """{"query":"crystal"}""", environment);

        Assert.Equal("Bearer secret", handler.Requests[0].Header("Authorization"));
    }

    [Fact]
    public async Task AddsAnExaKeyHeaderAndUsesTheExaEndpoint()
    {
        var handler = new FakeHttpMessageHandler().Respond(FoundBody);
        var environment = TestEnvironment.From(
            (SearchArguments.ProviderVariable, "exa"),
            ("EXA_API_KEY", "secret"));

        await SearchAsync(handler, """{"query":"crystal"}""", environment);

        var request = handler.Requests[0];
        Assert.Equal("https://mcp.exa.ai/mcp", request.Uri!.ToString());
        Assert.Equal("secret", request.Header("x-api-key"));
        Assert.Contains("web_search_exa", request.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReportsAnHttpStatusFailure()
    {
        var handler = new FakeHttpMessageHandler().Respond(
            _ => TestResponses.Status(HttpStatusCode.TooManyRequests));

        var exception = await Assert.ThrowsAsync<WebToolException>(
            () => SearchAsync(handler, """{"query":"crystal"}"""));

        Assert.Equal("Parallel search failed with HTTP status 429.", exception.Message);
    }

    [Fact]
    public async Task ReportsANetworkFailure()
    {
        var handler = new FakeHttpMessageHandler().Respond(
            _ => throw new HttpRequestException("boom"));

        var exception = await Assert.ThrowsAsync<WebToolException>(
            () => SearchAsync(handler, """{"query":"crystal"}"""));

        Assert.Equal("Network request failed or timed out.", exception.Message);
    }

    [Fact]
    public async Task CapsTheResponseSize()
    {
        var handler = new FakeHttpMessageHandler().Respond(
            new string('a', WebLimits.MaximumSearchResponseBytes + 1));

        var exception = await Assert.ThrowsAsync<WebToolException>(
            () => SearchAsync(handler, """{"query":"crystal"}"""));

        Assert.Contains("Response too large", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FallsBackWhenNothingIsReturned()
    {
        var handler = new FakeHttpMessageHandler().Respond("""{"result":{"content":[]}}""");

        var text = await SearchAsync(handler, """{"query":"crystal"}""");

        Assert.Equal(WebSearchClient.NoResultsText, text);
    }

    [Fact]
    public async Task TruncatesReturnedText()
    {
        var handler = new FakeHttpMessageHandler().Respond(
            """{"result":{"content":[{"type":"text","text":"abcdefghijklmnopqrstuvwxyz"}]}}""");

        var text = await SearchAsync(handler, """{"query":"crystal","maxCharacters":10}""");

        Assert.Equal("abcdefghij\n[truncated to 10 characters]", text);
    }

    private static async Task<string> SearchAsync(
        FakeHttpMessageHandler handler,
        string argumentsJson,
        Func<string, string?>? environment = null)
    {
        var lookup = environment ?? TestEnvironment.Empty;
        Assert.True(SearchArguments.TryRead(argumentsJson, lookup, out var request, out var error), error);
        var settings = SearchSettings.Resolve(request.Provider, lookup);
        using var http = new HttpClient(handler);
        return await WebSearchClient.SearchAsync(http, settings, request, CancellationToken.None);
    }
}
