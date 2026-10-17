namespace CrystalCode.Web.Searching;

/// <summary>
/// Renders normalized results in the same block format for every provider.
/// </summary>
internal static class SearchResultRenderer
{
    public static string Render(IEnumerable<SearchResultItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var blocks = new List<string>();
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Url))
            {
                continue;
            }

            var lines = new List<string>
            {
                "Title: " + (string.IsNullOrWhiteSpace(item.Title) ? "N/A" : item.Title.Trim()),
                "URL: " + item.Url.Trim(),
                "Published: " + (string.IsNullOrWhiteSpace(item.Published) ? "N/A" : item.Published.Trim())
            };
            if (!string.IsNullOrWhiteSpace(item.Site))
            {
                lines.Add("Site: " + item.Site.Trim());
            }

            if (!string.IsNullOrWhiteSpace(item.Updated))
            {
                lines.Add("Updated: " + item.Updated.Trim());
            }

            var highlights = item.Highlights
                .Where(highlight => !string.IsNullOrWhiteSpace(highlight))
                .Select(highlight => highlight.Trim())
                .ToArray();
            if (highlights.Length > 0)
            {
                lines.Add("Highlights:");
                lines.Add(string.Join("\n\n", highlights));
            }

            blocks.Add(string.Join("\n", lines));
        }

        return string.Join("\n---\n", blocks);
    }
}
