using PricingTool.Services.Pricing;

namespace PricingTool.Tests.Pricing;

// Research R7: one rounding rule, applied only when a figure is displayed; totals are rounded from
// the full-precision sum.
public class RoundingTests
{
    [Theory]
    [InlineData("2.345", "2.35")]      // midpoint goes away from zero
    [InlineData("-2.345", "-2.35")]
    [InlineData("2.3449999", "2.34")]
    [InlineData("114.0208333333", "114.02")]
    [InlineData("4522.652", "4522.65")]
    [InlineData("0.005", "0.01")]
    [InlineData("0", "0")]
    public void ToPenniesRoundsHalfAwayFromZero(string value, string expected)
    {
        Assert.Equal(decimal.Parse(expected), Rounding.ToPennies(decimal.Parse(value)));
    }

    [Fact]
    public void TotalsAreRoundedFromTheFullPrecisionSum()
    {
        decimal[] parts = [0.333m, 0.333m, 0.333m];
        Assert.Equal(1.00m, Rounding.ToPennies(parts.Sum()));
        Assert.Equal(0.99m, parts.Sum(Rounding.ToPennies));   // why per-part rounding isn't used for totals
    }

    [Theory]
    [InlineData("1234.565", "£1,234.57")]
    [InlineData("0", "£0.00")]
    [InlineData("0.5", "£0.50")]
    [InlineData("1234567.891", "£1,234,567.89")]
    [InlineData("-2.5", "-£2.50")]
    [InlineData("27135.912", "£27,135.91")]
    public void MoneyIsFormattedTheEnglishWay(string value, string expected)
    {
        Assert.Equal(expected, MoneyFormat.Pounds(decimal.Parse(value)));
    }

    [Fact]
    public void FormattingUsesTheSameRoundingRule()
    {
        Assert.Equal("£2.35", MoneyFormat.Pounds(2.345m));
        Assert.Equal(MoneyFormat.Pounds(Rounding.ToPennies(9.995m)), MoneyFormat.Pounds(9.995m));
    }
}
