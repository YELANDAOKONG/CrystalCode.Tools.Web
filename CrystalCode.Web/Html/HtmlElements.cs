namespace CrystalCode.Web.Html;

/// <summary>
/// Element sets shared by the text and Markdown extractors. They mirror the
/// Python web tool set so both implementations drop and break the same tags.
/// </summary>
internal static class HtmlElements
{
    public static readonly HashSet<string> Skipped = new(StringComparer.Ordinal)
    {
        "script",
        "style",
        "noscript",
        "iframe",
        "object",
        "embed"
    };

    public static readonly HashSet<string> Blocks = new(StringComparer.Ordinal)
    {
        "address",
        "article",
        "aside",
        "blockquote",
        "div",
        "dl",
        "fieldset",
        "figcaption",
        "figure",
        "footer",
        "form",
        "h1",
        "h2",
        "h3",
        "h4",
        "h5",
        "h6",
        "header",
        "hr",
        "li",
        "main",
        "nav",
        "ol",
        "p",
        "pre",
        "section",
        "table",
        "tr",
        "ul"
    };
}
