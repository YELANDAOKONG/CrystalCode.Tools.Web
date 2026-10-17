using System.Text.Json;

namespace CrystalCode.Web.Searching;

/// <summary>
/// Reads a REST search response and normalizes its results for the model.
/// Returns <see langword="false"/> when the body is not a JSON object; provider
/// error payloads still count as readable and return <see langword="true"/>.
/// </summary>
internal static class RestSearchResponse
{
    private const string ErrorPrefix = "The search provider returned an error: ";

    public static bool TryRead(
        SearchProvider provider,
        string body,
        out string? text,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(body);
        text = null;
        error = null;

        JsonDocument? document = null;
        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            error = "The search provider returned an invalid JSON response.";
            return false;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                error = "The search provider returned an invalid JSON response.";
                return false;
            }

            if (TryReadError(root, out error))
            {
                return true;
            }

            var items = provider switch
            {
                SearchProvider.Tavily => ReadTavily(root),
                SearchProvider.Bocha => ReadBocha(root),
                SearchProvider.Perplexity => ReadPerplexity(root),
                SearchProvider.Firecrawl => ReadFirecrawl(root),
                _ => throw new ArgumentOutOfRangeException(nameof(provider))
            };
            text = SearchResultRenderer.Render(items);
            return true;
        }
    }

    private static bool TryReadError(JsonElement root, out string? error)
    {
        error = null;
        if (root.TryGetProperty("error", out var errorValue)
            && TryReadMessage(errorValue, out var errorMessage))
        {
            error = FormatError(errorMessage);
            return true;
        }

        if (root.TryGetProperty("detail", out var detail)
            && TryReadMessage(detail, out var detailMessage))
        {
            error = FormatError(detailMessage);
            return true;
        }

        if (root.TryGetProperty("success", out var success)
            && success.ValueKind == JsonValueKind.False)
        {
            error = FormatError(
                ReadString(root, "message")
                ?? ReadString(root, "error")
                ?? ReadString(root, "detail"));
            return true;
        }

        if (root.TryGetProperty("code", out var code)
            && !IsSuccessCode(code))
        {
            error = FormatError(
                ReadString(root, "msg")
                ?? ReadString(root, "message"));
            return true;
        }

        return false;
    }

    private static bool IsSuccessCode(JsonElement code) => code.ValueKind switch
    {
        JsonValueKind.Number => code.TryGetInt32(out var value) && value == 200,
        JsonValueKind.String => string.Equals(code.GetString(), "200", StringComparison.Ordinal),
        _ => true
    };

    private static string FormatError(string? message) =>
        message is null
            ? "The search provider returned an error."
            : ErrorPrefix + message;

    private static bool TryReadMessage(JsonElement value, out string? message)
    {
        message = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString()?.Trim(),
            JsonValueKind.Object => ReadString(value, "message")
                ?? ReadString(value, "msg")
                ?? ReadString(value, "detail")
                ?? ReadString(value, "error"),
            _ => null
        };
        return !string.IsNullOrWhiteSpace(message);
    }

    private static List<SearchResultItem> ReadTavily(JsonElement root)
    {
        var items = new List<SearchResultItem>();
        if (!TryGetArray(root, "results", out var results))
        {
            return items;
        }

        foreach (var result in results.EnumerateArray())
        {
            var url = ReadString(result, "url");
            if (url is null)
            {
                continue;
            }

            items.Add(new SearchResultItem(
                ReadString(result, "title"),
                url,
                ReadString(result, "published_date"),
                null,
                null,
                ReadHighlights(result, "content")));
        }

        return items;
    }

    private static List<SearchResultItem> ReadBocha(JsonElement root)
    {
        var items = new List<SearchResultItem>();
        if (!root.TryGetProperty("data", out var data)
            || data.ValueKind != JsonValueKind.Object
            || !data.TryGetProperty("webPages", out var webPages)
            || webPages.ValueKind != JsonValueKind.Object
            || !TryGetArray(webPages, "value", out var results))
        {
            return items;
        }

        foreach (var result in results.EnumerateArray())
        {
            var url = ReadString(result, "url");
            if (url is null)
            {
                continue;
            }

            var highlight = ReadString(result, "summary") ?? ReadString(result, "snippet");
            items.Add(new SearchResultItem(
                ReadString(result, "name"),
                url,
                ReadString(result, "datePublished"),
                ReadString(result, "siteName"),
                null,
                highlight is null ? [] : [highlight]));
        }

        return items;
    }

    private static List<SearchResultItem> ReadPerplexity(JsonElement root)
    {
        var items = new List<SearchResultItem>();
        if (!TryGetArray(root, "results", out var results))
        {
            return items;
        }

        foreach (var result in results.EnumerateArray())
        {
            var url = ReadString(result, "url");
            if (url is null)
            {
                continue;
            }

            items.Add(new SearchResultItem(
                ReadString(result, "title"),
                url,
                ReadString(result, "date"),
                null,
                ReadString(result, "last_updated"),
                ReadHighlights(result, "snippet")));
        }

        return items;
    }

    private static List<SearchResultItem> ReadFirecrawl(JsonElement root)
    {
        var items = new List<SearchResultItem>();
        if (!root.TryGetProperty("data", out var data)
            || data.ValueKind != JsonValueKind.Object
            || !TryGetArray(data, "web", out var results))
        {
            return items;
        }

        foreach (var result in results.EnumerateArray())
        {
            var url = ReadString(result, "url");
            if (url is null)
            {
                continue;
            }

            items.Add(new SearchResultItem(
                ReadString(result, "title"),
                url,
                null,
                null,
                null,
                ReadHighlights(result, "description")));
        }

        return items;
    }

    private static bool TryGetArray(JsonElement element, string propertyName, out JsonElement value)
    {
        value = default;
        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(propertyName, out value)
            && value.ValueKind == JsonValueKind.Array;
    }

    private static List<string> ReadHighlights(JsonElement element, string propertyName)
    {
        var values = new List<string>();
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return values;
        }

        if (value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()))
        {
            values.Add(value.GetString()!.Trim());
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                {
                    values.Add(item.GetString()!.Trim());
                }
            }
        }

        return values;
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(value.GetString()))
        {
            return null;
        }

        return value.GetString()!.Trim();
    }
}
