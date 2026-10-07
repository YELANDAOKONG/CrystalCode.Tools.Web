namespace CrystalCode.Web.Html;

/// <summary>
/// One token from a tolerant HTML scan. Only the first href of a start tag is
/// kept, because that is the only attribute the extractors read.
/// </summary>
internal readonly record struct HtmlToken(
    HtmlTokenKind Kind,
    string Name,
    string? Href,
    string Text)
{
    public static HtmlToken StartTag(string name, string? href) =>
        new(HtmlTokenKind.StartTag, name, href, string.Empty);

    public static HtmlToken EndTag(string name) =>
        new(HtmlTokenKind.EndTag, name, null, string.Empty);

    public static HtmlToken Data(string text) =>
        new(HtmlTokenKind.Text, string.Empty, null, text);
}
