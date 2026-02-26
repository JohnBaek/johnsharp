namespace JohnIsDev.Core.Models.Common.Enums;

/// <summary>
/// 응답결과 
/// </summary>
public enum EnumResponseResult
{
    /// <summary>
    /// 예외 또는 에러 
    /// </summary>
    Error = -99 ,
    
    /// <summary>
    /// 경고 
    /// </summary>
    Warning = -1,
    
    /// <summary>
    /// 성공
    /// </summary>
    Success = 0,
}