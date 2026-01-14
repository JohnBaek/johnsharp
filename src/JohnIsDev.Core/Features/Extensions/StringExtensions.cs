using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace JohnIsDev.Core.Features.Extensions;

/// <summary>
/// An Extension class for the String
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// A regex pattern used to match Korean mobile phone numbers starting with "010" followed by 8 numeric digits.
    /// </summary>
    /// <returns>A compiled regular expression for validating Korean mobile phone numbers.</returns>
    [GeneratedRegex(@"^010\d{8}$")]
    private static partial Regex KoreanMobileRegex();

    /// <summary>
    /// A regex used to match any non-numeric characters in a string.
    /// </summary>
    /// <returns>A compiled regular expression for identifying non-numeric characters.</returns>
    [GeneratedRegex(@"[^\d]")]
    private static partial Regex NonNumericRegex();

    /// <summary>
    /// A regex used to match any email address in a string.
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$")]
    private static partial Regex EmailRegex();

    /// <summary>
    /// Hashing with SHA
    /// </summary>
    /// <param name="input">대상 문자열</param>
    /// <returns>해싱 결과</returns>
    public static string WithDateTime(this string input)
        =>  $"[{DateTime.Now:yyyy-MM-dd hh:mm:ss}] {input}";

    /// <summary>
    /// Parsing to Guid
    /// </summary>
    /// <param name="input">대상 문자열</param>
    /// <returns>해싱 결과</returns>
    public static Guid ToGuid(this string input)
    {
        if (Guid.TryParse(input, out Guid ourGuid))
        {
            return ourGuid;
        }

        return "00000000-0000-0000-0000-000000000000".ToGuid();
    }

    /// <summary>
    /// Determines whether the specified string is null, empty, or consists only of white-space characters.
    /// </summary>
    /// <param name="input">The string to evaluate.</param>
    /// <returns>
    /// <c>true</c> if the specified string is null, empty, or consists only of white-space characters; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsEmpty(this string input)
        => string.IsNullOrWhiteSpace(input);

    /// <summary>
    /// Checks if the current object is not empty
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static bool IsNotEmpty(this string input)
        => !string.IsNullOrWhiteSpace(input);


    /// <summary>
    /// String to Int
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static int ToInt(this string? input)
    {
        if (input == null)
            return 0;
            
        return int.Parse(input);
    }

    /// <summary>
    /// Checks if the current object, which is a regex pattern, matches the provided input string.
    /// </summary>
    /// <param name="pattern">The regex pattern to match against the input string.</param>
    /// <param name="input">The input string to be checked against the regex pattern.</param>
    /// <returns>True if the regex pattern matches the input string; otherwise, false.</returns>
    private static bool IsMatch(this string pattern, string input)
    {
        Regex regex = new Regex(pattern: pattern);
        return regex.Match(input).Success;
    }
    
    /// <summary>
    /// Checks if the current object 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static bool IsValidEmail(this string input)
        => !string.IsNullOrEmpty(input) && EmailRegex().IsMatch(input);

    /// <summary>
    /// Checks if the current object is an invalid email
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static bool IsInValidEmail(this string input)
        => input.IsValidEmail() == false;
    
    /// <summary>
    /// Validate Email value
    /// </summary>
    /// <param name="input">Input Email</param>
    /// <returns>Validation</returns>
    public static bool IsValidEmailWithDomain(this string input)
    {
        try
        {
            var addr = new MailAddress(input);
            string host = addr.Host;

            // 도메인 존재 확인
            // IPHostEntry entry = Dns.GetHostEntry(host);

            // MX 레코드 검사 (간단 예시)
            var addresses = Dns.GetHostAddresses(host);
            return addresses.Any(address => address.AddressFamily == AddressFamily.InterNetwork);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Normalizes a given string by removing all non-alphanumeric characters and converting it to uppercase.
    /// </summary>
    /// <param name="input">The input string to be normalized.</param>
    /// <returns>A normalized string containing only alphanumeric characters in uppercase. Returns an empty string if an exception occurs.</returns>
    public static string NormalizeText(this string input)
    {
        string pattern = "[^a-zA-Z가-힣0-9]";
        try
        {
            return Regex.Replace(input, pattern, "").ToUpper();
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Validate Korean Phone Number
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static bool IsValidKoreanPhoneNumber(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;
        string onlyDigits = input.ToExactNumberStringOnly();
        return KoreanMobileRegex().IsMatch(onlyDigits);
    }

    /// <summary>
    /// Determines whether the specified string is an invalid Korean mobile phone number.
    /// </summary>
    /// <param name="input">The string to evaluate.</param>
    /// <returns>
    /// <c>true</c> if the specified string is not a valid Korean mobile phone number; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsInValidKoreanPhoneNumber(this string input)
        => input.IsValidKoreanPhoneNumber() == false;

    /// <summary>
    /// 한국 휴대전화 번호 형식으로 변환한다.
    /// </summary>
    /// <param name="input">변환할 휴대전화 번호 문자열</param>
    /// <param name="isHyphen">하이픈을 포함할지 여부</param>
    /// <returns>한국 휴대전화 번호 형식으로 변환된 문자열</returns>
    public static string ToKoreanPhoneNumber(this string input, bool isHyphen = true)
    {
        if (input.IsEmpty())
            return "";

        string cleanedNumber = Regex.Replace(input, @"[^\d\+]", "");
        if (cleanedNumber.StartsWith("+82"))
            cleanedNumber = "0" + cleanedNumber.Substring(3);

        if (isHyphen)
        {
            string formattedNumber = cleanedNumber;
            try
            {
                if (cleanedNumber.Length == 11 && cleanedNumber.StartsWith("010"))
                {
                    formattedNumber = Regex.Replace(cleanedNumber, @"(\d{3})(\d{4})(\d{4})", "$1-$2-$3");
                }
                else if (cleanedNumber.StartsWith("02"))
                {
                    if (cleanedNumber.Length == 9)
                    {
                        formattedNumber = Regex.Replace(cleanedNumber, @"(\d{2})(\d{3})(\d{4})", "$1-$2-$3");
                    }
                    else if (cleanedNumber.Length == 10)
                    {
                        formattedNumber = Regex.Replace(cleanedNumber, @"(\d{2})(\d{4})(\d{4})", "$1-$2-$3");
                    }
                }
                else if (cleanedNumber.Length == 10)
                {
                    formattedNumber = Regex.Replace(cleanedNumber, @"(\d{3})(\d{3})(\d{4})", "$1-$2-$3");
                }
            }
            catch
            {
                return cleanedNumber;
            }
            return formattedNumber;
        }

        return cleanedNumber;
    }
    
    
    /// <summary>
    /// Validate Phone Number
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static bool IsValidPhoneNumber(this string input)
    {
        // Replace hyphens
        string onlyDigits = input.Replace("-", "");
        var regex = new Regex(@"^[0-9]+$");
        return regex.IsMatch(onlyDigits);
    }

    /// <summary>
    /// Checks if the phone number is invalid.
    /// </summary>
    /// <param name="input">The phone number string to be checked.</param>
    /// <returns>True if the phone number is invalid; otherwise, false.</returns>
    public static bool IsInvalidPhoneNumber(this string input)
        => input.IsValidPhoneNumber() == false;

    /// <summary>
    /// Removes all non-numeric characters from the input string and returns the resulting string.
    /// </summary>
    /// <param name="input">The input string from which non-numeric characters will be removed.</param>
    /// <returns>A string containing only numeric characters extracted from the input, or an empty string if the input is null or empty.</returns>
    public static string ToExactNumberStringOnly(this string input)
        => string.IsNullOrEmpty(input) ? "" : NonNumericRegex().Replace(input, "");

    /// <summary>
    /// Extract IFrame Src
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string ExtractIframeSrc(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "";

        var regex = new Regex(@"<iframe[^>]*\ssrc=['""]([^'""]+)['""]", RegexOptions.IgnoreCase);
        var match = regex.Match(input);

        if (match.Success)
        {
            // 캡처된 첫 번째 그룹(URL) 반환
            return match.Groups[1].Value;
        }

        return input;
    }
}