using System.Text.Json;

using Crystal.Tools;

using CrystalCode.Web.Fetching;

namespace CrystalCode.Web.Tools;

/// <summary>
/// Fetches one HTTP or HTTPS page as Markdown, plain text, or raw HTML.
/// </summary>
public sealed class WebFetchTool : ITool
{
    private const string Schema = """
        {
          "type": "object",
          "properties": {
            "url": {
              "type": "string",
              "description": "The http or https URL to fetch."
            },
            "format": {
              "type": "string",
              "enum": ["text", "markdown", "html"],
              "default": "markdown",
              "description": "Returned representation: 'markdown' (default), 'text', or raw 'html'."
            },
            "timeout": {
              "type": "integer",
              "minimum": 1,
              "maximum": 120,
              "default": 30,
              "description": "Request timeout in seconds (1-120, default 30)."
            },
            "maxCharacters": {
              "type": "integer",
              "minimum": 1,
              "maximum": 100000,
              "description": "Maximum characters in the returned page text (1-100000). The tool truncates longer pages and notes it; optional."
            }
          },
          "required": ["url"],
          "additionalProperties": false
        }
        """;

    private static readonly ToolDefinition Tool = CreateTool();

    private readonly HttpClient _http;

    /// <summary>
    /// Initializes the tool with the shared HTTP client.
    /// </summary>
    public WebFetchTool()
        : this(WebHttp.Shared)
    {
    }

    internal WebFetchTool(HttpClient http)
    {
        ArgumentNullException.ThrowIfNull(http);
        _http = http;
    }

    /// <inheritdoc />
    public ToolDefinition Definition => Tool;

    /// <inheritdoc />
    public async ValueTask<ToolOutput> InvokeAsync(
        ToolCall call,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(call);
        if (!FetchArguments.TryRead(call.Arguments, out var request, out var error))
        {
            return WebToolResult.Failure(error ?? "Tool arguments are not valid.");
        }

        try
        {
            var text = await WebPageFetcher
                .FetchAsync(_http, request, cancellationToken)
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
            "webfetch",
            document.RootElement.Clone(),
            "Fetch a public HTTP or HTTPS page and return Markdown (default), plain text, or raw HTML. Use this after websearch when you need to read a specific URL. maxCharacters truncates long pages.");
    }
}
