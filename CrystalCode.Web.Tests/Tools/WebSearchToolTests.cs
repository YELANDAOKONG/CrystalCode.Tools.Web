using System.Net;
using System.Text.Json;

using Crystal.Tools;

using CrystalCode.Web.Tools;

namespace CrystalCode.Web.Tests.Tools;

public sealed class WebSearchToolTests
{
    [Fact]
    public void ExposesTheWebsearchDefinition()
    {
        var tool = new WebSearchTool();

        Assert.Equal("websearch", tool.Definition.Name);
        Assert.NotNull(tool.Definition.Description);
        using var schema = JsonDocument.Parse(tool.Definition.InputSchema.GetRawText());
        Assert.False(schema.RootElement.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal(
            "query",
            schema.RootElement.GetProperty("required").EnumerateArray().Single().GetString());
    }

    [Fact]
    public async Task RejectsInvalidArguments()
    {
        using var http = new HttpClient(new FakeHttpMessageHandler());
        var tool = new WebSearchTool(http, TestEnvironment.Empty);

        var output = await tool.InvokeAsync(new ToolCall("1", "websearch", "{}"));

        Assert.Equal(ToolResultStatus.Failure, output.Status);
        Assert.Equal("Argument 'query' must be a non-empty string.", output.Text);
    }

    [Fact]
    public async Task ReturnsSearchText()
    {
        var handler = new FakeHttpMessageHandler().Respond(
            """
            {"result":{"content":[{"type":"text","text":"found it"}]}}
            """);
        using var http = new HttpClient(handler);
        var tool = new WebSearchTool(http, TestEnvironment.Empty);

        var output = await tool.InvokeAsync(
            new ToolCall("1", "websearch", """{"query":"crystal"}"""));

        Assert.Equal(ToolResultStatus.Success, output.Status);
        Assert.Equal("found it", output.Text);
    }

    [Fact]
    public async Task ReturnsAFailureForAnHttpError()
    {
        var handler = new FakeHttpMessageHandler().Respond(
            _ => TestResponses.Status(HttpStatusCode.InternalServerError));
        using var http = new HttpClient(handler);
        var tool = new WebSearchTool(http, TestEnvironment.Empty);

        var output = await tool.InvokeAsync(
            new ToolCall("1", "websearch", """{"query":"crystal"}"""));

        Assert.Equal(ToolResultStatus.Failure, output.Status);
        Assert.Equal("Parallel search failed with HTTP status 500.", output.Text);
    }
}
