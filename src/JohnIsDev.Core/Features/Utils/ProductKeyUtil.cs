namespace JohnIsDev.Core.Features.Utils;

/// <summary>
/// Provides utility methods for generating product keys based on a category key and sequence number.
/// </summary>
public static class ProductKeyUtil
{
    /// <summary>
    /// Generates a product key using a specified category key and product sequence.
    /// </summary>
    /// <param name="cateogryKey">The category key, which must be a 3-character string.</param>
    /// <param name="productSequnce">The sequence number used to generate the product key.</param>
    /// <returns>A string representing the generated product key in the format: [categoryKey]-[YYMMDD]-[sequenceNumber].</returns>
    /// <exception cref="ArgumentException">Thrown when the category key is null, empty, or not 3 characters long.</exception>
    public static string GetProductKey(string cateogryKey, int productSequnce)
    {
        if (string.IsNullOrWhiteSpace(cateogryKey) || cateogryKey.Length != 3)
        {
            throw new ArgumentException("카테고리 키는 3자리 문자열이어야 합니다.");
        }
        
        // 현재 날짜 가져오기 (yyyyMMdd 형식)
        string datePart = DateTime.Now.ToString("yyMMdd");
        
        // 시퀀스를 12자리로 포맷팅
        string sequenceString = productSequnce.ToString("D12");
        
        // 최종 제품 키 조합
        return $"{cateogryKey}-{datePart}-{sequenceString}";
    }
}