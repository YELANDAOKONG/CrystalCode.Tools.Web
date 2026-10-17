using System.Text.Json.Nodes;

namespace CrystalCode.Web.Searching;

/// <summary>
/// Builds the request body for one search provider. MCP tools use a JSON-RPC
/// envelope; REST providers receive their native search payload.
/// </summary>
internal static class SearchRequestBody
{
    public static string Build(SearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var definition = SearchProviders.Get(request.Provider);
        return definition.Transport == SearchTransport.Mcp
            ? BuildMcp(request, definition)
            : BuildRest(request);
    }

    private static string BuildMcp(
        SearchRequest request,
        SearchProviderDefinition definition)
    {
        var payload = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 1,
            ["method"] = "tools/call",
            ["params"] = new JsonObject
            {
                ["name"] = definition.ToolName
                    ?? throw new InvalidOperationException(
                        $"{definition.Title} does not define an MCP tool name."),
                ["arguments"] = request.Provider switch
                {
                    SearchProvider.Parallel => BuildParallelArguments(request),
                    SearchProvider.Exa => BuildExaArguments(request),
                    SearchProvider.Jina => BuildJinaArguments(request),
                    _ => throw new ArgumentOutOfRangeException(nameof(request.Provider))
                }
            }
        };

        return payload.ToJsonString();
    }

    private static JsonObject BuildExaArguments(SearchRequest request) => new()
    {
        ["query"] = request.Query,
        ["objective"] = request.Query,
        ["type"] = request.Type,
        ["numResults"] = request.NumResults,
        ["livecrawl"] = request.Livecrawl,
        ["contextMaxCharacters"] = request.ContextMaxCharacters
    };

    private static JsonObject BuildJinaArguments(SearchRequest request) => new()
    {
        ["query"] = request.Query,
        ["num"] = Math.Clamp(request.NumResults, 1, 10)
    };

    private static JsonObject BuildParallelArguments(SearchRequest request)
    {
        var arguments = new JsonObject
        {
            ["objective"] = request.Query,
            ["search_queries"] = new JsonArray { request.Query }
        };
        if (request.SessionId is not null)
        {
            arguments["session_id"] = request.SessionId;
        }

        if (request.ModelName is not null)
        {
            arguments["model_name"] = request.ModelName;
        }

        return arguments;
    }

    private static string BuildRest(SearchRequest request) =>
        request.Provider switch
        {
            SearchProvider.Tavily => BuildTavilyArguments(request).ToJsonString(),
            SearchProvider.Bocha => BuildBochaArguments(request).ToJsonString(),
            SearchProvider.Perplexity => BuildPerplexityArguments(request).ToJsonString(),
            SearchProvider.Firecrawl => BuildFirecrawlArguments(request).ToJsonString(),
            _ => throw new ArgumentOutOfRangeException(nameof(request.Provider))
        };

    private static JsonObject BuildTavilyArguments(SearchRequest request) => new()
    {
        ["query"] = request.Query,
        ["max_results"] = Math.Clamp(request.NumResults, 1, 20),
        ["search_depth"] = request.Type switch
        {
            "fast" => "fast",
            "deep" => "advanced",
            _ => "basic"
        },
        ["include_answer"] = false,
        ["include_published_date"] = true
    };

    private static JsonObject BuildBochaArguments(SearchRequest request) => new()
    {
        ["query"] = request.Query,
        ["count"] = Math.Clamp(request.NumResults, 1, 50),
        ["summary"] = true,
        ["freshness"] = "noLimit"
    };

    private static JsonObject BuildPerplexityArguments(SearchRequest request) => new()
    {
        ["query"] = request.Query,
        ["max_results"] = Math.Clamp(request.NumResults, 1, 20),
        ["search_type"] = request.Type == "fast" ? "fast" : "web"
    };

    private static JsonObject BuildFirecrawlArguments(SearchRequest request) => new()
    {
        ["query"] = request.Query,
        ["limit"] = Math.Clamp(request.NumResults, 1, 100),
        ["sources"] = new JsonArray { "web" },
        ["highlights"] = true
    };
}
