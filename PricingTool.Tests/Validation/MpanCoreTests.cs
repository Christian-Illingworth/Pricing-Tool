using PricingTool.Domain;

namespace PricingTool.Tests.Validation;

public class MpanCoreTests
{
    [Theory]
    [InlineData("1610028163273")]   // POWWR's own example
    [InlineData("1620000546231")]   // POWWR's own example
    [InlineData("1600000001013")]   // mock: standard site
    [InlineData("1200000000011")]   // mock: half-hourly
    [InlineData("2300000000019")]   // mock: every supplier declines
    [InlineData("1600000001050")]   // Σ mod 11 = 10, so the check digit is 0
    public void ValidCoresPass(string core)
    {
        Assert.True(MpanCore.TryParse(core, out var parsed, out var error), error);
        Assert.Equal(core, parsed!.Value);
    }

    [Fact]
    public void WrongCheckDigitFails()
    {
        Assert.False(MpanCore.TryParse("1600000001014", out var parsed, out var error));
        Assert.Null(parsed);
        Assert.Contains("check digit", error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("160000000101")]     // 12 digits
    [InlineData("16000000010130")]   // 14 digits
    [InlineData("16000000010A3")]    // a letter
    public void CoreMustBeExactly13Digits(string core)
    {
        Assert.False(MpanCore.TryParse(core, out _, out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void SpacesAreIgnoredWhenPasted()
    {
        Assert.True(MpanCore.TryParse(" 16 0000 0001 013 ", out var parsed, out _));
        Assert.Equal("1600000001013", parsed!.Value);
    }

    [Fact]
    public void CheckDigitIsWeightedSumMod11Mod10()
    {
        // 1,6,1,0,0,2,8,1,6,3,2,7 × primes 3,5,7,13,17,19,23,29,31,37,41,43 = 971; 971 mod 11 = 3.
        Assert.True(MpanCore.HasValidCheckDigit("1610028163273"));
        Assert.False(MpanCore.HasValidCheckDigit("1610028163274"));
    }

    [Theory]
    [InlineData("10", "Eastern")]
    [InlineData("11", "East Midlands")]
    [InlineData("12", "London")]
    [InlineData("13", "Merseyside and North Wales")]
    [InlineData("14", "Midlands")]
    [InlineData("15", "Northern")]
    [InlineData("16", "North Western")]
    [InlineData("17", "North Scotland")]
    [InlineData("18", "South Scotland")]
    [InlineData("19", "South Eastern")]
    [InlineData("20", "Southern")]
    [InlineData("21", "South Wales")]
    [InlineData("22", "South Western")]
    [InlineData("23", "Yorkshire")]
    [InlineData("24", "Independent network (IDNO)")]
    [InlineData("99", "Independent network (IDNO)")]
    public void DistributorIdGivesTheRegion(string distributorId, string region)
    {
        Assert.Equal(region, MpanCore.RegionFor(distributorId));
    }

    [Theory]
    [InlineData("00")]
    [InlineData("05")]
    [InlineData("09")]
    public void DistributorIdsBelow10AreNotRegions(string distributorId)
    {
        Assert.Null(MpanCore.RegionFor(distributorId));
    }

    [Fact]
    public void ParsedCoreCarriesDistributorAndRegion()
    {
        Assert.True(MpanCore.TryParse("2400000000004", out var idno, out _));
        Assert.Equal("24", idno!.DistributorId);
        Assert.Equal("Independent network (IDNO)", idno.RegionName);

        Assert.True(MpanCore.TryParse("1600000001013", out var nw, out _));
        Assert.Equal("16", nw!.DistributorId);
        Assert.Equal("North Western", nw.RegionName);
    }

    [Fact]
    public void CoreWithValidCheckDigitButNoRegionFails()
    {
        // 0,5 × 3,5 = 25; 25 mod 11 = 3, so the check digit is right but 05 isn't a region.
        Assert.True(MpanCore.HasValidCheckDigit("0500000000003"));
        Assert.False(MpanCore.TryParse("0500000000003", out _, out var error));
        Assert.Contains("region", error);
    }
}
