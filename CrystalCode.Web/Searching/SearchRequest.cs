namespace CrystalCode.Web.Searching;

/// <summary>
/// One validated web search call.
/// </summary>
internal sealed record SearchRequest(
    string Query,
    SearchProvider Provider,
    int NumResults,
    int ContextMaxCharacters,
    string Type,
    string Livecrawl,
    string? SessionId,
    string? ModelName,
    int? MaxCharacters);
