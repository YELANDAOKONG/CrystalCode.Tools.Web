using System.Net.Http.Headers;
using System.Text;

namespace CrystalCode.Web.Searching;

/// <summary>
/// Sends one MCP or REST search call and returns the model-visible result text.
/// </summary>
internal static class WebSearchClient
{
    public const string NoResultsText = "No search results found. Please try a different query.";

    private const string UserAgent = "crystal-code-web-tools/1";
    private const string McpAccept = "application/json, text/event-stream";
    private const string RestAccept = "application/json";

    public static async Task<string> SearchAsync(
        HttpClient http,
        SearchSettings settings,
        SearchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(request);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(WebLimits.SearchTimeoutSeconds));
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, settings.Endpoint)
            {
                Content = new StringContent(SearchRequestBody.Build(request), Encoding.UTF8, "application/json")
            };
            message.Headers.Accept.ParseAdd(
                settings.Definition.Transport == SearchTransport.Mcp ? McpAccept : RestAccept);
            message.Headers.UserAgent.ParseAdd(UserAgent);
            ApplyCredential(message, settings);

            using var response = await http
                .SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new WebToolException(
                    $"{SearchProviders.Title(request.Provider)} search failed with HTTP status {(int)response.StatusCode}.");
            }

            var body = Encoding.UTF8.GetString(
                await WebResponse
                    .ReadBytesAsync(response, WebLimits.MaximumSearchResponseBytes, timeout.Token)
                    .ConfigureAwait(false));
            string? text;
            string? error;
            if (settings.Definition.Transport == SearchTransport.Mcp)
            {
                text = McpSearchResponse.TryRead(body, out var mcpText, out error)
                    ? SearchResultFormatter.Format(mcpText ?? string.Empty)
                    : null;
            }
            else
            {
                text = RestSearchResponse.TryRead(
                    settings.Definition.Provider,
                    body,
                    out var restText,
                    out error)
                    ? restText
                    : null;
            }

            if (error is not null)
            {
                throw new WebToolException(error);
            }

            var result = string.IsNullOrWhiteSpace(text) ? NoResultsText : text;
            return WebText.Truncate(result, request.MaxCharacters);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new WebToolException("Network request failed or timed out.");
        }
        catch (HttpRequestException)
        {
            throw new WebToolException("Network request failed or timed out.");
        }
    }

    private static void ApplyCredential(
        HttpRequestMessage message,
        SearchSettings settings)
    {
        if (settings.ApiKey is null)
        {
            return;
        }

        if (settings.Definition.Provider == SearchProvider.Exa)
        {
            message.Headers.Add("x-api-key", settings.ApiKey);
        }
        else
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        }
    }
}
