using System.Text.Json;

namespace CrystalCode.Web.Searching;

/// <summary>
/// Converts a provider's structured search text into readable result blocks.
/// Plain text, such as the Exa or Jina payload, is returned unchanged.
/// </summary>
internal static class SearchResultFormatter
{
    public static string Format(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var trimmed = text.Trim();
        if (!trimmed.StartsWith('{'))
        {
            return text;
        }

        JsonDocument? document = null;
        try
        {
            document = JsonDocument.Parse(trimmed);
        }
        catch (JsonException)
        {
            return text;
        }

        using (document)
        {
            if (!TryReadResults(document.RootElement, out var results))
            {
                return text;
            }

            var warnings = ReadWarnings(document.RootElement);
            var resultCount = results.GetArrayLength();
            var items = ReadResults(results);
            var resultText = SearchResultRenderer.Render(items);
            if (resultCount > 0 && resultText.Length == 0)
            {
                return text;
            }

            var blocks = new List<string>();
            if (warnings.Count > 0)
            {
                blocks.Add("Warnings:\n- " + string.Join("\n- ", warnings));
            }

            if (resultText.Length > 0)
            {
                blocks.Add(resultText);
            }

            return string.Join("\n---\n", blocks);
        }
    }

    private static bool TryReadResults(JsonElement root, out JsonElement results)
    {
        results = default;
        return root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("results", out results)
            && results.ValueKind == JsonValueKind.Array;
    }

    private static List<SearchResultItem> ReadResults(JsonElement results)
    {
        var items = new List<SearchResultItem>();
        foreach (var result in results.EnumerateArray())
        {
            if (result.ValueKind != JsonValueKind.Object || ReadString(result, "url") is not string url)
            {
                continue;
            }

            items.Add(new SearchResultItem(
                ReadString(result, "title"),
                url,
                ReadString(result, "publish_date"),
                null,
                null,
                ReadStrings(result, "excerpts")));
        }

        return items;
    }

    private static List<string> ReadWarnings(JsonElement root)
    {
        var warnings = new List<string>();
        if (!root.TryGetProperty("warnings", out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return warnings;
        }

        foreach (var item in value.EnumerateArray())
        {
            var message = item.ValueKind switch
            {
                JsonValueKind.String => item.GetString(),
                JsonValueKind.Object => ReadString(item, "message"),
                _ => null
            };
            if (!string.IsNullOrWhiteSpace(message))
            {
                warnings.Add(message.Trim());
            }
        }

        return warnings;
    }

    private static List<string> ReadStrings(JsonElement element, string propertyName)
    {
        var values = new List<string>();
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return values;
        }

        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
            {
                values.Add(item.GetString()!.Trim());
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
