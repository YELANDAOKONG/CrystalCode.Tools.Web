using System.Net;
using System.Text;

using CrystalCode.Web.Html;

namespace CrystalCode.Web.Fetching;

/// <summary>
/// Fetches one page and converts it to the requested representation.
/// </summary>
internal static class WebPageFetcher
{
    private const string BrowserUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36";
    private const string CloudflareRetryUserAgent = "opencode";

    public static async Task<string> FetchAsync(
        HttpClient http,
        FetchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(request);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(request.TimeoutSeconds));
        try
        {
            using var response = await SendAsync(http, request, timeout.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new WebToolException($"Page fetch failed with HTTP status {(int)response.StatusCode}.");
            }

            var mime = response.Content.Headers.ContentType?.MediaType?.Trim().ToLowerInvariant() ?? string.Empty;
            EnsureSupportedContentType(mime);
            var body = Encoding.UTF8.GetString(
                await WebResponse
                    .ReadBytesAsync(response, WebLimits.MaximumFetchResponseBytes, timeout.Token)
                    .ConfigureAwait(false));
            return WebText.Truncate(Convert(body, request.Format, mime), request.MaxCharacters);
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

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient http,
        FetchRequest request,
        CancellationToken cancellationToken)
    {
        var response = await http
            .SendAsync(CreateRequest(request, BrowserUserAgent), HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        if (!IsCloudflareChallenge(response))
        {
            return response;
        }

        response.Dispose();
        return await http
            .SendAsync(CreateRequest(request, CloudflareRetryUserAgent), HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
    }

    private static HttpRequestMessage CreateRequest(FetchRequest request, string userAgent)
    {
        var message = new HttpRequestMessage(HttpMethod.Get, request.Url);
        message.Headers.UserAgent.ParseAdd(userAgent);
        message.Headers.Accept.ParseAdd(AcceptFor(request.Format));
        message.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
        return message;
    }

    private static bool IsCloudflareChallenge(HttpResponseMessage response) =>
        response.StatusCode == HttpStatusCode.Forbidden
        && response.Headers.TryGetValues("cf-mitigated", out var values)
        && values.Any(value => string.Equals(value, "challenge", StringComparison.OrdinalIgnoreCase));

    private static string AcceptFor(PageFormat format) => format switch
    {
        PageFormat.Markdown => "text/markdown;q=1.0, text/plain;q=0.8, text/html;q=0.7, */*;q=0.1",
        PageFormat.Text => "text/plain;q=1.0, text/markdown;q=0.9, text/html;q=0.8, */*;q=0.1",
        PageFormat.Html => "text/html;q=1.0, application/xhtml+xml;q=0.9, text/plain;q=0.8, */*;q=0.1",
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };

    private static void EnsureSupportedContentType(string mime)
    {
        if (mime.StartsWith("image/", StringComparison.Ordinal) && mime != "image/svg+xml")
        {
            throw new WebToolException($"Unsupported fetched image content type: {mime}.");
        }

        if (mime.Length > 0 && !IsTextual(mime))
        {
            throw new WebToolException($"Unsupported fetched file content type: {mime}.");
        }
    }

    private static bool IsTextual(string mime) =>
        mime.StartsWith("text/", StringComparison.Ordinal)
        || mime is "application/json" or "application/xml" or "application/javascript" or "application/x-javascript"
        || mime.EndsWith("+json", StringComparison.Ordinal)
        || mime.EndsWith("+xml", StringComparison.Ordinal);

    private static string Convert(string body, PageFormat format, string mime)
    {
        if (format == PageFormat.Html || !mime.Contains("html", StringComparison.Ordinal))
        {
            return body;
        }

        return format == PageFormat.Text
            ? HtmlTextExtractor.ExtractText(body)
            : HtmlMarkdownConverter.Convert(body);
    }
}
