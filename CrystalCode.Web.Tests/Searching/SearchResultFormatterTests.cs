using CrystalCode.Web.Searching;

namespace CrystalCode.Web.Tests.Searching;

public sealed class SearchResultFormatterTests
{
    [Fact]
    public void FormatsStructuredResults()
    {
        var text = """
            {
              "search_id": "search_1",
              "results": [
                {
                  "url": "https://example.com/a",
                  "title": "First",
                  "publish_date": "2026-10-13",
                  "excerpts": ["one", "two"]
                },
                {
                  "url": "https://example.com/b",
                  "title": null,
                  "publish_date": null,
                  "excerpts": ["three"]
                }
              ],
              "session_id": "session_1"
            }
            """;

        Assert.Equal(
            """
            Title: First
            URL: https://example.com/a
            Published: 2026-10-13
            Highlights:
            one

            two
            ---
            Title: N/A
            URL: https://example.com/b
            Published: N/A
            Highlights:
            three
            """,
            SearchResultFormatter.Format(text));
    }

    [Fact]
    public void LeavesPlainTextUnchanged()
    {
        var text = """
            Title: Example
            URL: https://example.com
            Published: N/A
            Highlights:
            hello
            """;

        Assert.Equal(text, SearchResultFormatter.Format(text));
    }

    [Theory]
    [InlineData("""{"search_id":"search_1"}""")]
    [InlineData("""{"results":"not-an-array"}""")]
    [InlineData("""{"results":[}""")]
    public void LeavesUnrecognizedJsonUnchanged(string text)
    {
        Assert.Equal(text, SearchResultFormatter.Format(text));
    }

    [Fact]
    public void ReturnsAnEmptyTextWhenThereAreNoResults()
    {
        Assert.Equal(
            string.Empty,
            SearchResultFormatter.Format("""{"search_id":"search_1","results":[]}"""));
    }

    [Fact]
    public void IncludesWarnings()
    {
        var text = """
            {
              "results": [],
              "warnings": [
                {"type": "warning", "message": "query was adjusted"},
                {"type": "warning", "message": "results were capped"}
              ]
            }
            """;

        Assert.Equal(
            """
            Warnings:
            - query was adjusted
            - results were capped
            """,
            SearchResultFormatter.Format(text));
    }

    [Fact]
    public void SkipsResultsWithoutAUrl()
    {
        var text = """
            {
              "results": [
                {"title": "Broken", "excerpts": ["ignored"]},
                {"url": "https://example.com", "title": "Kept"}
              ]
            }
            """;

        Assert.Equal(
            """
            Title: Kept
            URL: https://example.com
            Published: N/A
            """,
            SearchResultFormatter.Format(text));
    }

    [Fact]
    public void FallsBackToTheRawPayloadWhenNoResultCanBeRead()
    {
        var text = """
            {"results":[{"link":"https://example.com","snippet":"a new schema"}]}
            """;

        Assert.Equal(text, SearchResultFormatter.Format(text));
    }
}
