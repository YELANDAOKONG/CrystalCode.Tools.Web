namespace CrystalCode.Web.Searching;

/// <summary>
/// Selects the search backend.
/// </summary>
internal enum SearchProvider
{
    Parallel,
    Exa,
    Tavily,
    Bocha,
    Perplexity,
    Jina,
    Firecrawl
}
