using System.Text.Json.Nodes;

namespace CrystalCode.Web.Searching;

/// <summary>
/// Builds the MCP tools/call body for one search provider. Providers accept
/// different argument sets; unsupported settings are left out instead of sent.
/// </summary>
internal static class SearchRequestBody
{
    public static string Build(SearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var payload = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 1,
            ["method"] = "tools/call",
            ["params"] = new JsonObject
            {
                ["name"] = request.Provider == SearchProvider.Exa ? "web_search_exa" : "web_search",
                ["arguments"] = request.Provider == SearchProvider.Exa
                    ? BuildExaArguments(request)
                    : BuildParallelArguments(request)
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
}
