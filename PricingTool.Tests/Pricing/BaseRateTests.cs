using PricingTool.Services.Pricing;

namespace PricingTool.Tests.Pricing;

// Research R7: base rates (agent-only) are the returned rate minus POWWR's reported uplift, shown with
// the returned string's decimal places.
public class BaseRateTests
{
    [Theory]
    [InlineData("26.912", "1.5", "25.412")]
    [InlineData("17.450", "1.5", "15.950")]
    [InlineData("62.00", "5", "57.00")]
    [InlineData("26.9", "1.2345", "25.7")]
    [InlineData("25.000", null, "25.000")]
    public void BaseRateIsReturnedMinusUpliftAtTheReturnedPrecision(string returned, string? uplift, string expected)
    {
        var value = BaseRates.Compute(returned, uplift);
        Assert.NotNull(value);
        Assert.Equal(expected, BaseRates.Format(value.Value, returned));
    }

    [Fact]
    public void ValueIsRoundedToTheReturnedDecimalPlaces()
    {
        Assert.Equal(25.7m, BaseRates.Compute("26.9", "1.2345"));
    }

    [Theory]
    [InlineData(null, "1.5")]
    [InlineData("", "1.5")]
    [InlineData("n/a", "1.5")]
    public void NoReturnedRateMeansNoBaseRate(string? returned, string? uplift)
    {
        Assert.Null(BaseRates.Compute(returned, uplift));
    }

    [Fact]
    public void DecimalPlacesComeFromTheString()
    {
        Assert.Equal(3, BaseRates.DecimalPlaces("26.912"));
        Assert.Equal(2, BaseRates.DecimalPlaces("62.00"));
        Assert.Equal(0, BaseRates.DecimalPlaces("62"));
    }
}
