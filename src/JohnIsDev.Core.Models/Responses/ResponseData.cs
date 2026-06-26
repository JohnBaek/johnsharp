using System.Diagnostics.CodeAnalysis;
using JohnIsDev.Core.Extensions;
using JohnIsDev.Core.Models.Common.Enums;

namespace JohnIsDev.Core.Models.Responses;


/// <summary>
/// 응답 데이터 모델 T의 객체를 같이 포함하여 리턴한다.
/// </summary>
/// <typeparam name="T">T Data</typeparam>
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
public class ResponseData<T> : Response
{
    /// <summary>
    /// Represents a data model encapsulating the response, derived from a generic type, with additional response properties.
    /// </summary>
    /// <typeparam name="T">The type of the data object associated with the response.</typeparam>
    public ResponseData()
    {
    }

    /// <summary>
    /// Represents a response data model that includes an object of type T along with the response details.
    /// </summary>
    /// <typeparam name="T">The type of the data included in the response.</typeparam>
    public ResponseData(ResponseData<T> targetCopy)
    {
        Result = targetCopy.Result;
        Code = targetCopy.Code;
        Message = targetCopy.Message;
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="result"></param>
    /// <param name="code"></param>
    /// <param name="message"></param>
    /// <param name="data"></param>
    public ResponseData(EnumResponseResult result, string code, string message, T? data) : base(result, code, message)
    {
        Result = result;
        Code = code;
        Message = message;
        Data = data;
    }

    /// <summary>
    /// Represents a response model that includes the result status, code, message, and an optional data object of a specified type.
    /// </summary>
    /// <typeparam name="T">The type of the data object included in the response.</typeparam>
    public ResponseData(EnumResponseResult result, T? data) : base(result)
    {
        Result = result;
        Code = "";
        Message = "";
        Data = data;
    }
    
    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="result"></param>
    /// <param name="code"></param>
    /// <param name="message"></param>
    public  ResponseData(EnumResponseResult result, string code, string message) : base(result, code, message)
    {
        Result = result;
        Code = code;
        Message = message;
    }

    /// <summary>
    /// Represents a data model encapsulating the response, derived from a generic type, with additional support for customization of result types, codes, messages, and optional associated data.
    /// </summary>
    /// <typeparam name="T">The type of the data object associated with the response.</typeparam>
    public ResponseData(EnumResponseResult result) : base(result)
    {
        Result = result;
        Code = "";
        Message = "";
    }


    /// <summary>
    /// 응답 데이터
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Converts the current response data to a new type and returns it as a new ResponseData object.
    /// </summary>
    /// <typeparam name="TConvert">The type to convert the data to.</typeparam>
    /// <returns>A new ResponseData object with the converted data.</returns>
    public ResponseData<TConvert> ToConvert<TConvert>() where TConvert : class, new()
    {
        return new ResponseData<TConvert>
        {
            Result = Result ,
            Code = Code ,
            Message = Message ,
            Data = 
                    Data == null ? null :
                    Data.FromCopyValue<TConvert>() 
        };
    }
}