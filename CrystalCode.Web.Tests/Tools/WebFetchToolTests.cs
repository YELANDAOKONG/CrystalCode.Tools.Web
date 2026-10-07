using System.Text.Json;

using Crystal.Tools;

using CrystalCode.Web.Tools;

namespace CrystalCode.Web.Tests.Tools;

public sealed class WebFetchToolTests
{
    [Fact]
    public void ExposesTheWebfetchDefinition()
    {
        var tool = new WebFetchTool();

        Assert.Equal("webfetch", tool.Definition.Name);
        Assert.NotNull(tool.Definition.Description);
        using var schema = JsonDocument.Parse(tool.Definition.InputSchema.GetRawText());
        Assert.False(schema.RootElement.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal(
            "url",
            schema.RootElement.GetProperty("required").EnumerateArray().Single().GetString());
    }

    [Fact]
    public async Task RejectsANonHttpUrl()
    {
        using var http = new HttpClient(new FakeHttpMessageHandler());
        var tool = new WebFetchTool(http);

        var output = await tool.InvokeAsync(
            new ToolCall("1", "webfetch", """{"url":"ftp://example.com"}"""));

        Assert.Equal(ToolResultStatus.Failure, output.Status);
        Assert.Equal("URL must use http:// or https://.", output.Text);
    }

    [Fact]
    public async Task ReturnsTheConvertedPage()
    {
        var handler = new FakeHttpMessageHandler().Respond(
            _ => TestResponses.Html("<h1>Title</h1><p>Body</p>"));
        using var http = new HttpClient(handler);
        var tool = new WebFetchTool(http);

        var output = await tool.InvokeAsync(
            new ToolCall("1", "webfetch", """{"url":"https://example.com/"}"""));

        Assert.Equal(ToolResultStatus.Success, output.Status);
        Assert.Equal("# Title\n\nBody", output.Text);
    }

    [Fact]
    public async Task ReturnsAFailureForUnsupportedContent()
    {
        var handler = new FakeHttpMessageHandler().Respond(
            _ => TestResponses.Text("binary", "image/png"));
        using var http = new HttpClient(handler);
        var tool = new WebFetchTool(http);

        var output = await tool.InvokeAsync(
            new ToolCall("1", "webfetch", """{"url":"https://example.com/"}"""));

        Assert.Equal(ToolResultStatus.Failure, output.Status);
        Assert.Equal("Unsupported fetched image content type: image/png.", output.Text);
    }
}
