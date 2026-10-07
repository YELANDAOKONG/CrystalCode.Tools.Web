using System.Text.RegularExpressions;

namespace CrystalCode.Web.Html;

/// <summary>
/// Collapses HTML whitespace the same way the Python web tool set does.
/// </summary>
internal static class HtmlWhitespace
{
    private static readonly Regex Runs = new(
        @"\s+",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    public static string Collapse(string value) => Runs.Replace(value, " ").Trim();

    public static string CollapseInline(string value) => Runs.Replace(value, " ");
}
