using CrystalCode.Web.Searching;

namespace CrystalCode.Web.Tests.Searching;

public sealed class RestSearchResponseTests
{
    [Fact]
    public void ReadsTavilyResults()
    {
        var body = """
            {
              "results": [
                {
                  "title": "Example",
                  "url": "https://example.com",
                  "content": "a useful excerpt",
                  "published_date": "2026-10-13"
                }
              ]
            }
            """;

        Assert.True(RestSearchResponse.TryRead(SearchProvider.Tavily, body, out var text, out var error));
        Assert.Null(error);
        Assert.Equal(
            """
            Title: Example
            URL: https://example.com
            Published: 2026-10-13
            Highlights:
            a useful excerpt
            """,
            text);
    }

    [Fact]
    public void ReadsBochaResults()
    {
        var body = """
            {
              "code": 200,
              "data": {
                "webPages": {
                  "value": [
                    {
                      "name": "Example",
                      "url": "https://example.com",
                      "summary": "a detailed summary",
                      "snippet": "a short snippet",
                      "siteName": "Example Site",
                      "datePublished": "2026-10-13T00:00:00+08:00"
                    }
                  ]
                }
              }
            }
            """;

        Assert.True(RestSearchResponse.TryRead(SearchProvider.Bocha, body, out var text, out var error));
        Assert.Null(error);
        Assert.Equal(
            """
            Title: Example
            URL: https://example.com
            Published: 2026-10-13T00:00:00+08:00
            Site: Example Site
            Highlights:
            a detailed summary
            """,
            text);
    }

    [Fact]
    public void ReadsPerplexityResults()
    {
        var body = """
            {
              "results": [
                {
                  "title": "Example",
                  "url": "https://example.com",
                  "snippet": "a useful excerpt",
                  "date": "2026-10-13",
                  "last_updated": "2026-10-14"
                }
              ],
              "id": "search-1"
            }
            """;

        Assert.True(RestSearchResponse.TryRead(SearchProvider.Perplexity, body, out var text, out var error));
        Assert.Null(error);
        Assert.Equal(
            """
            Title: Example
            URL: https://example.com
            Published: 2026-10-13
            Updated: 2026-10-14
            Highlights:
            a useful excerpt
            """,
            text);
    }

    [Fact]
    public void ReadsFirecrawlResults()
    {
        var body = """
            {
              "success": true,
              "data": {
                "web": [
                  {
                    "title": "Example",
                    "url": "https://example.com",
                    "description": "a useful excerpt"
                  }
                ]
              }
            }
            """;

        Assert.True(RestSearchResponse.TryRead(SearchProvider.Firecrawl, body, out var text, out var error));
        Assert.Null(error);
        Assert.Equal(
            """
            Title: Example
            URL: https://example.com
            Published: N/A
            Highlights:
            a useful excerpt
            """,
            text);
    }

    [Theory]
    [InlineData("tavily", """{"results":[]}""")]
    [InlineData("bocha", """{"code":200,"data":{"webPages":{"value":[]}}}""")]
    [InlineData("perplexity", """{"results":[]}""")]
    [InlineData("firecrawl", """{"success":true,"data":{"web":[]}}""")]
    public void ReturnsEmptyTextForNoResults(string providerName, string body)
    {
        Assert.True(SearchProviders.TryParse(providerName, out var provider));

        Assert.True(RestSearchResponse.TryRead(provider, body, out var text, out var error));
        Assert.Null(error);
        Assert.Equal(string.Empty, text);
    }

    [Theory]
    [InlineData("""{"code":403,"msg":"insufficient balance"}""")]
    [InlineData("""{"code":"403","message":"insufficient balance"}""")]
    public void ReadsABochaError(string body)
    {
        Assert.True(RestSearchResponse.TryRead(SearchProvider.Bocha, body, out var text, out var error));
        Assert.Null(text);
        Assert.Equal("The search provider returned an error: insufficient balance", error);
    }

    [Fact]
    public void ReadsAFirecrawlError()
    {
        var body = """{"success":false,"error":"rate limited"}""";

        Assert.True(RestSearchResponse.TryRead(SearchProvider.Firecrawl, body, out var text, out var error));
        Assert.Null(text);
        Assert.Equal("The search provider returned an error: rate limited", error);
    }

    [Fact]
    public void ReadsATavilyErrorDetail()
    {
        var body = """{"detail":{"error":"invalid api key"}}""";

        Assert.True(RestSearchResponse.TryRead(SearchProvider.Tavily, body, out var text, out var error));
        Assert.Null(text);
        Assert.Equal("The search provider returned an error: invalid api key", error);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    public void RejectsBodiesThatAreNotJsonObjects(string body)
    {
        Assert.False(RestSearchResponse.TryRead(SearchProvider.Tavily, body, out var text, out var error));
        Assert.Null(text);
        Assert.Equal("The search provider returned an invalid JSON response.", error);
    }
}
