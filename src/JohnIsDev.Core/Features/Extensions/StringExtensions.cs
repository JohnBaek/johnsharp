using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Ganss.Xss;

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
    /// A regex pattern used to match Korean mobile phone numbers starting with "02" followed by 11 numeric digits.
    /// </summary>
    private static readonly HtmlSanitizer Sanitizer = new HtmlSanitizer();

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
    /// 한국 전화번호의 국가번호를 국내 접두번호로 변환하고 필요하면 하이픈을 붙인다.
    /// 해외 번호나 지원하지 않는 형식은 숫자와 국제번호 기호를 유지하여 반환한다.
    /// 번호의 유효성은 별도로 검사해야 한다.
    /// </summary>
    /// <param name="input">변환할 유선 또는 휴대전화 번호</param>
    /// <param name="isHyphen">국내 번호에 하이픈을 포함할지 여부</param>
    /// <returns>국내 형식으로 변환한 번호 또는 정리한 원본 번호</returns>
    public static string ToKoreanPhoneNumber(this string input, bool isHyphen = true)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "";

        // 표시용 괄호와 구분자를 제거하되, 해외 번호의 국가번호는 임의로 변경하지 않는다.
        string cleanedNumber = Regex.Replace(input, @"[^0-9+]", "");
        string nationalNumber = cleanedNumber;
        if (cleanedNumber.StartsWith("+82", StringComparison.Ordinal))
            nationalNumber = "0" + cleanedNumber[3..].TrimStart('0');
        else if (cleanedNumber.StartsWith("0082", StringComparison.Ordinal))
            nationalNumber = "0" + cleanedNumber[4..].TrimStart('0');
        else if (cleanedNumber.StartsWith("82", StringComparison.Ordinal))
            nationalNumber = "0" + cleanedNumber[2..].TrimStart('0');

        // 국내 번호로 인정되는 경우에만 변환을 적용한다. 잘못된 국가번호를 추측하여 보정하지 않는다.
        if (!nationalNumber.IsValidPhoneNumber())
            return cleanedNumber;

        if (!isHyphen)
            return nationalNumber;

        // 서울 지역번호는 두 자리, 그 외 지원하는 지역번호와 이동전화 접두번호는 세 자리이다.
        int prefixLength = nationalNumber.StartsWith("02", StringComparison.Ordinal) ? 2 : 3;
        int middleLength = nationalNumber.Length - prefixLength - 4;
        return $"{nationalNumber[..prefixLength]}-{nationalNumber.Substring(prefixLength, middleLength)}-{nationalNumber[^4..]}";
    }

    /// <summary>
    /// Validate Phone Number
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static bool IsValidPhoneNumber(this string input)
    {
        // 필수값 누락과 공백 입력을 차단한다.
        if (string.IsNullOrWhiteSpace(input))
            return false;

        // 010은 11자리, 지역번호 유선 및 070은 각각의 자릿수를 검사한다.
        return Regex.IsMatch(
            input.Trim(),
            @"\A(?:010[0-9]{8}|01[16789][0-9]{7,8}|02[0-9]{7,8}|0(?:3[1-3]|4[1-4]|5[1-5]|6[1-4])[0-9]{7,8}|070[0-9]{8})\z"
        );
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

    /// <summary>
    /// Sanitizes the provided HTML string to remove potentially harmful or unwanted elements and attributes.
    /// </summary>
    /// <param name="html">The input HTML string to be sanitized.</param>
    /// <returns>The sanitized HTML string with harmful or unwanted content removed.</returns>
    public static string SanitizeHtml(this string html)
        => string.IsNullOrWhiteSpace(html) ? html : Sanitizer.Sanitize(html);
    
    /// <summary>
    /// String to Digit Only
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string ToDigitOnly(this string input)
        => Regex.Replace(input, "[^0-9]", "");
    
    /// <summary>
    /// String to Decimal
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static decimal ToDecimal(this string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return 0m;
        var numericPart = new string(input.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());
        return decimal.TryParse(numericPart, out var result) ? result : 0m;
    }

    /// <summary>
    /// Attempts to parse the string representation of a date in specific formats and outputs the parsed <see cref="DateTime"/>.
    /// </summary>
    /// <param name="value">The string to parse as a date.</param>
    /// <param name="dateTime">
    /// When this method returns, contains the <see cref="DateTime"/> value equivalent to the date contained in the input string, if the parsing succeeds; otherwise, the default value for <see cref="DateTime"/>.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <c>true</c> if the string was successfully parsed into a <see cref="DateTime"/> value; otherwise, <c>false</c>.
    /// </returns>
    public static bool TryParseDate(
        this string value,
        out DateTime dateTime)
    {
        return DateTime.TryParseExact(
            value,
            ["yyyyMMdd", "yyyy-MM-dd"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out dateTime);
    }
}