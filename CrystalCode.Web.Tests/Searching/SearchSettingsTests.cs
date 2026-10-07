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
}
