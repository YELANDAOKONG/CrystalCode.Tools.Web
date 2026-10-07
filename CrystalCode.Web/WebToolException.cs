namespace CrystalCode.Web;

/// <summary>
/// A web tool failure with a model-visible English message.
/// </summary>
internal sealed class WebToolException : Exception
{
    public WebToolException(string message)
        : base(message)
    {
    }
}
