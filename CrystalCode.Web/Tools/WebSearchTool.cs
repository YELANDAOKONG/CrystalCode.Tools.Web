using System.Text.Json;

using Crystal.Tools;

using CrystalCode.Web.Searching;

namespace CrystalCode.Web.Tools;

/// <summary>
/// Searches the live web through the hosted Parallel or Exa MCP endpoint.
/// </summary>
public sealed class WebSearchTool : ITool
{
    private const string Schema = """
        {
          "type": "object",
          "properties": {
            "query": {
              "type": "string",
              "description": "The web search query."
            },
            "numResults": {
              "type": "integer",
              "minimum": 1,
              "maximum": 20,
              "default": 8,
              "description": "Number of search results to return (1-20, default 8; the Exa backend honors it)."
            },
            "contextMaxCharacters": {
              "type": "integer",
              "minimum": 1,
              "maximum": 50000,
              "default": 10000,
              "description": "Maximum characters of context the search engine returns (1-50000, default 10000; the Exa backend honors it)."
            },
            "type": {
              "type": "string",
              "enum": ["auto", "fast", "deep"],
              "default": "auto",
              "description": "Search effort for the Exa backend: 'auto' is balanced, 'fast' is quicker, 'deep' is more thorough."
            },
            "livecrawl": {
              "type": "string",
              "enum": ["fallback", "preferred"],
              "default": "fallback",
              "description": "Exa live crawl preference: 'fallback' uses live crawling as backup, 'preferred' prioritizes it."
            },
            "session_id": {
              "type": "string",
              "description": "Stable conversation id reused across related Parallel calls; optional."
            },
            "model_name": {
              "type": "string",
              "description": "Exact model identifier for Parallel product analytics only; optional."
            },
            "maxCharacters": {
              "type": "integer",
              "minimum": 1,
              "maximum": 100000,
              "description": "Maximum characters in the returned text (1-100000). The tool truncates longer results and notes it; optional."
            }
          },
          "required": ["query"],
          "additionalProperties": false
        }
        """;

    private static readonly ToolDefinition Tool = CreateTool();

    private readonly HttpClient _http;
    private readonly Func<string, string?> _environment;

    /// <summary>
    /// Initializes the tool with the shared HTTP client and the process environment.
    /// </summary>
    public WebSearchTool()
        : this(WebHttp.Shared, Environment.GetEnvironmentVariable)
    {
    }

    internal WebSearchTool(HttpClient http, Func<string, string?> environment)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(environment);
        _http = http;
        _environment = environment;
    }

    /// <inheritdoc />
    public ToolDefinition Definition => Tool;

    /// <inheritdoc />
    public async ValueTask<ToolOutput> InvokeAsync(
        ToolCall call,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(call);
        if (!SearchArguments.TryRead(call.Arguments, _environment, out var request, out var error))
        {
            return WebToolResult.Failure(error ?? "Tool arguments are not valid.");
        }

        try
        {
            var settings = SearchSettings.Resolve(request.Provider, _environment);
            var text = await WebSearchClient
                .SearchAsync(_http, settings, request, cancellationToken)
                .ConfigureAwait(false);
            return new ToolOutput(text);
        }
        catch (WebToolException exception)
        {
            return WebToolResult.Failure(exception.Message);
        }
    }

    private static ToolDefinition CreateTool()
    {
        using var document = JsonDocument.Parse(Schema);
        return new ToolDefinition(
            "websearch",
            document.RootElement.Clone(),
            "Search the live web through the Parallel or Exa hosted MCP backend selected by the operator (Parallel by default). Use this for current information and discovery. numResults, contextMaxCharacters, and maxCharacters control how much information comes back.");
    }
}
