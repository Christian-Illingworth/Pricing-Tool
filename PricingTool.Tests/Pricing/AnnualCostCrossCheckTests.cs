using PricingTool.Domain;
using PricingTool.Powwr.Pricing;
using PricingTool.Services.Pricing;

namespace PricingTool.Tests.Pricing;

// Research R7: the cross-check recomputes the annual cost, logs a big difference, and never changes
// the figures shown.
public class AnnualCostCrossCheckTests
{
    private static readonly Dictionary<Register, int> DayNight = new() { [Register.Day] = 63_000, [Register.Night] = 27_000 };

    // 26.912 × 63,000 / 100 + 17.450 × 27,000 / 100 + 62.00 × 365 / 100 = 21,892.36
    private const decimal Exact = 21_892.36m;

    private static ReturnedRate Rate(decimal raw, string? capacity = null) => new()
    {
        Supplier = "EDF", TermMonths = 24, DayUnitrate = "26.912", NightUnitrate = "17.450", StandingCharge = "62.00",
        CapacityCharge = capacity, RawAnnualPrice = raw,
    };

    [Fact]
    public void MatchingFiguresHaveNoDifference()
    {
        var check = AnnualCostCrossCheck.Check(Rate(Exact), DayNight, kva: null, kvarh: null);
        Assert.Equal(Exact, check.Recomputed);
        Assert.Equal(0m, check.Difference);
        Assert.False(check.IsSignificant);
    }

    [Fact]
    public void SmallDifferenceIsNotFlagged()
    {
        Assert.False(AnnualCostCrossCheck.Check(Rate(Exact + 0.50m), DayNight, null, null).IsSignificant);
    }

    [Fact]
    public void DifferenceOverOnePoundIsFlagged()
    {
        var check = AnnualCostCrossCheck.Check(Rate(Exact + 1.50m), DayNight, null, null);
        Assert.Equal(1.50m, check.Difference);
        Assert.True(check.IsSignificant);
    }

    [Fact]
    public void DifferenceOverOnePercentIsFlaggedEvenUnderAPound()
    {
        var small = new Dictionary<Register, int> { [Register.Day] = 100 };
        var rate = new ReturnedRate { Supplier = "EDF", TermMonths = 12, DayUnitrate = "20.000", StandingCharge = "10.00", RawAnnualPrice = 57.40m };
        // Recomputed: 20.00 + 36.50 = 56.50; the 0.90 difference is 1.6%.
        var check = AnnualCostCrossCheck.Check(rate, small, null, null);
        Assert.Equal(56.50m, check.Recomputed);
        Assert.True(check.IsSignificant);
    }

    [Fact]
    public void CapacityChargeIsIncludedForHalfHourlyMeters()
    {
        var withCapacity = Exact + 100 * 2.50m * 365m / 100m;
        var check = AnnualCostCrossCheck.Check(Rate(withCapacity, capacity: "2.50"), DayNight, kva: 100, kvarh: null);
        Assert.Equal(0m, check.Difference);
    }
}
