using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using JohnIsDev.Core.Models.Common;

namespace JohnIsDev.Core.Features.Utils;

/// <summary>
/// A utility class for generating HMAC signing information.
/// </summary>
public class SignUtil
{
    /// <summary>
    /// HMAC 서명에 사용할 32바이트 난수 키를 생성한다.
    /// </summary>
    /// <returns>Base64로 인코딩한 키를 반환한다.</returns>
    public static string GenerateKey()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    /// <summary>
    /// Hmac 사이닝 정보를 생성한다.
    /// </summary>
    /// <param name="clientSecret">시크릿 정보</param>
    /// <param name="loginId">로그인 아이디</param>
    /// <returns></returns>
    public SigningInfo GenerateSign(string clientSecret, string loginId)
    {
        // Prepare materials
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string nonce = Convert.ToBase64String(Guid.CreateVersion7().ToByteArray());
        string message = string.Join(
            "\n" ,
            timestamp.ToString(CultureInfo.InvariantCulture),
            nonce,
            loginId
        );
        
        // Generate HMAC signature
        byte[] hmacHashBytes = HMACSHA256.HashData(Encoding.UTF8.GetBytes(clientSecret), Encoding.UTF8.GetBytes(message));
        return new SigningInfo(timestamp, nonce, Convert.ToBase64String(hmacHashBytes), loginId);
    }

    /// <summary>
    /// Validates the given signing information by comparing the provided signature
    /// with the one generated using the client secret and signing parameters.
    /// </summary>
    /// <param name="signingInfo">The signing information containing the timestamp, nonce, and signature to validate.</param>
    /// <param name="loginId"></param>
    /// <param name="clientSecret">The client secret used to generate the expected signature.</param>
    /// <returns>True if the provided signature matches the expected signature; otherwise, false.</returns>
    public bool ValidateSigning(SigningInfo signingInfo, string loginId, string clientSecret)
    {
        try
        {
            // 필요한 정보가 없는 경우 
            if (string.IsNullOrWhiteSpace(clientSecret) ||
                string.IsNullOrWhiteSpace(signingInfo.Nonce) ||
                string.IsNullOrWhiteSpace(signingInfo.Signature))
                return false;
        
            // SHA-256 서명은 32바이트이고 Base64로 인코딩하면 44문자
            if(signingInfo.Signature.Length != 44)
                return false;
        
            // Base64 를 디코딩해서 Bytes 로 변환한다.
            byte[] receivedSignature = Convert.FromBase64String(signingInfo.Signature);
            
            // 서명 정보를 조립한다.
            string message = string.Join(
                "\n",
                signingInfo.TimeStamp.ToString(CultureInfo.InvariantCulture),
                signingInfo.Nonce,
                loginId
                );
            
            // HMAC 서명정보를 생성한다.
            byte[] expectedSignature = HMACSHA256.HashData(Encoding.UTF8.GetBytes(clientSecret), Encoding.UTF8.GetBytes(message));
            
            // 서명정보 일치 결과를 리턴한다.
            return CryptographicOperations.FixedTimeEquals(receivedSignature, expectedSignature);
        }
        catch (FormatException)
        {
            return false;
        }
        catch 
        {
            return false;
        }
    }
}