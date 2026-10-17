using System.Net;
using System.Text.Json;

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
    public async Task FormatsStructuredProviderText()
    {
        var content = """
            {"results":[{"url":"https://example.com","title":"Example","publish_date":null,"excerpts":["hello"]}]}
            """;
        var body = JsonSerializer.Serialize(new
        {
            result = new
            {
                content = new[] { new { type = "text", text = content } }
            }
        });
        var handler = new FakeHttpMessageHandler().Respond(body);

        var text = await SearchAsync(handler, """{"query":"crystal"}""");

        Assert.Equal(
            """
            Title: Example
            URL: https://example.com
            Published: N/A
            Highlights:
            hello
            """,
            text);
    }

    [Fact]
    public async Task FallsBackToRawProviderTextWhenStructuredParsingFails()
    {
        const string content = """{"results":[}""";
        var body = JsonSerializer.Serialize(new
        {
            result = new
            {
                content = new[] { new { type = "text", text = content } }
            }
        });
        var handler = new FakeHttpMessageHandler().Respond(body);

        var text = await SearchAsync(handler, """{"query":"crystal"}""");

        Assert.Equal(content, text);
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

    [Fact]
    public async Task PostsToTavilyWithABearerToken()
    {
        var handler = new FakeHttpMessageHandler().Respond(
            """{"results":[{"title":"Example","url":"https://example.com","content":"hello"}]}""");
        var environment = TestEnvironment.From(
            (SearchArguments.ProviderVariable, "tavily"),
            ("TAVILY_API_KEY", "secret"));

        var text = await SearchAsync(
            handler,
            """{"query":"crystal","type":"deep","numResults":5}""",
            environment);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://api.tavily.com/search", request.Uri!.ToString());
        Assert.Equal("application/json", request.Header("Accept"));
        Assert.Equal("Bearer secret", request.Header("Authorization"));
        Assert.Contains("\"search_depth\":\"advanced\"", request.Body, StringComparison.Ordinal);
        Assert.Contains("\"max_results\":5", request.Body, StringComparison.Ordinal);
        Assert.Equal(
            """
            Title: Example
            URL: https://example.com
            Published: N/A
            Highlights:
            hello
            """,
            text);
    }

    [Fact]
    public async Task PostsToBochaWithABearerToken()
    {
        var handler = new FakeHttpMessageHandler().Respond(
            """
            {"code":200,"data":{"webPages":{"value":[
              {"name":"Example","url":"https://example.com","summary":"hello","siteName":"Example"}
            ]}}}
            """);
        var environment = TestEnvironment.From(
            (SearchArguments.ProviderVariable, "bocha"),
            ("BOCHA_API_KEY", "secret"));

        var text = await SearchAsync(handler, """{"query":"crystal"}""", environment);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://api.bocha.cn/v1/web-search", request.Uri!.ToString());
        Assert.Equal("Bearer secret", request.Header("Authorization"));
        Assert.Contains("\"count\":8", request.Body, StringComparison.Ordinal);
        Assert.Contains("\"summary\":true", request.Body, StringComparison.Ordinal);
        Assert.Equal(
            """
            Title: Example
            URL: https://example.com
            Published: N/A
            Site: Example
            Highlights:
            hello
            """,
            text);
    }

    [Fact]
    public async Task PostsToPerplexityWithABearerToken()
    {
        var handler = new FakeHttpMessageHandler().Respond(
            """{"results":[{"title":"Example","url":"https://example.com","snippet":"hello"}],"id":"x"}""");
        var environment = TestEnvironment.From(
            (SearchArguments.ProviderVariable, "perplexity"),
            ("PERPLEXITY_API_KEY", "secret"));

        var text = await SearchAsync(handler, """{"query":"crystal","type":"fast"}""", environment);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://api.perplexity.ai/search", request.Uri!.ToString());
        Assert.Equal("Bearer secret", request.Header("Authorization"));
        Assert.Contains("\"search_type\":\"fast\"", request.Body, StringComparison.Ordinal);
        Assert.Equal(
            """
            Title: Example
            URL: https://example.com
            Published: N/A
            Highlights:
            hello
            """,
            text);
    }

    [Fact]
    public async Task PostsToJinaWithABearerToken()
    {
        var handler = new FakeHttpMessageHandler().Respond(
            """
            {"result":{"content":[{"type":"text","text":"title: Example\nurl: https://example.com"}]}}
            """);
        var environment = TestEnvironment.From(
            (SearchArguments.ProviderVariable, "jina"),
            ("JINA_API_KEY", "secret"));

        var text = await SearchAsync(handler, """{"query":"crystal"}""", environment);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://mcp.jina.ai/v1", request.Uri!.ToString());
        Assert.Equal("application/json, text/event-stream", request.Header("Accept"));
        Assert.Equal("Bearer secret", request.Header("Authorization"));
        Assert.Contains("\"name\":\"search_web\"", request.Body, StringComparison.Ordinal);
        Assert.Equal("title: Example\nurl: https://example.com", text);
    }

    [Fact]
    public async Task PostsToFirecrawlWithABearerToken()
    {
        var handler = new FakeHttpMessageHandler().Respond(
            """{"success":true,"data":{"web":[{"title":"Example","url":"https://example.com","description":"hello"}]}}""");
        var environment = TestEnvironment.From(
            (SearchArguments.ProviderVariable, "firecrawl"),
            ("FIRECRAWL_API_KEY", "secret"));

        var text = await SearchAsync(handler, """{"query":"crystal"}""", environment);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://api.firecrawl.dev/v2/search", request.Uri!.ToString());
        Assert.Equal("Bearer secret", request.Header("Authorization"));
        Assert.Contains("\"sources\":[\"web\"]", request.Body, StringComparison.Ordinal);
        Assert.Equal(
            """
            Title: Example
            URL: https://example.com
            Published: N/A
            Highlights:
            hello
            """,
            text);
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
