using JohnIsDev.Core.Features.Extensions;

namespace JohnIsDev.Core.Test.Features.Extensions;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("+82227094548", "02-2709-4548", "0227094548")]
    [InlineData("+8221234567", "02-123-4567", "021234567")]
    [InlineData("+821033356168", "010-3335-6168", "01033356168")]
    [InlineData("010-3335-6168", "010-3335-6168", "01033356168")]
    [InlineData("[+821033356168]", "010-3335-6168", "01033356168")]
    [InlineData("+82314608027", "031-460-8027", "0314608027")]
    [InlineData("+827012345678", "070-1234-5678", "07012345678")]
    [InlineData("+821112345678", "011-1234-5678", "01112345678")]
    [InlineData("+8201033356168", "010-3335-6168", "01033356168")]
    [InlineData("00821033356168", "010-3335-6168", "01033356168")]
    [InlineData("821033356168", "010-3335-6168", "01033356168")]
    public void 국내_번호를_표시용과_발송용으로_변환한다(string input, string display, string recipient)
    {
        Assert.Equal(display, input.ToKoreanPhoneNumber());
        Assert.Equal(recipient, input.ToKoreanPhoneNumber(false));
        Assert.True(recipient.IsValidPhoneNumber());
    }

    [Theory]
    [InlineData("+81227094548")]
    [InlineData("+18227094548")]
    [InlineData("+8210123")]
    [InlineData("010123")]
    [InlineData("++821033356168")]
    [InlineData("")]
    public void 해외_번호와_잘못된_번호는_국내_수신번호로_인정하지_않는다(string input)
    {
        Assert.Equal(input, input.ToKoreanPhoneNumber(false));
        Assert.False(input.ToKoreanPhoneNumber(false).IsValidPhoneNumber());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void 번호가_없으면_빈_문자열을_반환한다(string? input)
    {
        Assert.Equal("", input!.ToKoreanPhoneNumber());
    }
}
