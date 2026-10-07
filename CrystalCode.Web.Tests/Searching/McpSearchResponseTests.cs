using CrystalCode.Web.Searching;

namespace CrystalCode.Web.Tests.Searching;

public sealed class McpSearchResponseTests
{
    [Fact]
    public void ReadsAPlainJsonBody()
    {
        var body = """
            {"jsonrpc":"2.0","id":1,"result":{"content":[{"type":"text","text":"hello"}]}}
            """;

        Assert.True(McpSearchResponse.TryRead(body, out var text, out var error));
        Assert.Equal("hello", text);
        Assert.Null(error);
    }

    [Fact]
    public void ReadsAnEventStreamBody()
    {
        var body = """
            event: message
            data: {"jsonrpc":"2.0","id":1,"result":{"content":[{"type":"text","text":"hello"}]}}
            """;

        Assert.True(McpSearchResponse.TryRead(body, out var text, out var error));
        Assert.Equal("hello", text);
        Assert.Null(error);
    }

    [Fact]
    public void JoinsMultipleTextItems()
    {
        var body = """
            {"result":{"content":[{"type":"text","text":"one"},{"type":"image","data":"x"},{"type":"text","text":"two"}]}}
            """;

        Assert.True(McpSearchResponse.TryRead(body, out var text, out var error));
        Assert.Equal("one\ntwo", text);
        Assert.Null(error);
    }

    [Fact]
    public void ReadsAProtocolError()
    {
        var body = """{"error":{"code":-32000,"message":"boom"}}""";

        Assert.True(McpSearchResponse.TryRead(body, out var text, out var error));
        Assert.Null(text);
        Assert.Equal("The search provider returned an error: boom", error);
    }

    [Fact]
    public void ReadsAResultError()
    {
        var body = """
            {"result":{"isError":true,"content":[{"type":"text","text":"rate limited"}]}}
            """;

        Assert.True(McpSearchResponse.TryRead(body, out var text, out var error));
        Assert.Null(text);
        Assert.Equal("rate limited", error);
    }

    [Fact]
    public void IgnoresBodiesWithoutContent()
    {
        Assert.False(McpSearchResponse.TryRead(
            """{"result":{"content":[]}}""",
            out var text,
            out var error));
        Assert.Null(text);
        Assert.Null(error);

        Assert.False(McpSearchResponse.TryRead("not json", out _, out _));
    }
}
