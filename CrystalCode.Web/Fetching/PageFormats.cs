namespace CrystalCode.Web.Fetching;

/// <summary>
/// Parses and names <see cref="PageFormat"/> values.
/// </summary>
internal static class PageFormats
{
    private static readonly string[] Names = ["text", "markdown", "html"];

    public static IReadOnlyList<string> Choices => Names;

    public static bool TryParse(string? value, out PageFormat format)
    {
        switch (value)
        {
            case "text":
                format = PageFormat.Text;
                return true;
            case "markdown":
                format = PageFormat.Markdown;
                return true;
            case "html":
                format = PageFormat.Html;
                return true;
            default:
                format = PageFormat.Markdown;
                return false;
        }
    }

    public static string Name(PageFormat format) => format switch
    {
        PageFormat.Text => "text",
        PageFormat.Markdown => "markdown",
        PageFormat.Html => "html",
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };
}
