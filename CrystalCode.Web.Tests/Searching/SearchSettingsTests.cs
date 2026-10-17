using CrystalCode.Web.Searching;

namespace CrystalCode.Web.Tests.Searching;

public sealed class SearchSettingsTests
{
    [Fact]
    public void ResolvesParallelDefaults()
    {
        var settings = SearchSettings.Resolve(SearchProvider.Parallel, TestEnvironment.Empty);

        Assert.Equal("https://search.parallel.ai/mcp", settings.Endpoint.ToString());
        Assert.Null(settings.ApiKey);
    }

    [Fact]
    public void ResolvesExaDefaults()
    {
        var settings = SearchSettings.Resolve(SearchProvider.Exa, TestEnvironment.Empty);

        Assert.Equal("https://mcp.exa.ai/mcp", settings.Endpoint.ToString());
        Assert.Null(settings.ApiKey);
    }

    [Fact]
    public void ReadsEndpointOverridesAndKeys()
    {
        var environment = TestEnvironment.From(
            ("CRYSTAL_EXA_MCP_URL", "http://localhost:1234/mcp"),
            ("EXA_API_KEY", "  secret  "));

        var settings = SearchSettings.Resolve(SearchProvider.Exa, environment);

        Assert.Equal("http://localhost:1234/mcp", settings.Endpoint.ToString());
        Assert.Equal("secret", settings.ApiKey);
    }

    [Fact]
    public void FallsBackWhenTheEndpointOverrideIsBlank()
    {
        var environment = TestEnvironment.From(("CRYSTAL_PARALLEL_MCP_URL", "   "));

        var settings = SearchSettings.Resolve(SearchProvider.Parallel, environment);

        Assert.Equal("https://search.parallel.ai/mcp", settings.Endpoint.ToString());
    }

    [Fact]
    public void RejectsAnInvalidEndpointOverride()
    {
        var environment = TestEnvironment.From(("CRYSTAL_EXA_MCP_URL", "not a url"));

        var exception = Assert.Throws<WebToolException>(
            () => SearchSettings.Resolve(SearchProvider.Exa, environment));

        Assert.Equal(
            "Environment variable CRYSTAL_EXA_MCP_URL must be an absolute http or https URL.",
            exception.Message);
    }

    [Theory]
    [InlineData(
        "tavily",
        "TAVILY_API_KEY",
        "https://api.tavily.com/search")]
    [InlineData(
        "bocha",
        "BOCHA_API_KEY",
        "https://api.bocha.cn/v1/web-search")]
    [InlineData(
        "perplexity",
        "PERPLEXITY_API_KEY",
        "https://api.perplexity.ai/search")]
    [InlineData(
        "jina",
        "JINA_API_KEY",
        "https://mcp.jina.ai/v1")]
    [InlineData(
        "firecrawl",
        "FIRECRAWL_API_KEY",
        "https://api.firecrawl.dev/v2/search")]
    public void ResolvesKeyedProviderDefaults(
        string providerName,
        string keyVariable,
        string endpoint)
    {
        Assert.True(SearchProviders.TryParse(providerName, out var provider));
        var environment = TestEnvironment.From((keyVariable, "  secret  "));

        var settings = SearchSettings.Resolve(provider, environment);

        Assert.Equal(endpoint, settings.Endpoint.ToString());
        Assert.Equal("secret", settings.ApiKey);
        Assert.True(settings.Definition.RequiresKey);
    }

    [Theory]
    [InlineData("tavily", "TAVILY_API_KEY")]
    [InlineData("bocha", "BOCHA_API_KEY")]
    [InlineData("perplexity", "PERPLEXITY_API_KEY")]
    [InlineData("jina", "JINA_API_KEY")]
    [InlineData("firecrawl", "FIRECRAWL_API_KEY")]
    public void RequiresAKeyForKeyedProviders(
        string providerName,
        string keyVariable)
    {
        Assert.True(SearchProviders.TryParse(providerName, out var provider));
        var exception = Assert.Throws<WebToolException>(
            () => SearchSettings.Resolve(provider, TestEnvironment.Empty));

        Assert.Equal(
            $"Environment variable {keyVariable} is required for {SearchProviders.Title(provider)} search.",
            exception.Message);
    }

    [Fact]
    public void ReadsANewProviderEndpointOverride()
    {
        var environment = TestEnvironment.From(
            ("CRYSTAL_TAVILY_SEARCH_URL", "http://localhost:1234/search"),
            ("TAVILY_API_KEY", "secret"));

        var settings = SearchSettings.Resolve(SearchProvider.Tavily, environment);

        Assert.Equal("http://localhost:1234/search", settings.Endpoint.ToString());
    }
}
