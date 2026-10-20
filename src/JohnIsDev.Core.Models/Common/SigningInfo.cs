namespace JohnIsDev.Core.Models.Common;

/// <summary>
/// Represents information required for signing operations, such as a cryptographic signature.
/// </summary>
public class SigningInfo
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="timeStamp"></param>
    /// <param name="nonce"></param>
    /// <param name="signature"></param>
    public SigningInfo(long timeStamp, string nonce, string signature)
    {
        TimeStamp = timeStamp;
        Nonce = nonce;
        Signature = signature;
    }

    /// <summary>
    /// Represents the Unix timestamp indicating the specific point in time related to the signing information.
    /// </summary>
    public long TimeStamp { get; set; }

    /// <summary>
    /// A unique, one-time value used to prevent replay attacks or duplicate requests.
    /// </summary>
    public string Nonce { get; set; }

    /// <summary>
    /// Represents a cryptographic signature used to validate the integrity and authenticity of the data.
    /// </summary>
    public string Signature { get; set; }
}