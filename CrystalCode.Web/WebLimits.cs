namespace CrystalCode.Web;

/// <summary>
/// Model-facing and transport limits for the web tools.
/// </summary>
internal static class WebLimits
{
    public const int DefaultResultCount = 8;
    public const int MaximumResultCount = 20;
    public const int DefaultContextCharacters = 10_000;
    public const int MaximumContextCharacters = 50_000;
    public const int MaximumCharacters = 100_000;
    public const int DefaultFetchTimeoutSeconds = 30;
    public const int MaximumFetchTimeoutSeconds = 120;
    public const int SearchTimeoutSeconds = 25;
    public const int MaximumSearchResponseBytes = 512 * 1024;
    public const int MaximumFetchResponseBytes = 5 * 1024 * 1024;
}
