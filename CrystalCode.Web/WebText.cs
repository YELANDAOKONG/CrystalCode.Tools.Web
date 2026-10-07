namespace CrystalCode.Web;

/// <summary>
/// Applies the model-requested character budget to returned tool text.
/// </summary>
internal static class WebText
{
    public static string Truncate(string text, int? maximumCharacters)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (maximumCharacters is not int maximum || text.Length <= maximum)
        {
            return text;
        }

        return text[..maximum] + $"\n[truncated to {maximum} characters]";
    }
}
