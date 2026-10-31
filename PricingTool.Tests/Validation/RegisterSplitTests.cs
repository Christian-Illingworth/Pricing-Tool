using PricingTool.Domain;
using PricingTool.Services.Validation;

namespace PricingTool.Tests.Validation;

public class RegisterSplitTests
{
    [Fact]
    public void SplitMustTotalExactlyOneHundred()
    {
        Assert.True(RegisterSplit.TotalsOneHundred([60m, 40m]));
        Assert.True(RegisterSplit.TotalsOneHundred([33.3333m, 33.3333m, 33.3334m]));
        Assert.False(RegisterSplit.TotalsOneHundred([60m, 39.99m]));
        Assert.False(RegisterSplit.TotalsOneHundred([60m, 40.01m]));
        Assert.False(RegisterSplit.TotalsOneHundred([]));
    }

    [Fact]
    public void WholeNumberSplitDividesExactly()
    {
        var kwh = RegisterSplit.Split(90_000, new Dictionary<Register, decimal> { [Register.Day] = 70m, [Register.Night] = 30m });
        Assert.Equal(63_000, kwh[Register.Day]);
        Assert.Equal(27_000, kwh[Register.Night]);
    }

    [Fact]
    public void LargestRemainderMakesThePartsAddUp()
    {
        // 33.3333 / 33.3333 / 33.3334 of 100: floors 33+33+33 = 99, and the spare kWh goes to the
        // largest remainder (Wend's .3334).
        var kwh = RegisterSplit.Split(100, new Dictionary<Register, decimal>
        {
            [Register.Day] = 33.3333m, [Register.Night] = 33.3333m, [Register.Wend] = 33.3334m,
        });
        Assert.Equal(33, kwh[Register.Day]);
        Assert.Equal(33, kwh[Register.Night]);
        Assert.Equal(34, kwh[Register.Wend]);
        Assert.Equal(100, kwh.Values.Sum());
    }

    [Fact]
    public void TiesGoToTheEarlierRegister()
    {
        var kwh = RegisterSplit.Split(1, new Dictionary<Register, decimal> { [Register.Night] = 50m, [Register.Day] = 50m });
        Assert.Equal(1, kwh[Register.Day]);
        Assert.Equal(0, kwh[Register.Night]);
    }

    [Theory]
    [InlineData(12_345)]
    [InlineData(99_999)]
    [InlineData(7)]
    public void PartsAlwaysAddUpToTheTotal(int total)
    {
        var kwh = RegisterSplit.Split(total, new Dictionary<Register, decimal>
        {
            [Register.Day] = 41.7m, [Register.Night] = 29.15m, [Register.Wend] = 29.15m,
        });
        Assert.Equal(total, kwh.Values.Sum());
    }

    [Fact]
    public void SplitRefusesPercentagesThatDoNotTotalOneHundred()
    {
        Assert.Throws<ArgumentException>(() =>
            RegisterSplit.Split(100, new Dictionary<Register, decimal> { [Register.Day] = 60m, [Register.Night] = 30m }));
    }
}
