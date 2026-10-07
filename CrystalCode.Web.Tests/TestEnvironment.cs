namespace CrystalCode.Web.Tests;

/// <summary>
/// Builds environment lookups for tests.
/// </summary>
internal static class TestEnvironment
{
    public static Func<string, string?> Empty => _ => null;

    public static Func<string, string?> From(params (string Name, string Value)[] values)
    {
        var map = values.ToDictionary(
            value => value.Name,
            value => value.Value,
            StringComparer.Ordinal);
        return name => map.TryGetValue(name, out var value) ? value : null;
    }
}
