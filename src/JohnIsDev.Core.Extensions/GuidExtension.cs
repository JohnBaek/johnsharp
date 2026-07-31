using System.Security.Cryptography;

namespace JohnIsDev.Core.Extensions;

/// <summary>
/// Provides extension methods for converting between Guid and URL-safe Base64 string representations.
/// </summary>
public static class GuidExtension
{
    /// <summary>
    /// Converts a Guid to a URL-safe Base64 string representation with 22 characters.
    /// </summary>
    /// <param name="guid">The Guid to convert into a URL-safe Base64 string.</param>
    /// <returns>A 22-character URL-safe Base64 string representation of the Guid.</returns>
    public static string ToShortString(this Guid guid)
    {
        return Convert.ToBase64String(guid.ToByteArray())
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>
    /// URL Safe Base64 문자열(22자리)을 Guid로 변환합니다.
    /// </summary>
    /// <param name="value">22자리 URL Safe Base64 문자열</param>
    /// <returns>Guid</returns>
    /// <exception cref="ArgumentNullException">입력 값이 null이거나 공백인 경우</exception>
    public static Guid ToGuid(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentNullException(nameof(value));

        string base64 = value
            .Replace('-', '+')
            .Replace('_', '/');

        switch (base64.Length % 4)
        {
            case 2:
                base64 += "==";
                break;
            case 3:
                base64 += "=";
                break;
        }

        return new Guid(Convert.FromBase64String(base64));
    }
}