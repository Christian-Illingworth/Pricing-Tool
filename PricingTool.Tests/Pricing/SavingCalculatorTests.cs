using PricingTool.Domain;
using PricingTool.Services.Pricing;

namespace PricingTool.Tests.Pricing;

// Research R11: a saving against current only when the current rates are complete.
public class SavingCalculatorTests
{
    private static readonly Dictionary<Register, int> DayNight = new() { [Register.Day] = 63_000, [Register.Night] = 27_000 };

    [Fact]
    public void CurrentCostIsRatesTimesKwhPlusStandingCharge()
    {
        var current = SavingCalculator.CurrentAnnualCost(DayNight, new CurrentRates(30.1m, 19.2m, null, 75m));
        Assert.Equal((30.1m * 63_000 + 19.2m * 27_000) / 100m + 75m * 365m / 100m, current);
    }

    [Fact]
    public void NoSavingWithoutTheStandingCharge()
    {
        Assert.Null(SavingCalculator.CurrentAnnualCost(DayNight, new CurrentRates(30.1m, 19.2m, null, null)));
    }

    [Fact]
    public void NoSavingWithoutARateForEveryRegister()
    {
        Assert.Null(SavingCalculator.CurrentAnnualCost(DayNight, new CurrentRates(30.1m, null, null, 75m)));
    }

    [Fact]
    public void RatesForRegistersTheSiteDoesNotUseAreIgnored()
    {
        var single = new Dictionary<Register, int> { [Register.Day] = 18_000 };
        Assert.Equal(25m * 180m + 50m * 3.65m, SavingCalculator.CurrentAnnualCost(single, new CurrentRates(25m, null, null, 50m)));
    }

    [Fact]
    public void SavingIsCurrentMinusNew()
    {
        Assert.Equal(2_528.39m, SavingCalculator.Saving(24_420.75m, 21_892.36m));
        Assert.Equal(SavingDirection.Saves, SavingCalculator.DirectionOf(2_528.39m));
        Assert.Equal(SavingDirection.CostsMore, SavingCalculator.DirectionOf(-12.5m));
        Assert.Equal(SavingDirection.Same, SavingCalculator.DirectionOf(0m));
    }

    [Fact]
    public void WithVatAndCclBothSidesGetTheSameTreatment()
    {
        var rates = new SiteRates(new DateOnly(2026, 12, 1), 20m, 5m, 0.801m, 33m, null);
        var currentEx = 24_420.75m;
        var newEx = 21_892.36m;

        var currentInc = VatCclCalculator.Apply(currentEx, 90_000, SiteTreatment.Standard, 0m, rates).Total;
        var newInc = VatCclCalculator.Apply(newEx, 90_000, SiteTreatment.Standard, 0m, rates).Total;

        // CCL is the same on both sides, so only VAT on the cost difference remains.
        Assert.Equal((currentEx - newEx) * 1.2m, SavingCalculator.Saving(currentInc, newInc));
    }
}
