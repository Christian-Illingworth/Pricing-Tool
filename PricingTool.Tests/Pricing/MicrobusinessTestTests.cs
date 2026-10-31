using PricingTool.Domain;
using PricingTool.Services.Pricing;

namespace PricingTool.Tests.Pricing;

// FR-027, FR-028, research R10.
public class MicrobusinessTestTests
{
    [Theory]
    [InlineData(90_000, true)]     // quickstart ZZ1 1AB
    [InlineData(100_000, true)]    // exactly at the threshold counts
    [InlineData(100_001, false)]
    public void ElectricityAtOrBelowTheThresholdIsFlagged(int kwh, bool expected)
    {
        Assert.Equal(expected, MicrobusinessTest.Evaluate([kwh], 100_000m, false, false).IsMicrobusiness);
    }

    [Theory]
    [InlineData(293_000, true)]
    [InlineData(293_001, false)]
    public void GasHasItsOwnThreshold(int kwh, bool expected)
    {
        Assert.Equal(expected, MicrobusinessTest.Evaluate([kwh], 293_000m, false, false).IsMicrobusiness);
    }

    [Fact]
    public void ConsumptionIsCombinedAcrossTheQuotesSites()
    {
        // US6-5: several small sites adding up to more than the threshold aren't a microbusiness.
        var over = MicrobusinessTest.Evaluate([40_000, 40_000, 30_000], 100_000m, false, false);
        Assert.False(over.IsMicrobusiness);
        Assert.Equal(110_000, over.CombinedAnnualKwh);

        Assert.True(MicrobusinessTest.Evaluate([40_000, 40_000, 20_000], 100_000m, false, false).IsMicrobusiness);
    }

    [Fact]
    public void DeclarationNeedsBothAnswersYes()
    {
        Assert.False(MicrobusinessTest.Evaluate([500_000], 100_000m, true, false).IsMicrobusiness);
        Assert.False(MicrobusinessTest.Evaluate([500_000], 100_000m, false, true).IsMicrobusiness);

        var declared = MicrobusinessTest.Evaluate([500_000], 100_000m, true, true);
        Assert.True(declared.IsMicrobusiness);
        Assert.Equal(MicrobusinessSource.Declaration, declared.Source);
    }

    [Fact]
    public void SourceSaysWhy()
    {
        Assert.Equal(MicrobusinessSource.Consumption, MicrobusinessTest.Evaluate([50_000], 100_000m, false, false).Source);
        Assert.Equal(MicrobusinessSource.Both, MicrobusinessTest.Evaluate([50_000], 100_000m, true, true).Source);
        Assert.Equal(MicrobusinessSource.None, MicrobusinessTest.Evaluate([500_000], 100_000m, true, false).Source);
    }
}
