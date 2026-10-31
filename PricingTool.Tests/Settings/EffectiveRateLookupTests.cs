using PricingTool.Data.Entities;
using PricingTool.Domain;
using PricingTool.Services.Settings;

namespace PricingTool.Tests.Settings;

public class EffectiveRateLookupTests
{
    private static readonly EffectiveRate[] Rows =
    [
        new() { Kind = EffectiveRateKind.CclElectricity, Value = 0.775m, EffectiveFrom = new DateOnly(2024, 4, 1) },
        new() { Kind = EffectiveRateKind.CclElectricity, Value = 0.801m, EffectiveFrom = new DateOnly(2025, 4, 1) },
        new() { Kind = EffectiveRateKind.CclElectricity, Value = 0.826m, EffectiveFrom = new DateOnly(2027, 4, 1) },
        new() { Kind = EffectiveRateKind.CclGas, Value = 0.700m, EffectiveFrom = new DateOnly(2020, 1, 1) },
    ];

    [Fact]
    public void LatestRowOnOrBeforeTheDateWins()
    {
        Assert.Equal(0.801m, EffectiveRateLookup.Find(Rows, EffectiveRateKind.CclElectricity, new DateOnly(2026, 12, 1)));
        Assert.Equal(0.775m, EffectiveRateLookup.Find(Rows, EffectiveRateKind.CclElectricity, new DateOnly(2025, 3, 31)));
    }

    [Fact]
    public void TheEffectiveDateItselfCounts()
    {
        Assert.Equal(0.801m, EffectiveRateLookup.Find(Rows, EffectiveRateKind.CclElectricity, new DateOnly(2025, 4, 1)));
        Assert.Equal(0.826m, EffectiveRateLookup.Find(Rows, EffectiveRateKind.CclElectricity, new DateOnly(2027, 4, 1)));
    }

    [Fact]
    public void FutureRowsApplyOnlyFromTheirDate()
    {
        // US8-3: a quote starting on or after the new rate's date uses it; an earlier start doesn't.
        Assert.Equal(0.801m, EffectiveRateLookup.Find(Rows, EffectiveRateKind.CclElectricity, new DateOnly(2027, 3, 31)));
    }

    [Fact]
    public void OtherKindsAreIgnored()
    {
        Assert.Equal(0.700m, EffectiveRateLookup.Find(Rows, EffectiveRateKind.CclGas, new DateOnly(2026, 12, 1)));
    }

    [Fact]
    public void DateBeforeAnyRowIsAnError()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            EffectiveRateLookup.Find(Rows, EffectiveRateKind.CclElectricity, new DateOnly(2024, 3, 31)));
        Assert.Contains("CclElectricity", error.Message);
        Assert.Null(EffectiveRateLookup.TryFind(Rows, EffectiveRateKind.CclElectricity, new DateOnly(2024, 3, 31)));
    }

    [Fact]
    public void KindWithNoRowsIsAnErrorButOptionalLookupsGiveNull()
    {
        Assert.Throws<InvalidOperationException>(() =>
            EffectiveRateLookup.Find(Rows, EffectiveRateKind.QualifyingWholeSupplyPercent, new DateOnly(2026, 12, 1)));
        Assert.Null(EffectiveRateLookup.TryFind(Rows, EffectiveRateKind.QualifyingWholeSupplyPercent, new DateOnly(2026, 12, 1)));
    }
}
