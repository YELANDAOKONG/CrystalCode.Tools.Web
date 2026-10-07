namespace CrystalCode.Web.Tests;

/// <summary>
/// A request snapshot taken while the fake handler still owns the message.
/// </summary>
internal sealed record CapturedRequest(
    string Method,
    Uri? Uri,
    IReadOnlyDictionary<string, string> Headers,
    string Body)
{
    public string? Header(string name) =>
        Headers.TryGetValue(name, out var value) ? value : null;
}
