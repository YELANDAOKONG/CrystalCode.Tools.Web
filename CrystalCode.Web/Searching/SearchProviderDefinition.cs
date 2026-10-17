namespace CrystalCode.Web.Searching;

/// <summary>
/// Describes one search backend, its transport, and its configuration.
/// </summary>
internal sealed record SearchProviderDefinition(
    SearchProvider Provider,
    string Name,
    string Title,
    SearchTransport Transport,
    string DefaultEndpoint,
    string EndpointVariable,
    string KeyVariable,
    bool RequiresKey,
    string? ToolName);
