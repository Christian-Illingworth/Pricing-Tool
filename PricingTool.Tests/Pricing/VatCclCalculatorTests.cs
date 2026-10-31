using PricingTool.Domain;
using PricingTool.Services.Pricing;

namespace PricingTool.Tests.Pricing;

// FR-029 to FR-031, research R8. Money in pounds, CCL rates in pence per kWh, VAT in percent.
public class VatCclCalculatorTests
{
    private static SiteRates Rates(decimal ccl = 0.801m, decimal lowUse = 33m, decimal? wholeSupply = null, DateOnly? start = null) =>
        new(start ?? new DateOnly(2026, 12, 1), 20m, 5m, ccl, lowUse, wholeSupply);

    // ── Standard ──

    [Fact]
    public void StandardChargesCclAndVatOnCostPlusCcl()
    {
        // 90,000 kWh at 0.801 p/kWh = £720.90 CCL; VAT 20% of (21,892.36 + 720.90).
        var result = VatCclCalculator.Apply(21_892.36m, 90_000, SiteTreatment.Standard, 0m, Rates());
        Assert.Equal(720.90m, result.Ccl);
        Assert.Equal((21_892.36m + 720.90m) * 0.20m, result.Vat);
        Assert.Equal(21_892.36m + 720.90m + result.Vat, result.Total);
    }

    // ── Low use ──

    [Fact]
    public void LowUseIsAtOrBelowTheDailyThreshold()
    {
        Assert.True(LowUseTest.IsLowUse(10_950, 33m));      // 30 kWh a day (quickstart ZZ1 1AC)
        Assert.True(LowUseTest.IsLowUse(33 * 365, 33m));    // exactly the threshold counts
        Assert.False(LowUseTest.IsLowUse(33 * 365 + 1, 33m));
        Assert.True(LowUseTest.IsLowUse(145 * 365, 145m));  // gas
        Assert.False(LowUseTest.IsLowUse(145 * 365 + 1, 145m));
    }

    [Fact]
    public void LowUseGivesFivePercentVatAndNoCcl()
    {
        var result = VatCclCalculator.Apply(3_000m, 10_950, SiteTreatment.LowUse, 0m, Rates());
        Assert.Equal(0m, result.Ccl);
        Assert.Equal(150m, result.Vat);
        Assert.Equal(3_150m, result.Total);
    }

    [Fact]
    public void LowUseIsDecidedByConsumptionNotTheAgent()
    {
        Assert.Equal((SiteTreatment.LowUse, 0m), VatCclCalculator.DecideTreatment(VatCclTreatment.Standard, null, 10_950, Rates()));
        Assert.Equal((SiteTreatment.LowUse, 0m), VatCclCalculator.DecideTreatment(VatCclTreatment.Qualifying, 60m, 10_950, Rates()));
        Assert.Equal((SiteTreatment.Standard, 0m), VatCclCalculator.DecideTreatment(VatCclTreatment.Standard, null, 90_000, Rates()));
    }

    // ── Qualifying use ──

    [Fact]
    public void QualifyingSplitsCclAndVatByThePercentage()
    {
        // q = 60%: CCL on the 40% non-qualifying kWh; VAT 5% on the qualifying cost, 20% on the rest plus CCL.
        const decimal cost = 10_000m;
        const int kwh = 50_000;
        var result = VatCclCalculator.Apply(cost, kwh, SiteTreatment.Qualifying, 0.6m, Rates());

        var expectedCcl = kwh * 0.4m * 0.801m / 100m;
        Assert.Equal(expectedCcl, result.Ccl);
        Assert.Equal(cost * 0.6m * 0.05m + (cost * 0.4m + expectedCcl) * 0.20m, result.Vat);
    }

    [Fact]
    public void QualifyingFractionComesFromTheDeclaredPercentage()
    {
        Assert.Equal((SiteTreatment.Qualifying, 0.6m), VatCclCalculator.DecideTreatment(VatCclTreatment.Qualifying, 60m, 50_000, Rates()));
    }

    [Fact]
    public void WholeSupplyRuleMakesItOneHundredPercentWhenMet()
    {
        Assert.Equal((SiteTreatment.Qualifying, 1m), VatCclCalculator.DecideTreatment(VatCclTreatment.Qualifying, 60m, 50_000, Rates(wholeSupply: 60m)));
        Assert.Equal((SiteTreatment.Qualifying, 0.6m), VatCclCalculator.DecideTreatment(VatCclTreatment.Qualifying, 60m, 50_000, Rates(wholeSupply: 60.01m)));

        var allQualifying = VatCclCalculator.Apply(10_000m, 50_000, SiteTreatment.Qualifying, 1m, Rates());
        Assert.Equal(0m, allQualifying.Ccl);
        Assert.Equal(500m, allQualifying.Vat);
    }

    // ── Rates by start date ──

    [Fact]
    public void EachSitesRatesComeFromItsOwnStartDate()
    {
        var before = VatCclCalculator.Apply(1_000m, 10_000, SiteTreatment.Standard, 0m, Rates(ccl: 0.801m, start: new DateOnly(2027, 3, 1)));
        var after = VatCclCalculator.Apply(1_000m, 10_000, SiteTreatment.Standard, 0m, Rates(ccl: 0.826m, start: new DateOnly(2027, 4, 1)));
        Assert.Equal(80.10m, before.Ccl);
        Assert.Equal(82.60m, after.Ccl);
    }

    [Fact]
    public void NothingIsRounded()
    {
        var result = VatCclCalculator.Apply(1_234.5678901m, 12_345, SiteTreatment.Standard, 0m, Rates());
        Assert.Equal(12_345 * 0.801m / 100m, result.Ccl);   // 98.88345
        Assert.Equal((1_234.5678901m + 98.88345m) * 0.2m, result.Vat);
    }
}
