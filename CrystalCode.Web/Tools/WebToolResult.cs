using Crystal.Tools;

namespace CrystalCode.Web.Tools;

/// <summary>
/// Builds model-visible results for the web tools.
/// </summary>
internal static class WebToolResult
{
    public static ToolOutput Failure(string message) => new(message, ToolResultStatus.Failure);
}
