using PricingTool.Powwr.Pricing;
using PricingTool.Services.Pricing;

namespace PricingTool.Tests.Pricing;

// Research R12: options matched across sites by supplier, term and plan type; partial when a site is missing.
public class MultiSiteCombinerTests
{
    private static SiteOptionFigures Site(
        int siteKey, string supplier = "EDF", int term = 24, string? planType = null, decimal cost = 1_000m,
        decimal? saving = 100m, string? standingCharge = "62.00", bool outside = false, decimal commission = 120m) =>
        new(
            siteKey,
            new ReturnedRate
            {
                Supplier = supplier, TermMonths = term, PlanType = planType, StandingCharge = standingCharge,
                RawAnnualPrice = cost, OutsidePricingWindow = outside,
            },
            AnnualCostExVatCcl: cost,
            Ccl: cost * 0.03m,
            Vat: cost * 0.2m,
            AnnualCostIncVatCcl: cost * 1.23m,
            SavingExVatCcl: saving,
            SavingIncVatCcl: saving * 1.2m,
            Commission: new CommissionSplit(commission, 0m, commission / term));

    [Fact]
    public void SameSupplierTermAndPlanTypeAcrossSitesIsOneOption()
    {
        var option = Assert.Single(MultiSiteCombiner.Combine([Site(1, cost: 1_000m), Site(2, cost: 2_500.5m)], sitesTotal: 2));
        Assert.Equal("EDF", option.Supplier);
        Assert.Equal(24, option.TermMonths);
        Assert.Equal(2, option.SitesCovered);
        Assert.Equal(2, option.SitesTotal);
        Assert.False(option.IsPartial);
        Assert.Equal(3_500.5m, option.TotalAnnualCostExVatCcl);
        Assert.Equal(3_500.5m * 1.23m, option.TotalAnnualCostIncVatCcl);
        Assert.Equal(240m, option.Commission.ContractTotal);
    }

    [Fact]
    public void SupplierNameMatchIgnoresCase()
    {
        Assert.Single(MultiSiteCombiner.Combine([Site(1, supplier: "EDF"), Site(2, supplier: "edf")], sitesTotal: 2));
    }

    [Fact]
    public void TermAndPlanTypeSeparateOptions()
    {
        var options = MultiSiteCombiner.Combine(
        [
            Site(1, term: 12), Site(1, term: 24),
            Site(1, supplier: "Opus Energy", planType: "Fixed"), Site(1, supplier: "Opus Energy", planType: "Fixed No Standing Charge"),
        ], sitesTotal: 1);
        Assert.Equal(4, options.Count);
    }

    [Fact]
    public void OptionMissingASiteIsPartial()
    {
        var options = MultiSiteCombiner.Combine([Site(1), Site(2), Site(1, supplier: "Valda Energy")], sitesTotal: 2);
        var valda = options.Single(o => o.Supplier == "Valda Energy");
        Assert.True(valda.IsPartial);
        Assert.Equal(1, valda.SitesCovered);
        Assert.False(options.Single(o => o.Supplier == "EDF").IsPartial);
    }

    [Fact]
    public void TotalsStayAtFullPrecision()
    {
        var option = Assert.Single(MultiSiteCombiner.Combine([Site(1, cost: 0.333m), Site(2, cost: 0.333m), Site(3, cost: 0.333m)], sitesTotal: 3));
        Assert.Equal(0.999m, option.TotalAnnualCostExVatCcl);
    }

    [Fact]
    public void TotalSavingOnlyWhenEveryCoveredSiteHasOne()
    {
        Assert.Equal(250m, Assert.Single(MultiSiteCombiner.Combine([Site(1, saving: 100m), Site(2, saving: 150m)], 2)).TotalSavingExVatCcl);

        var oneMissing = Assert.Single(MultiSiteCombiner.Combine([Site(1, saving: 100m), Site(2, saving: null)], 2));
        Assert.Null(oneMissing.TotalSavingExVatCcl);
        Assert.Null(oneMissing.TotalSavingIncVatCcl);
    }

    [Fact]
    public void NoStandingChargeWhenEveryCoveredSitesChargeIsZero()
    {
        Assert.True(Assert.Single(MultiSiteCombiner.Combine([Site(1, standingCharge: "0.00"), Site(2, standingCharge: "0")], 2)).HasNoStandingCharge);
        Assert.False(Assert.Single(MultiSiteCombiner.Combine([Site(1, standingCharge: "0.00"), Site(2, standingCharge: "62.00")], 2)).HasNoStandingCharge);
        Assert.False(Assert.Single(MultiSiteCombiner.Combine([Site(1, standingCharge: null)], 1)).HasNoStandingCharge);
    }

    [Fact]
    public void OutsidePricingWindowOnAnySiteFlagsTheOption()
    {
        Assert.True(Assert.Single(MultiSiteCombiner.Combine([Site(1), Site(2, outside: true)], 2)).AnyOutsidePricingWindow);
    }

    [Fact]
    public void ASecondRateForTheSameSiteAndKeyIsNotCountedTwice()
    {
        var option = Assert.Single(MultiSiteCombiner.Combine([Site(1, cost: 1_000m), Site(1, cost: 9_999m)], 1));
        Assert.Equal(1, option.SitesCovered);
        Assert.Equal(1_000m, option.TotalAnnualCostExVatCcl);
    }
}
