namespace CrystalCode.Web.Searching;

/// <summary>
/// Defines and parses the available <see cref="SearchProvider"/> values.
/// </summary>
internal static class SearchProviders
{
    public const string ParallelName = "parallel";
    public const string ExaName = "exa";
    public const string TavilyName = "tavily";
    public const string BochaName = "bocha";
    public const string PerplexityName = "perplexity";
    public const string JinaName = "jina";
    public const string FirecrawlName = "firecrawl";

    private static readonly SearchProviderDefinition[] Definitions =
    [
        new(
            SearchProvider.Parallel,
            ParallelName,
            "Parallel",
            SearchTransport.Mcp,
            "https://search.parallel.ai/mcp",
            "CRYSTAL_PARALLEL_MCP_URL",
            "PARALLEL_API_KEY",
            false,
            "web_search"),
        new(
            SearchProvider.Exa,
            ExaName,
            "Exa",
            SearchTransport.Mcp,
            "https://mcp.exa.ai/mcp",
            "CRYSTAL_EXA_MCP_URL",
            "EXA_API_KEY",
            false,
            "web_search_exa"),
        new(
            SearchProvider.Tavily,
            TavilyName,
            "Tavily",
            SearchTransport.Rest,
            "https://api.tavily.com/search",
            "CRYSTAL_TAVILY_SEARCH_URL",
            "TAVILY_API_KEY",
            true,
            null),
        new(
            SearchProvider.Bocha,
            BochaName,
            "Bocha",
            SearchTransport.Rest,
            "https://api.bocha.cn/v1/web-search",
            "CRYSTAL_BOCHA_SEARCH_URL",
            "BOCHA_API_KEY",
            true,
            null),
        new(
            SearchProvider.Perplexity,
            PerplexityName,
            "Perplexity",
            SearchTransport.Rest,
            "https://api.perplexity.ai/search",
            "CRYSTAL_PERPLEXITY_SEARCH_URL",
            "PERPLEXITY_API_KEY",
            true,
            null),
        new(
            SearchProvider.Jina,
            JinaName,
            "Jina",
            SearchTransport.Mcp,
            "https://mcp.jina.ai/v1",
            "CRYSTAL_JINA_MCP_URL",
            "JINA_API_KEY",
            true,
            "search_web"),
        new(
            SearchProvider.Firecrawl,
            FirecrawlName,
            "Firecrawl",
            SearchTransport.Rest,
            "https://api.firecrawl.dev/v2/search",
            "CRYSTAL_FIRECRAWL_SEARCH_URL",
            "FIRECRAWL_API_KEY",
            true,
            null)
    ];

    public static IReadOnlyList<SearchProviderDefinition> All => Definitions;

    public static bool TryParse(string? value, out SearchProvider provider)
    {
        foreach (var definition in Definitions)
        {
            if (string.Equals(value, definition.Name, StringComparison.OrdinalIgnoreCase))
            {
                provider = definition.Provider;
                return true;
            }
        }

        provider = SearchProvider.Parallel;
        return false;
    }

    public static SearchProviderDefinition Get(SearchProvider provider) =>
        Definitions.First(definition => definition.Provider == provider);

    public static string Title(SearchProvider provider) => Get(provider).Title;

    public static string DescribeChoices()
    {
        var names = Definitions.Select(definition => $"'{definition.Name}'").ToArray();
        return names.Length == 1
            ? names[0]
            : $"{string.Join(", ", names[..^1])}, or {names[^1]}";
    }
}
