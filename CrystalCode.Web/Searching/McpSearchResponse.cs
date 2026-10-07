using System.Text.Json;

namespace CrystalCode.Web.Searching;

/// <summary>
/// Reads result text or an error from one MCP response body. The body is a
/// plain JSON object or an event-stream body with <c>data:</c> lines.
/// </summary>
internal static class McpSearchResponse
{
    public static bool TryRead(string body, out string? text, out string? error)
    {
        ArgumentNullException.ThrowIfNull(body);
        text = null;
        error = null;
        foreach (var candidate in Candidates(body))
        {
            if (!candidate.StartsWith('{'))
            {
                continue;
            }

            JsonDocument? document = null;
            try
            {
                document = JsonDocument.Parse(candidate);
            }
            catch (JsonException)
            {
                continue;
            }

            using (document)
            {
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (TryReadProtocolError(root, out error))
                {
                    return true;
                }

                if (TryReadResultError(root, out error))
                {
                    return true;
                }

                if (TryReadText(root, out text))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static IEnumerable<string> Candidates(string body)
    {
        var trimmed = body.Trim();
        if (trimmed.Length > 0)
        {
            yield return trimmed;
        }

        foreach (var line in body.Split('\n'))
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("data: ", StringComparison.Ordinal))
            {
                yield return trimmedLine[6..].Trim();
            }
        }
    }

    private static bool TryReadProtocolError(JsonElement root, out string? error)
    {
        error = null;
        if (!root.TryGetProperty("error", out var value) || value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (value.TryGetProperty("message", out var message)
            && message.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(message.GetString()))
        {
            error = "The search provider returned an error: " + message.GetString()!.Trim();
        }
        else
        {
            error = "The search provider returned an error.";
        }

        return true;
    }

    private static bool TryReadResultError(JsonElement root, out string? error)
    {
        error = null;
        if (!root.TryGetProperty("result", out var result)
            || result.ValueKind != JsonValueKind.Object
            || !result.TryGetProperty("isError", out var isError)
            || isError.ValueKind != JsonValueKind.True)
        {
            return false;
        }

        error = ReadTextItems(result, out var text) && !string.IsNullOrWhiteSpace(text)
            ? text
            : "The search provider returned an error.";
        return true;
    }

    private static bool TryReadText(JsonElement root, out string? text)
    {
        text = null;
        if (!root.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!ReadTextItems(result, out var joined) || string.IsNullOrWhiteSpace(joined))
        {
            return false;
        }

        text = joined;
        return true;
    }

    private static bool ReadTextItems(JsonElement result, out string joined)
    {
        joined = string.Empty;
        if (!result.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var parts = new List<string>();
        foreach (var item in content.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object
                && item.TryGetProperty("text", out var value)
                && value.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(value.GetString()))
            {
                parts.Add(value.GetString()!);
            }
        }

        if (parts.Count == 0)
        {
            return false;
        }

        joined = string.Join("\n", parts);
        return true;
    }
}
