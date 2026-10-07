using System.Net;

namespace CrystalCode.Web.Html;

/// <summary>
/// Scans HTML into start tags, end tags, and decoded text without building a
/// tree. Comments and declarations are dropped. A lone '&lt;' stays text.
/// </summary>
internal static class HtmlTokenizer
{
    public static List<HtmlToken> Tokenize(string html)
    {
        ArgumentNullException.ThrowIfNull(html);
        var tokens = new List<HtmlToken>();
        var index = 0;
        while (index < html.Length)
        {
            var opening = html.IndexOf('<', index);
            if (opening < 0)
            {
                AddData(tokens, html[index..]);
                break;
            }

            if (opening > index)
            {
                AddData(tokens, html[index..opening]);
            }

            if (html.AsSpan(opening).StartsWith("<!--", StringComparison.Ordinal))
            {
                var commentEnd = html.IndexOf("-->", opening + 4, StringComparison.Ordinal);
                index = commentEnd < 0 ? html.Length : commentEnd + 3;
                continue;
            }

            if (!TryReadTag(html, opening, out var token, out var afterTag))
            {
                tokens.Add(HtmlToken.Data("<"));
                index = opening + 1;
                continue;
            }

            if (token is { } value)
            {
                tokens.Add(value);
            }

            index = afterTag;
        }

        return tokens;
    }

    private static void AddData(List<HtmlToken> tokens, string text)
    {
        if (text.Length > 0)
        {
            tokens.Add(HtmlToken.Data(WebUtility.HtmlDecode(text)));
        }
    }

    private static bool TryReadTag(
        string html,
        int opening,
        out HtmlToken? token,
        out int afterTag)
    {
        token = null;
        afterTag = opening + 1;
        var next = opening + 1;
        if (next >= html.Length)
        {
            return false;
        }

        var character = html[next];
        if (character == '!' || character == '?')
        {
            var declarationEnd = html.IndexOf('>', next + 1);
            if (declarationEnd < 0)
            {
                return false;
            }

            afterTag = declarationEnd + 1;
            return true;
        }

        if (character == '/')
        {
            var closeEnd = html.IndexOf('>', next + 1);
            if (closeEnd < 0)
            {
                return false;
            }

            var closeName = ReadName(html, next + 1, closeEnd);
            afterTag = closeEnd + 1;
            if (closeName.Length > 0)
            {
                token = HtmlToken.EndTag(closeName);
            }

            return true;
        }

        if (!IsAsciiLetter(character))
        {
            return false;
        }

        var tagEnd = html.IndexOf('>', next + 1);
        if (tagEnd < 0)
        {
            return false;
        }

        var name = ReadName(html, next, tagEnd);
        if (name.Length == 0)
        {
            return false;
        }

        token = HtmlToken.StartTag(name, ReadHref(html, next + name.Length, tagEnd));
        afterTag = tagEnd + 1;
        return true;
    }

    private static string ReadName(string html, int start, int end)
    {
        var index = start;
        while (index < end && IsNameCharacter(html[index]))
        {
            index++;
        }

        return html[start..index].ToLowerInvariant();
    }

    private static string? ReadHref(string html, int start, int end)
    {
        var index = start;
        while (index < end)
        {
            if (char.IsWhiteSpace(html[index]) || html[index] == '/')
            {
                index++;
                continue;
            }

            var nameStart = index;
            while (index < end
                && !char.IsWhiteSpace(html[index])
                && html[index] != '='
                && html[index] != '/')
            {
                index++;
            }

            var name = html[nameStart..index];
            while (index < end && char.IsWhiteSpace(html[index]))
            {
                index++;
            }

            if (index >= end || html[index] != '=')
            {
                continue;
            }

            index++;
            while (index < end && char.IsWhiteSpace(html[index]))
            {
                index++;
            }

            var (value, afterValue) = ReadAttributeValue(html, index, end);
            index = afterValue;
            if (string.Equals(name, "href", StringComparison.OrdinalIgnoreCase))
            {
                return WebUtility.HtmlDecode(value);
            }
        }

        return null;
    }

    private static (string Value, int AfterValue) ReadAttributeValue(string html, int start, int end)
    {
        if (start < end && (html[start] == '"' || html[start] == '\''))
        {
            var quote = html[start];
            var valueStart = start + 1;
            var closing = html.IndexOf(quote, valueStart);
            if (closing < 0 || closing > end)
            {
                return (html[valueStart..end], end);
            }

            return (html[valueStart..closing], closing + 1);
        }

        var unquotedStart = start;
        var index = start;
        while (index < end && !char.IsWhiteSpace(html[index]))
        {
            index++;
        }

        return (html[unquotedStart..index], index);
    }

    private static bool IsNameCharacter(char value) =>
        char.IsAsciiLetterOrDigit(value) || value is '-' or '_' or ':';

    private static bool IsAsciiLetter(char value) =>
        (value >= 'a' && value <= 'z') || (value >= 'A' && value <= 'Z');
}
