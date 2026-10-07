using System.Text.Json;

using CrystalCode.Web.Tools;

namespace CrystalCode.Web.Searching;

/// <summary>
/// Reads and validates one model-generated websearch argument object.
/// </summary>
internal static class SearchArguments
{
    public const string ProviderVariable = "CRYSTAL_WEBSEARCH_PROVIDER";

    private static readonly string[] AllowedProperties =
    [
        "query",
        "numResults",
        "contextMaxCharacters",
        "type",
        "livecrawl",
        "session_id",
        "model_name",
        "maxCharacters"
    ];

    private static readonly string[] SearchTypes = ["auto", "fast", "deep"];
    private static readonly string[] LivecrawlModes = ["fallback", "preferred"];

    public static bool TryRead(
        string json,
        Func<string, string?> environment,
        out SearchRequest request,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(environment);
        request = null!;
        if (!WebArguments.TryOpen(json, out var root, out var document, out error))
        {
            return false;
        }

        using (document)
        {
            if (!WebArguments.TryRejectUnknown(root, AllowedProperties, out error)
                || !WebArguments.TryReadRequiredString(root, "query", out var query, out error)
                || !TryReadProvider(environment, out var provider, out error)
                || !WebArguments.TryReadBoundedInt(
                    root,
                    "numResults",
                    WebLimits.DefaultResultCount,
                    WebLimits.MaximumResultCount,
                    out var numResults,
                    out error)
                || !WebArguments.TryReadBoundedInt(
                    root,
                    "contextMaxCharacters",
                    WebLimits.DefaultContextCharacters,
                    WebLimits.MaximumContextCharacters,
                    out var contextMaxCharacters,
                    out error)
                || !WebArguments.TryReadEnum(root, "type", SearchTypes[0], SearchTypes, out var searchType, out error)
                || !WebArguments.TryReadEnum(root, "livecrawl", LivecrawlModes[0], LivecrawlModes, out var livecrawl, out error)
                || !WebArguments.TryReadOptionalString(root, "session_id", out var sessionId, out error)
                || !WebArguments.TryReadOptionalString(root, "model_name", out var modelName, out error)
                || !WebArguments.TryReadOptionalBoundedInt(
                    root,
                    "maxCharacters",
                    WebLimits.MaximumCharacters,
                    out var maxCharacters,
                    out error))
            {
                return false;
            }

            request = new SearchRequest(
                query,
                provider,
                numResults,
                contextMaxCharacters,
                searchType,
                livecrawl,
                sessionId,
                modelName,
                maxCharacters);
            return true;
        }
    }

    private static bool TryReadProvider(
        Func<string, string?> environment,
        out SearchProvider provider,
        out string? error)
    {
        error = null;
        var configured = environment(ProviderVariable);
        if (string.IsNullOrWhiteSpace(configured))
        {
            provider = SearchProvider.Parallel;
            return true;
        }

        if (SearchProviders.TryParse(configured.Trim(), out provider))
        {
            return true;
        }

        error = $"Provider must be '{SearchProviders.ExaName}' or '{SearchProviders.ParallelName}'.";
        return false;
    }
}
