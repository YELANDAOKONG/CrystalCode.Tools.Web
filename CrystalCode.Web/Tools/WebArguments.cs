using System.Text.Json;

namespace CrystalCode.Web.Tools;

/// <summary>
/// Shared JSON argument helpers for the web tools.
/// </summary>
internal static class WebArguments
{
    public static bool TryOpen(
        string json,
        out JsonElement root,
        out JsonDocument? document,
        out string? error)
    {
        root = default;
        document = null;
        error = null;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            error = "Tool arguments are not valid JSON.";
            return false;
        }

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            document.Dispose();
            document = null;
            error = "Tool arguments must be a JSON object.";
            return false;
        }

        root = document.RootElement;
        return true;
    }

    public static bool TryRejectUnknown(
        JsonElement root,
        string[] allowed,
        out string? error)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (!Array.Exists(allowed, name => string.Equals(name, property.Name, StringComparison.Ordinal)))
            {
                error = $"Tool arguments must contain only {JoinNames(allowed)}.";
                return false;
            }
        }

        error = null;
        return true;
    }

    public static bool TryReadRequiredString(
        JsonElement root,
        string name,
        out string value,
        out string? error)
    {
        value = string.Empty;
        if (!root.TryGetProperty(name, out var element) || element.ValueKind != JsonValueKind.String)
        {
            error = $"Argument '{name}' must be a non-empty string.";
            return false;
        }

        var text = element.GetString()!.Trim();
        if (text.Length == 0)
        {
            error = $"Argument '{name}' must be a non-empty string.";
            return false;
        }

        value = text;
        error = null;
        return true;
    }

    public static bool TryReadOptionalString(
        JsonElement root,
        string name,
        out string? value,
        out string? error)
    {
        value = null;
        error = null;
        if (!root.TryGetProperty(name, out var element) || element.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            error = $"Argument '{name}' must be a string.";
            return false;
        }

        var text = element.GetString()!.Trim();
        value = text.Length > 0 ? text : null;
        return true;
    }

    public static bool TryReadBoundedInt(
        JsonElement root,
        string name,
        int defaultValue,
        int maximum,
        out int value,
        out string? error)
    {
        if (!root.TryGetProperty(name, out var element) || element.ValueKind == JsonValueKind.Null)
        {
            value = defaultValue;
            error = null;
            return true;
        }

        return TryReadInteger(element, name, maximum, out value, out error);
    }

    public static bool TryReadOptionalBoundedInt(
        JsonElement root,
        string name,
        int maximum,
        out int? value,
        out string? error)
    {
        value = null;
        error = null;
        if (!root.TryGetProperty(name, out var element) || element.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (!TryReadInteger(element, name, maximum, out var parsed, out error))
        {
            return false;
        }

        value = parsed;
        return true;
    }

    public static bool TryReadEnum(
        JsonElement root,
        string name,
        string defaultValue,
        string[] choices,
        out string value,
        out string? error)
    {
        if (!root.TryGetProperty(name, out var element) || element.ValueKind == JsonValueKind.Null)
        {
            value = defaultValue;
            error = null;
            return true;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            var text = element.GetString()!.Trim();
            if (Array.Exists(choices, choice => string.Equals(choice, text, StringComparison.Ordinal)))
            {
                value = text;
                error = null;
                return true;
            }
        }

        value = defaultValue;
        error = $"Argument '{name}' must be {JoinChoices(choices)}.";
        return false;
    }

    private static bool TryReadInteger(
        JsonElement element,
        string name,
        int maximum,
        out int value,
        out string? error)
    {
        value = 0;
        error = null;
        if (element.ValueKind != JsonValueKind.Number
            || !element.TryGetDouble(out var number)
            || number != Math.Floor(number)
            || number < 1
            || number > maximum)
        {
            error = $"Argument '{name}' must be an integer between 1 and {maximum}.";
            return false;
        }

        value = (int)number;
        return true;
    }

    private static string JoinNames(string[] names) => names.Length switch
    {
        0 => string.Empty,
        1 => names[0],
        2 => $"{names[0]} and {names[1]}",
        _ => string.Join(", ", names[..^1]) + $", and {names[^1]}"
    };

    private static string JoinChoices(string[] choices) => choices.Length switch
    {
        0 => string.Empty,
        1 => $"'{choices[0]}'",
        2 => $"'{choices[0]}' or '{choices[1]}'",
        _ => string.Join(", ", choices[..^1].Select(choice => $"'{choice}'")) + $", or '{choices[^1]}'"
    };
}
