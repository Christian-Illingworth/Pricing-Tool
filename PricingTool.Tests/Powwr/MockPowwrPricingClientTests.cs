using System.Globalization;
using Microsoft.Extensions.Options;
using PricingTool.Domain;
using PricingTool.Options;
using PricingTool.Powwr.Pricing;
using PricingTool.Tests.Support;

namespace PricingTool.Tests.Powwr;

public class MockPowwrPricingClientTests
{
    private static readonly int[] AllTerms = [12, 24, 36, 48, 60];

    private sealed class Monitor(PowwrMockOptions value) : IOptionsMonitor<PowwrMockOptions>
    {
        public PowwrMockOptions CurrentValue => value;
        public PowwrMockOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<PowwrMockOptions, string?> listener) => null;
    }

    private static MockPowwrPricingClient Client(params string[] changedSuppliers) => new(
        FakeTimeProvider.At("2026-10-31T10:00:00Z"),
        Microsoft.Extensions.Options.Options.Create(new PowwrPricingOptions()),
        new Monitor(new PowwrMockOptions { ChangedSuppliers = changedSuppliers }));

    private static IReadOnlyList<SupplierPlan> AllSuppliers(decimal unitUplift, decimal standingUplift, int[] terms) =>
        MockPowwrPricingClient.Suppliers.Select(s => new SupplierPlan(s, terms.Select(t => new PlanUplift(t, unitUplift, standingUplift)).ToList())).ToList();

    private static ElectricPriceRequest Electric(
        string mpan = "1600000001013", int? night = null, int? kva = null, decimal uplift = 1.5m, decimal standingUplift = 5m) => new()
    {
        QuoteReference = "CUQ-26-000042",
        AgentName = "Alex Agent",
        Postcode = "ZZ1 1AA",
        QuoteType = QuoteType.Renewal,
        PaymentMethod = "Monthly Direct Debit",
        BusinessType = BusinessType.LimitedCompany,
        StartDate = new DateOnly(2026, 12, 1),
        TermsMonths = AllTerms,
        DefaultUplift = new UpliftPair(uplift, standingUplift),
        SupplierPlans = AllSuppliers(uplift, standingUplift, AllTerms),
        MpanCore = mpan,
        DayKwh = 18_000,
        NightKwh = night,
        KvaCapacity = kva,
    };

    private static GasPriceRequest Gas(string mprn = "1234567") => new()
    {
        QuoteReference = "CUQ-26-000043",
        AgentName = "Alex Agent",
        Postcode = "ZZ1 1AG",
        QuoteType = QuoteType.NewSupplier,
        PaymentMethod = "Monthly Direct Debit",
        BusinessType = BusinessType.Charity,
        StartDate = new DateOnly(2026, 12, 1),
        TermsMonths = [12],
        DefaultUplift = new UpliftPair(0.5m, 5m),
        SupplierPlans = AllSuppliers(0.5m, 5m, [12]),
        Mprn = mprn,
        AnnualQuantityKwh = 40_000,
    };

    private static decimal Num(string? s) => s is null ? 0m : decimal.Parse(s, CultureInfo.InvariantCulture);

    [Fact]
    public async Task SameMeterAndConsumptionGiveTheSamePrices()
    {
        var first = await Client().GetElectricPricesAsync(Electric(), default);
        var second = await Client().GetElectricPricesAsync(Electric(), default);

        Assert.Equal(first.Rates.Select(r => (r.Supplier, r.TermMonths, r.DayUnitrate, r.StandingCharge, r.RawAnnualPrice)),
                     second.Rates.Select(r => (r.Supplier, r.TermMonths, r.DayUnitrate, r.StandingCharge, r.RawAnnualPrice)));
    }

    [Fact]
    public async Task DifferentMetersGetDifferentPrices()
    {
        var a = await Client().GetElectricPricesAsync(Electric("1600000001013"), default);
        var b = await Client().GetElectricPricesAsync(Electric("1600000001031"), default);
        Assert.NotEqual(a.Rates.Select(r => r.DayUnitrate), b.Rates.Select(r => r.DayUnitrate));
    }

    [Fact]
    public async Task RawAnnualPriceMatchesTheCrossCheckExactly()
    {
        var response = await Client().GetElectricPricesAsync(Electric(night: 9_000, kva: 100), default);

        Assert.NotEmpty(response.Rates);
        foreach (var rate in response.Rates)
        {
            var expected = (Num(rate.DayUnitrate) * 18_000 + Num(rate.NightUnitrate) * 9_000) / 100m
                + Num(rate.StandingCharge) * 365m / 100m
                + 100 * Num(rate.CapacityCharge) * 365m / 100m;
            Assert.Equal(expected, rate.RawAnnualPrice);
        }
    }

    [Fact]
    public async Task CommissionIsTheUpliftsWorth()
    {
        var rate = (await Client().GetElectricPricesAsync(Electric(uplift: 1.5m), default)).Rates.First(r => r.PlanType == "Fixed");

        Assert.Equal(rate.RawAnnualPrice - rate.RawBaseAnnualPrice, rate.RawCommission);
        // 1.5 p/kWh on 18,000 kWh plus 5 p/day for a year.
        Assert.Equal(1.5m * 18_000 / 100m + 5m * 365m / 100m, rate.RawCommission);
        Assert.Equal("1.5", rate.Uplift);
        Assert.Equal("5", rate.StandingChargeUplift);
    }

    [Fact]
    public async Task LongerTermsCostTwoPercentMorePerYear()
    {
        var edf = (await Client().GetElectricPricesAsync(Electric(uplift: 0m), default)).Rates
            .Where(r => r.Supplier == "EDF").ToDictionary(r => r.TermMonths, r => Num(r.DayUnitrate));
        Assert.Equal(decimal.Round(edf[12] * 1.02m, 3), edf[24]);
        Assert.Equal(decimal.Round(edf[12] * 1.08m, 3), edf[60]);
    }

    [Fact]
    public async Task BaseRatesStayInTheirRanges()
    {
        var twelve = (await Client().GetElectricPricesAsync(Electric(night: 9_000, uplift: 0m, standingUplift: 0m), default)).Rates
            .Where(r => r.TermMonths == 12 && r.PlanType == "Fixed").ToList();
        Assert.All(twelve, r => Assert.InRange(Num(r.DayUnitrate), 22m, 28m));
        Assert.All(twelve, r => Assert.InRange(Num(r.NightUnitrate), 14m, 18m));
        Assert.All(twelve, r => Assert.InRange(Num(r.StandingCharge), 40m, 90m));
    }

    [Fact]
    public async Task EveryResultHasTheContractsVariety()
    {
        var response = await Client().GetElectricPricesAsync(Electric(), default);

        Assert.True(response.Succeeded);
        Assert.Contains(response.NoQuotes, n => n.SupplierName == "Valda Energy" && n.Reason == "Meter type not supported");
        Assert.Contains(response.Rates, r => r.Supplier == "Yu Energy" && r.TermMonths == 60 && r.OutsidePricingWindow);
        Assert.DoesNotContain(response.Rates, r => r.Supplier == "Yu Energy" && r.TermMonths != 60 && r.OutsidePricingWindow);
        Assert.Contains(response.Rates, r => r.Supplier == "Octopus Energy" && r.FooterMessage is not null);
        Assert.Contains(response.Rates, r => r.Supplier == "Opus Energy" && r.StandingCharge == "0.00");
        Assert.All(response.Rates, r => Assert.Equal("MOCK-20261031", r.PricebookVersion));
    }

    [Fact]
    public async Task EveryoneDeclinesForTheScenarioMpan()
    {
        var response = await Client().GetElectricPricesAsync(Electric(MockPowwrPricingClient.EveryoneDeclinesMpan), default);
        Assert.True(response.Succeeded);
        Assert.Empty(response.Rates);
        Assert.Equal(MockPowwrPricingClient.Suppliers.Count, response.NoQuotes.Count);
    }

    [Fact]
    public async Task OnlyBritishGasAndOctopusPriceTheMhhsMeter()
    {
        var response = await Client().GetElectricPricesAsync(Electric(MockPowwrPricingClient.MhhsNoSscMpan), default);
        Assert.Equal(["British Gas", "Octopus Energy"], response.Rates.Select(r => r.Supplier).Distinct().Order());
    }

    [Fact]
    public async Task ScenarioMprnGivesAPowwrError()
    {
        var response = await Client().GetGasPricesAsync(Gas(MockPowwrPricingClient.PowwrErrorMprn), default);
        Assert.False(response.Succeeded);
        Assert.Equal(ErrorCategory.PowwrError, response.ErrorCategory);
        Assert.NotNull(response.ErrorMessage);
    }

    [Fact]
    public async Task GasIsSingleRateInItsRanges()
    {
        var rates = (await Client().GetGasPricesAsync(Gas(), default)).Rates.Where(r => r.PlanType == "Fixed").ToList();
        Assert.NotEmpty(rates);
        Assert.All(rates, r =>
        {
            Assert.InRange(Num(r.DayUnitrate) - 0.5m, 5m, 8m);
            Assert.Null(r.NightUnitrate);
            Assert.Equal((Num(r.DayUnitrate) * 40_000) / 100m + Num(r.StandingCharge) * 365m / 100m, r.RawAnnualPrice);
        });
    }

    [Fact]
    public async Task StoredRequestHasTheLicenceMasked()
    {
        var response = await Client().GetElectricPricesAsync(Electric(), default);
        Assert.Contains("\"LicenceCode\":\"***\"", response.RequestJsonMasked);
        Assert.DoesNotContain("MOCK-LICENCE", response.RequestJsonMasked);
    }

    [Fact]
    public async Task PriceChangesAreTodaysUnlessConfiguredAsChanged()
    {
        var unchanged = await Client().GetLatestPriceChangesAsync(null, default);
        Assert.All(unchanged, c => Assert.Equal("MOCK-20261031", c.Version));

        var changed = await Client("EDF").GetLatestPriceChangesAsync(null, default);
        Assert.All(changed.Where(c => c.Supplier == "EDF"), c => Assert.Equal("MOCK-CHANGED", c.Version));
        Assert.All(changed.Where(c => c.Supplier != "EDF"), c => Assert.Equal("MOCK-20261031", c.Version));
    }

    [Fact]
    public async Task RateStructureFollowsTheSettlementConfiguration()
    {
        var single = await Client().GetRateStructureAsync(new RateStructureRequest { MpanCore = "1600000001013", StandardSettlementConfig = "0393", ProfileClass = 3 }, default);
        Assert.Equal([Register.Day], single.Registers);

        var dayNight = await Client().GetRateStructureAsync(new RateStructureRequest { MpanCore = "1600000001022", StandardSettlementConfig = "0151", ProfileClass = 4 }, default);
        Assert.Equal([Register.Day, Register.Night], dayNight.Registers);

        var mhhs = await Client().GetRateStructureAsync(new RateStructureRequest { MpanCore = MockPowwrPricingClient.MhhsNoSscMpan, MhhsMigrated = true }, default);
        Assert.False(mhhs.Succeeded);
    }
}
