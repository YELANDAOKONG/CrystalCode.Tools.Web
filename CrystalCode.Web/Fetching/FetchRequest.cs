namespace CrystalCode.Web.Fetching;

/// <summary>
/// One validated page fetch call.
/// </summary>
internal sealed record FetchRequest(
    Uri Url,
    PageFormat Format,
    int TimeoutSeconds,
    int? MaxCharacters);
