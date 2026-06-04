using JohnIsDev.Core.Extensions;
using JohnIsDev.Core.Models.Common.Enums;

namespace JohnIsDev.Core.Models.Responses;

/// <summary>
/// 응답 리스트 데이터 클래스
/// </summary>
public class ResponseList<T> : Response 
{
    /// <summary>
    /// 응답 리스트를 나타내는 데이터 클래스.
    /// </summary>
    /// <typeparam name="T">리스트에 포함된 항목의 타입.</typeparam>
    public ResponseList()
    {
    }

    public ResponseList(EnumResponseResult result, List<T>? items) : this(result, "", "", items)
    {
    }


    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="result"></param>
    /// <param name="code"></param>
    /// <param name="message"></param>
    /// <param name="items"></param>
    public ResponseList(EnumResponseResult result, string code, string message, List<T>? items) 
        : base(result, code, message)
    {
        Items = items ?? [];
    }
    
    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="result"></param>
    /// <param name="code"></param>
    /// <param name="message"></param>
    public ResponseList(EnumResponseResult result, string code, string message) 
        : base(result, code, message)
    {
    }

    /// <summary>
    /// 스킵
    /// </summary>
    public int Skip { get; set; } = 0;

    /// <summary>
    /// 페이지 카운트 
    /// </summary>
    public int PageCount { get; set; } = 20;
    
    /// <summary>
    /// 전체 수
    /// </summary>
    public int TotalCount { get; set; } = 0;
    
    /// <summary>
    /// 응답 데이터 목록
    /// </summary>
    public List<T> Items { get; set; } = new List<T>();

    /// <summary>
    /// Converts the current <see cref="ResponseList{T}"/> to a new <see cref="ResponseList{TConvert}"/>,
    /// transforming each item in the collection to the target type using the <c>FromCopyValue</c> extension method.
    /// </summary>
    /// <typeparam name="TConvert">The target type to convert the items in the list to.</typeparam>
    /// <returns>A new instance of <see cref="ResponseList{TConvert}"/> with converted items and copied metadata.</returns>
    public ResponseList<TConvert> ToConvert<TConvert>() where TConvert : class, new()
    {
        return new ResponseList<TConvert>
        {
            Result = Result ,
            Code = Code ,
            Message = Message ,
            Items = Items.Select(i => i.FromCopyValue<TConvert>()).ToList(),
        };
    }
}