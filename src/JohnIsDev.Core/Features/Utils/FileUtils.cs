using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text;
using JohnIsDev.Core.Models.Common.Enums;
using JohnIsDev.Core.Models.Responses;
using Microsoft.Extensions.Logging;

namespace JohnIsDev.Core.Features.Utils;

/// <summary>
/// Represents a file Utils
/// </summary>
[SuppressMessage("ReSharper", "UseUtf8StringLiteral")]
public class FileUtils(ILogger<FileUtils> logger)
{
    /// <summary>
    /// Array of size suffixes used to represent data sizes in human-readable formats.
    /// Ranges from "bytes" to "YB" (yottabytes).
    /// </summary>
    private static readonly string[] SizeSuffixes = ["bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"];
    
    /// <summary>
    /// Known Magic numbers for files
    /// </summary>
    private static readonly Dictionary<string, List<byte[]>> KnownSignatures = new()
    {
        { "pdf", [[0x25, 0x50, 0x44, 0x46]] },
        { "png", [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]] },
        { "jpg", [
            [0xFF, 0xD8, 0xFF, 0xE0],
            [0xFF, 0xD8, 0xFF, 0xE1],
            [0xFF, 0xD8, 0xFF, 0xDB],
            [0xFF, 0xD8, 0xFF, 0xEE] // 추가 JPEG 시그니처
        ]},
        { "xlsx", [[0x50, 0x4B, 0x03, 0x04]] }, // ZIP 시그니처이므로 추가 검증 필요
        { "docx", [[0x50, 0x4B, 0x03, 0x04]] }, // DOCX 추가
        { "zip", [[0x50, 0x4B, 0x03, 0x04]] }
    };

    /// <summary>
    /// IsValidMagicNumber
    /// </summary>
    /// <param name="bytes">bytes</param>
    /// <param name="expectedExtension">expectedExtension</param>
    /// <returns></returns>
    public ResponseData<bool> IsValidMagicNumber(byte[] bytes, string expectedExtension)
    {
        try
        {
            if (bytes.Length < 4)
                return new ResponseData<bool>(EnumResponseResult.Error,"FileNotValid","올바른 파일이 아닙니다.");
            
            // Cut useless character
            string extension = expectedExtension.ToLower().Replace(".", "");
            switch (extension)
            {
                // In case Text file
                case "txt":
                    return ValidateTextFile(bytes);
                // In case XLSX file
                case "xlsx":
                    return ValidateOfficeDocument(bytes, "xl/");
            }

            // Validate a magic number
            if (!KnownSignatures.TryGetValue(extension, out var willTestSignatureBytes))
            {
                logger.LogWarning($"Unknown file extension for magic number validation: {extension}");
                return new ResponseData<bool>(EnumResponseResult.Error,"FileNotValid","올바른 파일이 아닙니다.");
            }
            
            // Get all testable Signatures

            // Tests all Signature
            foreach (byte[] signature in willTestSignatureBytes)
            {
                // pass If less 
                if (bytes.Length < signature.Length) 
                    continue;
                
                bool matches = !signature.Where((t, i) => bytes[i] != t).Any();
                if (matches)
                    return new ResponseData<bool>(EnumResponseResult.Success, "", "");
            }

            return new ResponseData<bool>(EnumResponseResult.Error, "FileNotValid", "올바른 파일이 아닙니다.");
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return new ResponseData<bool>(EnumResponseResult.Error,"FileNotValid","에러가 발생했습니다.");
        }
    }

    /// <summary>
    /// ValidateTextFile
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    private ResponseData<bool> ValidateTextFile(byte[] bytes)
    {
        try
        {
            // BOM 검사
            if (HasUtf8Bom(bytes) || HasUtf16Bom(bytes))
                return new ResponseData<bool>(EnumResponseResult.Success, "", "");

            // UTF-8 유효성 검사
            try
            {
                var decoder = Encoding.UTF8.GetDecoder();
                decoder.Fallback = DecoderFallback.ExceptionFallback;
            
                char[] chars = new char[decoder.GetCharCount(bytes, 0, bytes.Length)];
                decoder.GetChars(bytes, 0, bytes.Length, chars, 0);
            
                return new ResponseData<bool>(EnumResponseResult.Success, "", "");
            }
            catch (DecoderFallbackException)
            {
                return new ResponseData<bool>(EnumResponseResult.Error, "InvalidTextFile", "유효하지 않은 텍스트 파일입니다.");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "텍스트 파일 검증 중 오류 발생");
            return new ResponseData<bool>(EnumResponseResult.Error, "ValidationError", "파일 검증 중 오류가 발생했습니다.");
        }
    }

    /// <summary>
    /// Office 문서 내부 구조 검증
    /// </summary>
    private ResponseData<bool> ValidateOfficeDocument(byte[] bytes, string expectedPath)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        
            // XLSX should have xl/ Folder
            // DOCX should have word/ Folder
            bool isValid = archive.Entries.Any(entry => entry.FullName.StartsWith(expectedPath));
            return new ResponseData<bool>(isValid ? EnumResponseResult.Success : EnumResponseResult.Error,"","");
        }
        catch(Exception e)
        {
            logger.LogError(e, e.Message);
            return new ResponseData<bool>(EnumResponseResult.Error,"","");
        }
    }
    
    private static bool HasUtf8Bom(byte[] bytes) =>
        bytes is [0xEF, 0xBB, 0xBF, ..];

    private static bool HasUtf16Bom(byte[] bytes) =>
        bytes.Length >= 2 && ((bytes[0] == 0xFF && bytes[1] == 0xFE) || (bytes[0] == 0xFE && bytes[1] == 0xFF));

    /// <summary>
    /// Converts a file size in bytes to a human-readable string representation using the appropriate size suffix (e.g., KB, MB, GB).
    /// </summary>
    /// <param name="value">The file size in bytes to convert.</param>
    /// <param name="decimalPlaces">The number of decimal places to include in the output. Defaults to 1.</param>
    /// <returns>A human-readable string representation of the file size with the appropriate size suffix.</returns>
    public static string ToReadableSize(long value, int decimalPlaces = 1)
    {
        if (value < 0)
        {
            return "-" + ToReadableSize(-value, decimalPlaces); }
        if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

        int mag = (int)Math.Log(value, 1024);

        decimal adjustedSize = (decimal)value / (1L << (mag * 10));

        if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
        {
            mag += 1;
            adjustedSize /= 1024;
        }

        return string.Format("{0:n" + decimalPlaces + "} {1}", adjustedSize, SizeSuffixes[mag]);
    }
    
}