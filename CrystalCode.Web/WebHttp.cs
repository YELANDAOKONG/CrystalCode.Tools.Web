using System.Net;

namespace CrystalCode.Web;

/// <summary>
/// Provides the shared HTTP client for the web tools. The tools time each
/// request themselves, so the client has no timeout of its own.
/// </summary>
internal static class WebHttp
{
    private static readonly Lazy<HttpClient> Client = new(Create);

    public static HttpClient Shared => Client.Value;

    private static HttpClient Create()
    {
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(10)
        };

        return new HttpClient(handler)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
    }
}
