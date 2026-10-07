using CrystalCode.Web.Searching;

namespace CrystalCode.Web.Tests.Searching;

public sealed class SearchArgumentsTests
{
    [Fact]
    public void ReadsDefaults()
    {
        var json = """{"query":"crystal code"}""";

        Assert.True(SearchArguments.TryRead(json, TestEnvironment.Empty, out var request, out var error), error);
        Assert.Equal("crystal code", request.Query);
        Assert.Equal(SearchProvider.Parallel, request.Provider);
        Assert.Equal(8, request.NumResults);
        Assert.Equal(10_000, request.ContextMaxCharacters);
        Assert.Equal("auto", request.Type);
        Assert.Equal("fallback", request.Livecrawl);
        Assert.Null(request.SessionId);
        Assert.Null(request.ModelName);
        Assert.Null(request.MaxCharacters);
    }

    [Fact]
    public void ReadsEveryArgument()
    {
        var json = """
            {
              "query": "  crystal code  ",
              "numResults": 12,
              "contextMaxCharacters": 2500,
              "type": "deep",
              "livecrawl": "preferred",
              "session_id": " session-1 ",
              "model_name": "gemini-3",
              "maxCharacters": 4000
            }
            """;

        Assert.True(SearchArguments.TryRead(json, TestEnvironment.Empty, out var request, out var error), error);
        Assert.Equal("crystal code", request.Query);
        Assert.Equal(12, request.NumResults);
        Assert.Equal(2500, request.ContextMaxCharacters);
        Assert.Equal("deep", request.Type);
        Assert.Equal("preferred", request.Livecrawl);
        Assert.Equal("session-1", request.SessionId);
        Assert.Equal("gemini-3", request.ModelName);
        Assert.Equal(4000, request.MaxCharacters);
    }

    [Theory]
    [InlineData("exa")]
    [InlineData("EXA")]
    [InlineData("parallel")]
    public void ReadsTheProviderFromTheEnvironment(string configured)
    {
        var environment = TestEnvironment.From((SearchArguments.ProviderVariable, configured));

        Assert.True(
            SearchArguments.TryRead("""{"query":"x"}""", environment, out var request, out var error),
            error);
        Assert.True(SearchProviders.TryParse(configured, out var expected));
        Assert.Equal(expected, request.Provider);
    }

    [Fact]
    public void RejectsAnUnknownProviderInTheEnvironment()
    {
        var environment = TestEnvironment.From((SearchArguments.ProviderVariable, "comet"));

        Assert.False(
            SearchArguments.TryRead("""{"query":"x"}""", environment, out _, out var error));
        Assert.Equal("Provider must be 'exa' or 'parallel'.", error);
    }

    [Fact]
    public void RejectsAMissingQuery()
    {
        Assert.False(SearchArguments.TryRead("{}", TestEnvironment.Empty, out _, out var error));
        Assert.Equal("Argument 'query' must be a non-empty string.", error);
    }

    [Fact]
    public void RejectsAnUnknownProperty()
    {
        Assert.False(
            SearchArguments.TryRead(
                """{"query":"x","limit":5}""",
                TestEnvironment.Empty,
                out _,
                out var error));
        Assert.Equal(
            "Tool arguments must contain only query, numResults, contextMaxCharacters, type, livecrawl, session_id, model_name, and maxCharacters.",
            error);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("21")]
    [InlineData("8.5")]
    public void RejectsANumResultsOutsideTheRange(string value)
    {
        var json = $$"""{"query":"x","numResults":{{value}}}""";

        Assert.False(SearchArguments.TryRead(json, TestEnvironment.Empty, out _, out var error));
        Assert.Equal("Argument 'numResults' must be an integer between 1 and 20.", error);
    }

    [Fact]
    public void RejectsASearchTypeOutsideTheChoices()
    {
        Assert.False(
            SearchArguments.TryRead(
                """{"query":"x","type":"slow"}""",
                TestEnvironment.Empty,
                out _,
                out var error));
        Assert.Equal("Argument 'type' must be 'auto', 'fast', or 'deep'.", error);
    }

    [Fact]
    public void RejectsALivecrawlOutsideTheChoices()
    {
        Assert.False(
            SearchArguments.TryRead(
                """{"query":"x","livecrawl":"always"}""",
                TestEnvironment.Empty,
                out _,
                out var error));
        Assert.Equal("Argument 'livecrawl' must be 'fallback' or 'preferred'.", error);
    }

    [Fact]
    public void RejectsAnOptionalStringOfTheWrongType()
    {
        Assert.False(
            SearchArguments.TryRead(
                """{"query":"x","session_id":5}""",
                TestEnvironment.Empty,
                out _,
                out var error));
        Assert.Equal("Argument 'session_id' must be a string.", error);
    }

    [Fact]
    public void RejectsMaxCharactersOutsideTheRange()
    {
        Assert.False(
            SearchArguments.TryRead(
                """{"query":"x","maxCharacters":100001}""",
                TestEnvironment.Empty,
                out _,
                out var error));
        Assert.Equal("Argument 'maxCharacters' must be an integer between 1 and 100000.", error);
    }

    [Fact]
    public void IgnoresNullOptionals()
    {
        var json = """{"query":"x","type":null,"session_id":null,"maxCharacters":null}""";

        Assert.True(SearchArguments.TryRead(json, TestEnvironment.Empty, out var request, out var error), error);
        Assert.Equal("auto", request.Type);
        Assert.Null(request.SessionId);
        Assert.Null(request.MaxCharacters);
    }

    [Fact]
    public void RejectsArgumentsThatAreNotAnObject()
    {
        Assert.False(SearchArguments.TryRead("[]", TestEnvironment.Empty, out _, out var error));
        Assert.Equal("Tool arguments must be a JSON object.", error);
    }

    [Fact]
    public void RejectsMalformedJson()
    {
        Assert.False(SearchArguments.TryRead("{", TestEnvironment.Empty, out _, out var error));
        Assert.Equal("Tool arguments are not valid JSON.", error);
    }
}
