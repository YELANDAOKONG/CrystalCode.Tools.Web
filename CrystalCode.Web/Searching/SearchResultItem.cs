namespace CrystalCode.Web.Searching;

/// <summary>
/// One normalized search result before it is rendered as model-facing text.
/// </summary>
internal sealed record SearchResultItem(
    string? Title,
    string Url,
    string? Published,
    string? Site,
    string? Updated,
    IReadOnlyList<string> Highlights);
