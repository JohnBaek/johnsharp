using FluentAssertions;
using JohnIsDev.Core.Features.Utils;
using JohnIsDev.Core.Models.Common;

namespace JohnIsDev.Core.Test.Features.Utils;

/// <summary>
/// HMAC 서명의 생성과 검증 동작을 확인한다.
/// </summary>
public class SignUtilTest
{
    // private const string ClientSecret = "test-client-secret";
    // private readonly SignUtil _signUtil = new();
    //
    // /// <summary>
    // /// 생성한 서명을 같은 시크릿으로 검증하고 요청마다 다른 nonce를 사용하는지 확인한다.
    // /// </summary>
    // [Fact]
    // public void GenerateSign_CreatesValidSignaturesWithDistinctNonces()
    // {
    //     SigningInfo first = _signUtil.GenerateSign(ClientSecret, TODO);
    //     SigningInfo second = _signUtil.GenerateSign(ClientSecret, TODO);
    //
    //     first.Nonce.Should().NotBeNullOrWhiteSpace().And.NotBe(second.Nonce);
    //     _signUtil.ValidateSigning(first, TODO, ClientSecret).Should().BeTrue();
    //     _signUtil.ValidateSigning(second, TODO, ClientSecret).Should().BeTrue();
    // }
    //
    // /// <summary>
    // /// 독립적으로 계산한 HMAC-SHA256 서명을 검증한다.
    // /// </summary>
    // [Fact]
    // public void ValidateSigning_AcceptsKnownSignature()
    // {
    //     // OpenSSL로 계산한 고정 서명으로 서명 원문의 형식을 검증한다.
    //     var signingInfo = new SigningInfo(
    //         1700000000123,
    //         "AAECAwQFBgcICQoLDA0ODw==",
    //         "oNK42CFCWhyRAZlwsd42jX/LiVKJTwcVe5/Uv2+iE0I=", TODO);
    //
    //     _signUtil.ValidateSigning(signingInfo, TODO, ClientSecret).Should().BeTrue();
    // }
    //
    // /// <summary>
    // /// 잘못된 시크릿으로 서명을 검증하면 실패하는지 확인한다.
    // /// </summary>
    // /// <param name="clientSecret">검증에 사용할 시크릿이다.</param>
    // [Theory]
    // [InlineData("wrong-secret")]
    // [InlineData("")]
    // [InlineData(" ")]
    // public void ValidateSigning_RejectsInvalidSecret(string clientSecret)
    // {
    //     SigningInfo signingInfo = _signUtil.GenerateSign(ClientSecret, TODO);
    //
    //     _signUtil.ValidateSigning(signingInfo, TODO, clientSecret).Should().BeFalse();
    // }
    //
    // /// <summary>
    // /// 서명에 사용한 타임스탬프나 nonce가 바뀌면 검증이 실패하는지 확인한다.
    // /// </summary>
    // [Fact]
    // public void ValidateSigning_RejectsChangedTimestampOrNonce()
    // {
    //     SigningInfo original = _signUtil.GenerateSign(ClientSecret, TODO);
    //     var changedTimestamp = new SigningInfo(original.TimeStamp + 1, original.Nonce, original.Signature, TODO);
    //     var changedNonce = new SigningInfo(original.TimeStamp, original.Nonce + "A", original.Signature, TODO);
    //
    //     _signUtil.ValidateSigning(changedTimestamp, TODO, ClientSecret).Should().BeFalse();
    //     _signUtil.ValidateSigning(changedNonce, TODO, ClientSecret).Should().BeFalse();
    // }
    //
    // /// <summary>
    // /// 비어 있거나 유효하지 않은 서명을 거부하는지 확인한다.
    // /// </summary>
    // /// <param name="signature">검증할 서명 문자열이다.</param>
    // [Theory]
    // [InlineData("")]
    // [InlineData("AAAA")]
    // [InlineData("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!")]
    // public void ValidateSigning_RejectsMalformedSignature(string signature)
    // {
    //     SigningInfo signingInfo = _signUtil.GenerateSign(ClientSecret, TODO);
    //     signingInfo.Signature = signature;
    //
    //     _signUtil.ValidateSigning(signingInfo, TODO, ClientSecret).Should().BeFalse();
    // }
}
