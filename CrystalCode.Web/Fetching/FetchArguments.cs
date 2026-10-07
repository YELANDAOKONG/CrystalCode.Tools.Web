using System.Text.Json;

using CrystalCode.Web.Tools;

namespace CrystalCode.Web.Fetching;

/// <summary>
/// Reads and validates one model-generated webfetch argument object.
/// </summary>
internal static class FetchArguments
{
    private static readonly string[] AllowedProperties =
    [
        "url",
        "format",
        "timeout",
        "maxCharacters"
    ];

    public static bool TryRead(string json, out FetchRequest request, out string? error)
    {
        request = null!;
        if (!WebArguments.TryOpen(json, out var root, out var document, out error))
        {
            return false;
        }

        using (document)
        {
            if (!WebArguments.TryRejectUnknown(root, AllowedProperties, out error)
                || !WebArguments.TryReadRequiredString(root, "url", out var url, out error)
                || !TryReadUri(url, out var uri, out error)
                || !WebArguments.TryReadEnum(root, "format", PageFormats.Name(PageFormat.Markdown), [.. PageFormats.Choices], out var format, out error)
                || !WebArguments.TryReadBoundedInt(
                    root,
                    "timeout",
                    WebLimits.DefaultFetchTimeoutSeconds,
                    WebLimits.MaximumFetchTimeoutSeconds,
                    out var timeout,
                    out error)
                || !WebArguments.TryReadOptionalBoundedInt(
                    root,
                    "maxCharacters",
                    WebLimits.MaximumCharacters,
                    out var maxCharacters,
                    out error))
            {
                return false;
            }

            if (!PageFormats.TryParse(format, out var pageFormat))
            {
                error = $"Argument 'format' must be 'text', 'markdown', or 'html'.";
                return false;
            }

            request = new FetchRequest(uri, pageFormat, timeout, maxCharacters);
            return true;
        }
    }

    private static bool TryReadUri(string value, out Uri uri, out string? error)
    {
        uri = null!;
        error = null;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed)
            || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrEmpty(parsed.Host))
        {
            error = "URL must use http:// or https://.";
            return false;
        }

        if (!string.IsNullOrEmpty(parsed.UserInfo))
        {
            error = "URL credentials are not allowed.";
            return false;
        }

        uri = parsed;
        return true;
    }
}
