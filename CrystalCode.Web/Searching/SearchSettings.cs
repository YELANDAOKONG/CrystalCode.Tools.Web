namespace CrystalCode.Web.Searching;

/// <summary>
/// Resolved endpoint and optional credential for one search call. The search
/// credentials come from the host environment and are never written to output.
/// </summary>
internal sealed record SearchSettings(Uri Endpoint, string? ApiKey)
{
    private const string ExaEndpoint = "https://mcp.exa.ai/mcp";
    private const string ParallelEndpoint = "https://search.parallel.ai/mcp";
    private const string ExaEndpointVariable = "CRYSTAL_EXA_MCP_URL";
    private const string ParallelEndpointVariable = "CRYSTAL_PARALLEL_MCP_URL";
    private const string ExaKeyVariable = "EXA_API_KEY";
    private const string ParallelKeyVariable = "PARALLEL_API_KEY";

    public static SearchSettings Resolve(
        SearchProvider provider,
        Func<string, string?> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        var endpointVariable = provider == SearchProvider.Exa
            ? ExaEndpointVariable
            : ParallelEndpointVariable;
        var fallback = provider == SearchProvider.Exa ? ExaEndpoint : ParallelEndpoint;
        var configured = environment(endpointVariable);
        var endpointText = string.IsNullOrWhiteSpace(configured) ? fallback : configured.Trim();
        if (!Uri.TryCreate(endpointText, UriKind.Absolute, out var endpoint)
            || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
        {
            throw new WebToolException(
                $"Environment variable {endpointVariable} must be an absolute http or https URL.");
        }

        var keyVariable = provider == SearchProvider.Exa ? ExaKeyVariable : ParallelKeyVariable;
        var key = environment(keyVariable);
        return new SearchSettings(endpoint, string.IsNullOrWhiteSpace(key) ? null : key.Trim());
    }
}
