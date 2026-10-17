using System.Text.Json;

namespace CrystalCode.Web.Searching;

/// <summary>
/// Converts a provider's structured search text into readable result blocks.
/// Plain text, such as the Exa payload, is returned unchanged.
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

            var blocks = new List<string>();
            var warnings = ReadWarnings(document.RootElement);
            if (warnings.Count > 0)
            {
                blocks.Add("Warnings:\n- " + string.Join("\n- ", warnings));
            }

            foreach (var result in results.EnumerateArray())
            {
                var block = FormatResult(result);
                if (block.Length > 0)
                {
                    blocks.Add(block);
                }
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

    private static string FormatResult(JsonElement result)
    {
        if (result.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        var url = ReadString(result, "url");
        if (url is null)
        {
            return string.Empty;
        }

        var lines = new List<string>
        {
            "Title: " + (ReadString(result, "title") ?? "N/A"),
            "URL: " + url,
            "Published: " + (ReadString(result, "publish_date") ?? "N/A")
        };
        var excerpts = ReadStrings(result, "excerpts");
        if (excerpts.Count > 0)
        {
            lines.Add("Highlights:");
            lines.Add(string.Join("\n\n", excerpts));
        }

        return string.Join("\n", lines);
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
