namespace CrystalCode.Web.Html;

/// <summary>
/// Extracts normalized visible text from HTML. Skipped elements contribute
/// nothing; every other element contributes its text without separators.
/// </summary>
internal static class HtmlTextExtractor
{
    public static string ExtractText(string html)
    {
        ArgumentNullException.ThrowIfNull(html);
        var parts = new List<string>();
        var skipDepth = 0;
        foreach (var token in HtmlTokenizer.Tokenize(html))
        {
            switch (token.Kind)
            {
                case HtmlTokenKind.StartTag:
                    if (skipDepth > 0)
                    {
                        skipDepth++;
                    }
                    else if (HtmlElements.Skipped.Contains(token.Name))
                    {
                        skipDepth = 1;
                    }

                    break;
                case HtmlTokenKind.EndTag:
                    if (skipDepth > 0)
                    {
                        skipDepth--;
                    }

                    break;
                default:
                    if (skipDepth == 0)
                    {
                        parts.Add(token.Text);
                    }

                    break;
            }
        }

        return HtmlWhitespace.Collapse(string.Concat(parts));
    }
}
