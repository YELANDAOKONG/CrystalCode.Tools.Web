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
}
