namespace JohnIsDev.Core.Models.Common;

/// <summary>
/// 서명 정보
/// </summary>
public class SigningInfo
{
    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="timeStamp"></param>
    /// <param name="nonce"></param>
    /// <param name="signature"></param>
    /// <param name="loginId"></param>
    public SigningInfo(long timeStamp, string nonce, string signature, string loginId)
    {
        TimeStamp = timeStamp;
        Nonce = nonce;
        Signature = signature;
        LoginId = loginId;
    }

    /// <summary>
    /// 서명 시점의 유닉스 타임스탬프
    /// </summary>
    public long TimeStamp { get; set; }

    /// <summary>
    /// 재전송 공격 및 중복 요청 방지용 일회성 고유값
    /// </summary>
    public string Nonce { get; set; }

    /// <summary>
    /// 데이터 무결성 및 진위 검증용 서명값
    /// </summary>
    public string Signature { get; set; }

    /// <summary>
    /// 로그인 아이디
    /// </summary>
    public string LoginId { get; set; }
}
