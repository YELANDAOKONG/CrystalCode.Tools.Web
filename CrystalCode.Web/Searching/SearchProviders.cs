namespace CrystalCode.Web.Searching;

/// <summary>
/// Parses and names <see cref="SearchProvider"/> values.
/// </summary>
internal static class SearchProviders
{
    public const string ParallelName = "parallel";
    public const string ExaName = "exa";

    public static bool TryParse(string? value, out SearchProvider provider)
    {
        if (string.Equals(value, ExaName, StringComparison.OrdinalIgnoreCase))
        {
            provider = SearchProvider.Exa;
            return true;
        }

        if (string.Equals(value, ParallelName, StringComparison.OrdinalIgnoreCase))
        {
            provider = SearchProvider.Parallel;
            return true;
        }

        provider = SearchProvider.Parallel;
        return false;
    }

    public static string Title(SearchProvider provider) => provider switch
    {
        SearchProvider.Exa => "Exa",
        SearchProvider.Parallel => "Parallel",
        _ => throw new ArgumentOutOfRangeException(nameof(provider))
    };
}
