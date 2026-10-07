using System.Net;
using System.Text.RegularExpressions;

namespace CrystalCode.Web.Html;

/// <summary>
/// Converts HTML to Markdown with the same element mapping as the Python web
/// tool set: headings, links, emphasis, code, pre, lists, break, and rule.
/// </summary>
internal static class HtmlMarkdownConverter
{
    private static readonly Regex TrailingSpaces = new(
        @"[ \t]+\n",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex ExtraNewlines = new(
        @"\n{3,}",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    public static string Convert(string html)
    {
        ArgumentNullException.ThrowIfNull(html);
        var parts = new List<string>();
        var links = new Stack<string>();
        var skipDepth = 0;
        var preDepth = 0;
        foreach (var token in HtmlTokenizer.Tokenize(html))
        {
            switch (token.Kind)
            {
                case HtmlTokenKind.StartTag:
                    if (skipDepth > 0)
                    {
                        skipDepth++;
                        break;
                    }

                    if (HtmlElements.Skipped.Contains(token.Name))
                    {
                        skipDepth = 1;
                        break;
                    }

                    if (HtmlElements.Blocks.Contains(token.Name))
                    {
                        Block(parts);
                    }

                    AppendStart(parts, links, token, ref preDepth);
                    break;
                case HtmlTokenKind.EndTag:
                    if (skipDepth > 0)
                    {
                        skipDepth--;
                        break;
                    }

                    AppendEnd(parts, links, token.Name, ref preDepth);
                    if (HtmlElements.Blocks.Contains(token.Name))
                    {
                        Block(parts);
                    }

                    break;
                default:
                    if (skipDepth == 0)
                    {
                        parts.Add(preDepth > 0 ? token.Text : HtmlWhitespace.CollapseInline(token.Text));
                    }

                    break;
            }
        }

        var result = WebUtility.HtmlDecode(string.Concat(parts));
        result = TrailingSpaces.Replace(result, "\n");
        result = ExtraNewlines.Replace(result, "\n\n");
        return result.Trim();
    }

    private static void Block(List<string> parts)
    {
        if (parts.Count > 0 && !parts[^1].EndsWith("\n\n", StringComparison.Ordinal))
        {
            parts.Add("\n\n");
        }
    }

    private static void AppendStart(
        List<string> parts,
        Stack<string> links,
        HtmlToken token,
        ref int preDepth)
    {
        switch (token.Name)
        {
            case "h1":
            case "h2":
            case "h3":
            case "h4":
            case "h5":
            case "h6":
                parts.Add(new string('#', token.Name[1] - '0') + " ");
                break;
            case "li":
                parts.Add("- ");
                break;
            case "a":
                parts.Add("[");
                links.Push(token.Href ?? string.Empty);
                break;
            case "strong":
            case "b":
                parts.Add("**");
                break;
            case "em":
            case "i":
                parts.Add("*");
                break;
            case "code":
                if (preDepth == 0)
                {
                    parts.Add("`");
                }

                break;
            case "pre":
                preDepth++;
                parts.Add("```\n");
                break;
            case "br":
                parts.Add("\n");
                break;
            case "hr":
                parts.Add("\n---\n");
                break;
        }
    }

    private static void AppendEnd(
        List<string> parts,
        Stack<string> links,
        string name,
        ref int preDepth)
    {
        switch (name)
        {
            case "a":
                if (links.Count > 0)
                {
                    var href = links.Pop();
                    parts.Add(href.Length > 0 ? $"]({href})" : "]");
                }

                break;
            case "strong":
            case "b":
                parts.Add("**");
                break;
            case "em":
            case "i":
                parts.Add("*");
                break;
            case "code":
                if (preDepth == 0)
                {
                    parts.Add("`");
                }

                break;
            case "pre":
                if (preDepth > 0)
                {
                    preDepth--;
                    parts.Add("\n```");
                }

                break;
        }
    }
}
