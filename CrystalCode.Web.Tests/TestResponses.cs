using System.Net;
using System.Text;

namespace CrystalCode.Web.Tests;

/// <summary>
/// Scripted HTTP responses for the fake handler.
/// </summary>
internal static class TestResponses
{
    public static HttpResponseMessage Json(string body) =>
        Content(HttpStatusCode.OK, body, "application/json");

    public static HttpResponseMessage Html(string body) =>
        Content(HttpStatusCode.OK, body, "text/html");

    public static HttpResponseMessage Text(string body, string mediaType = "text/plain") =>
        Content(HttpStatusCode.OK, body, mediaType);

    public static HttpResponseMessage Status(HttpStatusCode status) => new(status);

    public static HttpResponseMessage CloudflareChallenge()
    {
        var response = Status(HttpStatusCode.Forbidden);
        response.Headers.Add("cf-mitigated", "challenge");
        return response;
    }

    private static HttpResponseMessage Content(
        HttpStatusCode status,
        string body,
        string mediaType) =>
        new(status)
        {
            Content = new StringContent(body, Encoding.UTF8, mediaType)
        };
}
