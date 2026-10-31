using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PricingTool.Domain;
using PricingTool.Options;
using PricingTool.Powwr.Pricing;
using PricingTool.Services.Pricing;
using PricingTool.Tests.Support;

namespace PricingTool.Tests.Pricing;

// The whole engine on the mock adapter's output: the stored figures are exactly what the calculators
// give, and nothing is rounded.
public class PricingEngineTests
{
    private static readonly PricingEngine Engine = new(NullLogger<PricingEngine>.Instance);
    private static readonly DateOnly Start = new(2026, 12, 1);

    private sealed class Monitor : IOptionsMonitor<PowwrMockOptions>
    {
        public PowwrMockOptions CurrentValue { get; } = new();
        public PowwrMockOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<PowwrMockOptions, string?> listener) => null;
    }

    private static readonly MockPowwrPricingClient Mock = new(
        FakeTimeProvider.At("2026-10-31T10:00:00Z"), Microsoft.Extensions.Options.Options.Create(new PowwrPricingOptions()), new Monitor());

    private static PricingSettingsSnapshot Settings(CommissionBasis basis = CommissionBasis.PerYear, decimal upfront = 0m) => new(
        Fuel.Electricity, basis,
        [new SiteRates(Start, 20m, 5m, 0.801m, 33m, null)],
        MicrobusinessThresholdKwh: 100_000m,
        FuelMaxUnitRateUplift: 3m, FuelMaxStandingChargeUplift: 25m,
        MockPowwrPricingClient.Suppliers.Select((s, i) => new SupplierSettings(i + 1, s, s == "EDF" ? upfront : 0m, 3m, 25m)).ToList());

    private static async Task<SitePriceResponse> Price(string mpan, int dayKwh, decimal unitUplift = 1.5m, string[]? suppliers = null) =>
        await Mock.GetElectricPricesAsync(new ElectricPriceRequest
        {
            QuoteReference = "CUQ-26-000042", AgentName = "Alex Agent", Postcode = "ZZ1 1AA", QuoteType = QuoteType.Renewal,
            PaymentMethod = "Monthly Direct Debit", BusinessType = BusinessType.LimitedCompany, StartDate = Start,
            TermsMonths = [12, 24], DefaultUplift = new UpliftPair(unitUplift, 5m),
            SupplierPlans = (suppliers ?? MockPowwrPricingClient.Suppliers.ToArray())
                .Select(s => new SupplierPlan(s, [new PlanUplift(12, unitUplift, 5m), new PlanUplift(24, unitUplift, 5m)])).ToList(),
            MpanCore = mpan, DayKwh = dayKwh,
        }, default);

    private static EngineSite Site(int key, int dayKwh, SitePriceResponse? response, CurrentRates? current = null) =>
        new(key, key, Start, new Dictionary<Register, int> { [Register.Day] = dayKwh }, null, null, current ?? CurrentRates.None, response);

    private static IReadOnlyList<EngineUplift> Uplifts(decimal requested = 1.5m, decimal applied = 1.5m, string? onlyFor = null) =>
        MockPowwrPricingClient.Suppliers.SelectMany(s => new[] { 12, 24 }.Select(t =>
            onlyFor is null || s == onlyFor
                ? new EngineUplift(s, t, requested, 5m, applied, 5m)
                : new EngineUplift(s, t, applied, 5m, applied, 5m))).ToList();

    private static PricingEngineInput Input(IReadOnlyList<EngineSite> sites, PricingSettingsSnapshot? settings = null,
        IReadOnlyList<EngineUplift>? uplifts = null, VatCclTreatment treatment = VatCclTreatment.Standard) =>
        new(Fuel.Electricity, treatment, null, false, false, sites, uplifts ?? Uplifts(), settings ?? Settings());

    [Fact]
    public async Task AnnualCostIsPowwrsRawAnnualPrice()
    {
        var response = await Price("1600000001013", 18_000);
        var result = Engine.Calculate(Input([Site(1, 18_000, response)]));

        foreach (var option in result.Options)
        {
            var site = Assert.Single(option.Sites);
            Assert.Equal(site.Rate.RawAnnualPrice, site.AnnualCostExVatCcl);
            Assert.Equal(site.AnnualCostExVatCcl, option.TotalAnnualCostExVatCcl);
        }
    }

    [Fact]
    public async Task EveryReturnedRateBecomesAnOptionAndNoQuotesAreLeftOut()
    {
        var response = await Price("1600000001013", 18_000);
        var result = Engine.Calculate(Input([Site(1, 18_000, response)]));

        Assert.Equal(response.Rates.Count, result.Options.Count);
        Assert.DoesNotContain(result.Options, o => o.Supplier == "Valda Energy");
        Assert.All(result.Options, o => Assert.Null(o.IsGreen));
    }

    [Fact]
    public async Task VatAndCclFollowTheSitesTreatment()
    {
        var response = await Price("1600000001013", 18_000);
        var result = Engine.Calculate(Input([Site(1, 18_000, response)]));
        var site = Assert.Single(result.Sites);
        Assert.Equal(SiteTreatment.Standard, site.Treatment);

        var option = result.Options.First();
        var expected = VatCclCalculator.Apply(option.Sites[0].AnnualCostExVatCcl, 18_000, SiteTreatment.Standard, 0m, Settings().RatesFor(Start));
        Assert.Equal(expected.Ccl, option.TotalCcl);
        Assert.Equal(expected.Vat, option.TotalVat);
        Assert.Equal(expected.Total, option.TotalAnnualCostIncVatCcl);
    }

    [Fact]
    public async Task LowUseSiteGetsReducedVatAndNoCcl()
    {
        var response = await Price("1600000001031", 10_950);
        var result = Engine.Calculate(Input([Site(1, 10_950, response)]));
        Assert.Equal(SiteTreatment.LowUse, Assert.Single(result.Sites).Treatment);
        Assert.All(result.Options, o => Assert.Equal(0m, o.TotalCcl));
    }

    [Fact]
    public async Task CommissionUsesTheBasisAndThePaymentModel()
    {
        var response = await Price("1600000001013", 18_000);
        var result = Engine.Calculate(Input([Site(1, 18_000, response)], Settings(CommissionBasis.PerYear, upfront: 40m)));

        var edf24 = result.Options.Single(o => o.Supplier == "EDF" && o.TermMonths == 24);
        var raw = edf24.Sites[0].Rate.RawCommission!.Value;
        Assert.Equal(CommissionBasis.PerYear, result.CommissionBasis);
        Assert.Equal(raw * 2m, edf24.CommissionContractTotal);
        Assert.Equal(raw * 2m * 0.4m, edf24.CommissionUpfront);
        Assert.Equal((raw * 2m - raw * 2m * 0.4m) / 24m, edf24.CommissionMonthly);
    }

    [Fact]
    public async Task SavingsNeedCurrentRates()
    {
        var response = await Price("1600000001013", 18_000);
        var without = Engine.Calculate(Input([Site(1, 18_000, response)]));
        Assert.All(without.Options, o => Assert.Null(o.TotalSavingExVatCcl));
        Assert.Null(Assert.Single(without.Sites).CurrentAnnualCostExVatCcl);

        var with = Engine.Calculate(Input([Site(1, 18_000, response, new CurrentRates(30m, null, null, 70m))]));
        var current = 30m * 180m + 70m * 3.65m;
        Assert.Equal(current, Assert.Single(with.Sites).CurrentAnnualCostExVatCcl);
        var option = with.Options.First();
        Assert.Equal(current - option.TotalAnnualCostExVatCcl, option.TotalSavingExVatCcl);
    }

    [Fact]
    public async Task MicrobusinessIsDecidedFromAllSites()
    {
        var response = await Price("1600000001022", 90_000);
        var result = Engine.Calculate(Input([Site(1, 90_000, response)]));
        Assert.True(result.Microbusiness.IsMicrobusiness);
        Assert.Equal(MicrobusinessSource.Consumption, result.Microbusiness.Source);
    }

    [Fact]
    public async Task UpliftsAndCappingAreCarriedOntoTheOptions()
    {
        var response = await Price("1600000001013", 18_000, unitUplift: 1.0m);
        var result = Engine.Calculate(Input([Site(1, 18_000, response)], uplifts: Uplifts(requested: 1.5m, applied: 1.0m, onlyFor: "British Gas")));

        var bg = result.Options.First(o => o.Supplier == "British Gas");
        Assert.Equal(1.5m, bg.RequestedUnitRateUplift);
        Assert.Equal(1.0m, bg.UnitRateUplift);
        Assert.True(bg.IsCappedAtSupplierMax);
        Assert.False(result.Options.First(o => o.Supplier == "EDF").IsCappedAtSupplierMax);
    }

    [Fact]
    public async Task BaseRatesAreTheReturnedRateMinusTheUplift()
    {
        var response = await Price("1600000001013", 18_000, unitUplift: 1.5m);
        var site = Engine.Calculate(Input([Site(1, 18_000, response)])).Options.First().Sites[0];
        Assert.Equal(BaseRates.Compute(site.Rate.DayUnitrate, "1.5"), site.BaseDayRate);
        Assert.Equal(BaseRates.Compute(site.Rate.StandingCharge, "5"), site.BaseStandingCharge);
    }

    [Fact]
    public async Task SiteWithoutPricesMakesOptionsPartial()
    {
        var response = await Price("1600000001013", 18_000);
        var result = Engine.Calculate(Input([Site(1, 18_000, response), Site(2, 20_000, null)]));
        Assert.All(result.Options, o => { Assert.True(o.IsPartial); Assert.Equal(2, o.SitesTotal); });
        Assert.Equal(2, result.Sites.Count);
    }

    [Fact]
    public async Task CrossCheckNeverChangesTheShownFigures()
    {
        var response = await Price("1600000001013", 18_000);
        var tampered = response with
        {
            Rates = response.Rates.Select(r => r with { RawAnnualPrice = r.RawAnnualPrice + 50m }).ToList(),
        };

        var result = Engine.Calculate(Input([Site(1, 18_000, tampered)]));

        Assert.All(result.Options, o =>
        {
            Assert.Equal(o.Sites[0].Rate.RawAnnualPrice, o.TotalAnnualCostExVatCcl);
            Assert.Equal(50m, o.Sites[0].CrossCheckDifference);
        });
        Assert.NotEmpty(result.Warnings);
    }
}
