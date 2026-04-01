using System.Security.Cryptography;

namespace JohnIsDev.Core.Features.Utils;

/// <summary>
/// SessionKeyUtil
/// </summary>
public static class SessionKeyUtil
{
    /// <summary>
    /// Alphabet Salt
    /// </summary>
    private static readonly char[] _salt = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();

    /// <summary>
    /// Generates a random session key of the specified length using a predefined set of characters.
    /// </summary>
    /// <param name="length">The desired length of the session key. Defaults to 11 if not specified.</param>
    /// <returns>A randomly generated session key string.</returns>
    public static string Generate(int length = 11)
    {
        return string.Create(length, length, (span, state) =>
        {
            byte[] buffer = new byte[span.Length];
            RandomNumberGenerator.Fill(buffer);

            for (int i = 0; i < span.Length; i++)
            {
                span[i] = _salt[buffer[i] % _salt.Length];
            }
        });
    }
}