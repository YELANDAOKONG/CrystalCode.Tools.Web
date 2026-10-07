namespace CrystalCode.Web;

/// <summary>
/// Reads a response body while enforcing a byte ceiling.
/// </summary>
internal static class WebResponse
{
    private const int ReadBufferBytes = 64 * 1024;

    public static async Task<byte[]> ReadBytesAsync(
        HttpResponseMessage response,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (response.Content.Headers.ContentLength is long length && length > maximumBytes)
        {
            throw new WebToolException(TooLarge(maximumBytes));
        }

        await using var stream = await response.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        var buffer = new byte[ReadBufferBytes];
        using var content = new MemoryStream();
        while (true)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            content.Write(buffer, 0, read);
            if (content.Length > maximumBytes)
            {
                throw new WebToolException(TooLarge(maximumBytes));
            }
        }

        return content.ToArray();
    }

    private static string TooLarge(int maximumBytes) =>
        $"Response too large (exceeds {maximumBytes} bytes).";
}
