using System.Text.Json;

using CrystalCode.Web.Searching;

namespace CrystalCode.Web.Tests.Searching;

public sealed class SearchRequestBodyTests
{
    [Fact]
    public void BuildsAnExaRequest()
    {
        var request = new SearchRequest(
            "crystal code",
            SearchProvider.Exa,
            5,
            1234,
            "deep",
            "preferred",
            "session-1",
            "gemini-3",
            null);

        using var document = JsonDocument.Parse(SearchRequestBody.Build(request));
        var root = document.RootElement;
        var parameters = root.GetProperty("params");
        var arguments = parameters.GetProperty("arguments");

        Assert.Equal("2.0", root.GetProperty("jsonrpc").GetString());
        Assert.Equal(1, root.GetProperty("id").GetInt32());
        Assert.Equal("tools/call", root.GetProperty("method").GetString());
        Assert.Equal("web_search_exa", parameters.GetProperty("name").GetString());
        Assert.Equal("crystal code", arguments.GetProperty("query").GetString());
        Assert.Equal("crystal code", arguments.GetProperty("objective").GetString());
        Assert.Equal(5, arguments.GetProperty("numResults").GetInt32());
        Assert.Equal(1234, arguments.GetProperty("contextMaxCharacters").GetInt32());
        Assert.Equal("deep", arguments.GetProperty("type").GetString());
        Assert.Equal("preferred", arguments.GetProperty("livecrawl").GetString());
        Assert.False(arguments.TryGetProperty("session_id", out _));
        Assert.False(arguments.TryGetProperty("model_name", out _));
    }

    [Fact]
    public void BuildsAParallelRequest()
    {
        var request = new SearchRequest(
            "crystal code",
            SearchProvider.Parallel,
            5,
            1234,
            "deep",
            "preferred",
            "session-1",
            "gemini-3",
            null);

        using var document = JsonDocument.Parse(SearchRequestBody.Build(request));
        var parameters = document.RootElement.GetProperty("params");
        var arguments = parameters.GetProperty("arguments");

        Assert.Equal("web_search", parameters.GetProperty("name").GetString());
        Assert.Equal("crystal code", arguments.GetProperty("objective").GetString());
        Assert.Equal(
            ["crystal code"],
            arguments.GetProperty("search_queries").EnumerateArray().Select(item => item.GetString()));
        Assert.Equal("session-1", arguments.GetProperty("session_id").GetString());
        Assert.Equal("gemini-3", arguments.GetProperty("model_name").GetString());
        Assert.False(arguments.TryGetProperty("numResults", out _));
        Assert.False(arguments.TryGetProperty("contextMaxCharacters", out _));
    }

    [Fact]
    public void OmitsUnsetParallelMetadata()
    {
        var request = new SearchRequest(
            "crystal code",
            SearchProvider.Parallel,
            8,
            10_000,
            "auto",
            "fallback",
            null,
            null,
            null);

        using var document = JsonDocument.Parse(SearchRequestBody.Build(request));
        var arguments = document.RootElement.GetProperty("params").GetProperty("arguments");

        Assert.False(arguments.TryGetProperty("session_id", out _));
        Assert.False(arguments.TryGetProperty("model_name", out _));
    }

    [Fact]
    public void BuildsAJinaRequest()
    {
        using var document = JsonDocument.Parse(SearchRequestBody.Build(Request(SearchProvider.Jina)));
        var parameters = document.RootElement.GetProperty("params");
        var arguments = parameters.GetProperty("arguments");

        Assert.Equal("tools/call", document.RootElement.GetProperty("method").GetString());
        Assert.Equal("search_web", parameters.GetProperty("name").GetString());
        Assert.Equal("crystal code", arguments.GetProperty("query").GetString());
        Assert.Equal(5, arguments.GetProperty("num").GetInt32());
    }

    [Fact]
    public void ClampsJinaResultCount()
    {
        using var document = JsonDocument.Parse(
            SearchRequestBody.Build(Request(SearchProvider.Jina, numResults: 20)));
        var arguments = document.RootElement
            .GetProperty("params")
            .GetProperty("arguments");

        Assert.Equal(10, arguments.GetProperty("num").GetInt32());
    }

    [Fact]
    public void BuildsATavilyRequest()
    {
        using var document = JsonDocument.Parse(
            SearchRequestBody.Build(Request(SearchProvider.Tavily, type: "deep")));
        var root = document.RootElement;

        Assert.False(root.TryGetProperty("jsonrpc", out _));
        Assert.Equal("crystal code", root.GetProperty("query").GetString());
        Assert.Equal(5, root.GetProperty("max_results").GetInt32());
        Assert.Equal("advanced", root.GetProperty("search_depth").GetString());
        Assert.False(root.GetProperty("include_answer").GetBoolean());
        Assert.True(root.GetProperty("include_published_date").GetBoolean());
    }

    [Fact]
    public void BuildsABochaRequest()
    {
        using var document = JsonDocument.Parse(
            SearchRequestBody.Build(Request(SearchProvider.Bocha)));
        var root = document.RootElement;

        Assert.False(root.TryGetProperty("jsonrpc", out _));
        Assert.Equal("crystal code", root.GetProperty("query").GetString());
        Assert.Equal(5, root.GetProperty("count").GetInt32());
        Assert.True(root.GetProperty("summary").GetBoolean());
        Assert.Equal("noLimit", root.GetProperty("freshness").GetString());
    }

    [Fact]
    public void BuildsAPerplexityRequest()
    {
        using var document = JsonDocument.Parse(
            SearchRequestBody.Build(Request(SearchProvider.Perplexity, type: "fast")));
        var root = document.RootElement;

        Assert.False(root.TryGetProperty("jsonrpc", out _));
        Assert.Equal("crystal code", root.GetProperty("query").GetString());
        Assert.Equal(5, root.GetProperty("max_results").GetInt32());
        Assert.Equal("fast", root.GetProperty("search_type").GetString());
    }

    [Fact]
    public void BuildsAFirecrawlRequest()
    {
        using var document = JsonDocument.Parse(
            SearchRequestBody.Build(Request(SearchProvider.Firecrawl)));
        var root = document.RootElement;

        Assert.False(root.TryGetProperty("jsonrpc", out _));
        Assert.Equal("crystal code", root.GetProperty("query").GetString());
        Assert.Equal(5, root.GetProperty("limit").GetInt32());
        Assert.Equal(
            ["web"],
            root.GetProperty("sources").EnumerateArray().Select(item => item.GetString()));
        Assert.True(root.GetProperty("highlights").GetBoolean());
    }

    private static SearchRequest Request(
        SearchProvider provider,
        int numResults = 5,
        string type = "auto") =>
        new(
            "crystal code",
            provider,
            numResults,
            1234,
            type,
            "fallback",
            null,
            null,
            null);
}
