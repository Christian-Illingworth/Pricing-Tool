using PricingTool.Domain;

namespace PricingTool.Tests.Validation;

public class MpanTopLineTests
{
    [Fact]
    public void LegacyEightDigitsArePcMtcLlfc()
    {
        var top = MpanTopLine.Parse("03801631");
        Assert.Equal(3, top.ProfileClass);
        Assert.Equal(801, top.MeterTimeswitchClass);
        Assert.Equal("631", top.LineLossFactorClass);
        Assert.Null(top.StandardSettlementConfig);
        Assert.Null(top.DuosTariffId);
        Assert.False(top.MhhsMigrated);
    }

    [Fact]
    public void NineCharactersArePcSscDuosAndMeanMhhs()
    {
        // POWWR's example: PC 04, SSC 0151, DUoS tariff ID 661 (open item P-10).
        var top = MpanTopLine.Parse("040151661");
        Assert.Equal(4, top.ProfileClass);
        Assert.Equal("0151", top.StandardSettlementConfig);
        Assert.Equal("661", top.DuosTariffId);
        Assert.True(top.MhhsMigrated);
        Assert.Null(top.MeterTimeswitchClass);
        Assert.Null(top.LineLossFactorClass);
    }

    [Fact]
    public void HalfHourlyProfileClassIsZero()
    {
        Assert.Equal(0, MpanTopLine.Parse("00845661").ProfileClass);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0380163")]      // 7
    [InlineData("0380163100")]   // 10
    [InlineData("AB801631")]     // profile class not digits
    [InlineData("03A01631")]     // MTC not digits
    [InlineData("04A151661")]    // SSC not digits
    public void OtherShapesAreRejected(string input)
    {
        Assert.False(MpanTopLine.TryParse(input, out var top, out var error));
        Assert.Null(top);
        Assert.NotNull(error);
    }

    [Fact]
    public void ParseThrowsOnBadInput()
    {
        Assert.Throws<FormatException>(() => MpanTopLine.Parse("123"));
    }

    [Fact]
    public void LegacyTopLineAgreesWithMatchingFields()
    {
        var top = MpanTopLine.Parse("03801631");
        Assert.Empty(top.Disagreements(profileClass: 3, meterTimeswitchClass: 801, lineLossFactorClass: "631", standardSettlementConfig: "0393", duosTariffId: null));
    }

    [Fact]
    public void BlankFieldsDoNotDisagree()
    {
        var top = MpanTopLine.Parse("03801631");
        Assert.Empty(top.Disagreements(null, null, null, null, null));
    }

    [Fact]
    public void EachLegacyFieldThatDiffersIsNamed()
    {
        var top = MpanTopLine.Parse("03801631");
        Assert.Equal(["ProfileClass"], top.Disagreements(4, 801, "631", null, null));
        Assert.Equal(["MeterTimeswitchClass"], top.Disagreements(3, 845, "631", null, null));
        Assert.Equal(["LineLossFactorClass"], top.Disagreements(3, 801, "632", null, null));
        Assert.Equal(["ProfileClass", "MeterTimeswitchClass", "LineLossFactorClass"], top.Disagreements(4, 845, "632", null, null));
    }

    [Fact]
    public void MhhsTopLineChecksSscAndDuos()
    {
        var top = MpanTopLine.Parse("040151661");
        Assert.Empty(top.Disagreements(4, 845, "631", "0151", "661"));   // MTC and LLFC aren't in a 9-character line
        Assert.Equal(["StandardSettlementConfig"], top.Disagreements(4, null, null, "0393", null));
        Assert.Equal(["DuosTariffId"], top.Disagreements(4, null, null, null, "662"));
    }

    [Fact]
    public void LlfcComparisonIgnoresCase()
    {
        var top = MpanTopLine.Parse("03801A31");
        Assert.Empty(top.Disagreements(3, 801, "a31", null, null));
    }
}
