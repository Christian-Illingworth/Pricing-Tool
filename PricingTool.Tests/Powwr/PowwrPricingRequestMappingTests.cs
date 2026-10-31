using System.Text.Json;
using PricingTool.Domain;
using PricingTool.Powwr.Pricing;

namespace PricingTool.Tests.Powwr;

// One test per row of contracts/powwr-uk-pricing-mapping.md for QuoteDetails, ElectricSupply and
// GasSupply. Made-up data only.
public class PowwrPricingRequestMappingTests
{
    private const string Licence = "LICENCE-FOR-TESTS";

    private static readonly IReadOnlyDictionary<string, string> BusinessTypes = new Dictionary<string, string>
    {
        ["SoleTrader"] = "Sole Trader", ["Partnership"] = "Partnership", ["LimitedCompany"] = "Ltd Company",
        ["Llp"] = "LLP", ["Plc"] = "PLC", ["Charity"] = "Charity", ["PublicSector"] = "Public Sector",
    };

    private static ElectricPriceRequest Electric(Action<ElectricPriceRequestBuilder>? change = null)
    {
        var builder = new ElectricPriceRequestBuilder();
        change?.Invoke(builder);
        return builder.Build();
    }

    // Lets each test vary one thing from a valid request.
    private sealed class ElectricPriceRequestBuilder
    {
        public QuoteType QuoteType = QuoteType.Renewal;
        public DateOnly? CotDate;
        public IReadOnlyList<int> Terms = [12, 24];
        public string? TopLine = "03801631";
        public int? NightKwh;
        public int? WendKwh;
        public int? Kva;
        public int? Kvarh;
        public ForcedRateStructure? Forced;
        public BusinessType BusinessType = BusinessType.LimitedCompany;

        public ElectricPriceRequest Build() => new()
        {
            QuoteReference = "CUQ-26-000042",
            AgentName = "Alex Agent",
            Postcode = "ZZ1 1AA",
            QuoteType = QuoteType,
            CotDate = CotDate,
            OutOfContract = true,
            PaymentMethod = "Monthly Direct Debit",
            BusinessType = BusinessType,
            CurrentSupplierPricingName = "British Gas",
            StartDate = new DateOnly(2026, 12, 1),
            TermsMonths = Terms,
            DefaultUplift = new UpliftPair(1.5m, 5m),
            SupplierPlans =
            [
                new SupplierPlan("EDF", Terms.Select(t => new PlanUplift(t, t == 24 ? 2.0m : 1.5m, 5m)).ToList()),
                new SupplierPlan("British Gas", Terms.Select(t => new PlanUplift(t, 1.0m, 5m)).ToList()),
            ],
            CurrentContractEndDate = new DateOnly(2026, 11, 30),
            SmartMeter = true,
            MpanTopLine = TopLine,
            MpanCore = "1600000001013",
            ProfileClass = 3,
            MeterTimeswitchClass = 801,
            LineLossFactorClass = "631",
            StandardSettlementConfig = "0393",
            MeasurementClass = "A",
            MeterType = "N",
            DayKwh = 18_000,
            NightKwh = NightKwh,
            WendKwh = WendKwh,
            KvaCapacity = Kva,
            KvarhConsumption = Kvarh,
            ForcedRateStructure = Forced,
        };
    }

    private static GasPriceRequest Gas() => new()
    {
        QuoteReference = "CUQ-26-000043",
        AgentName = "Alex Agent",
        Postcode = "ZZ1 1AG",
        QuoteType = QuoteType.NewSupplier,
        PaymentMethod = "BACS",
        BusinessType = BusinessType.Charity,
        StartDate = new DateOnly(2027, 1, 1),
        TermsMonths = [36],
        DefaultUplift = new UpliftPair(0.5m, 5m),
        SupplierPlans = [new SupplierPlan("Octopus Energy", [new PlanUplift(36, 0.5m, 5m)])],
        SmartMeter = true,
        Mprn = "1234567",
        AnnualQuantityKwh = 40_000,
    };

    // ── QuoteDetails ──

    [Fact]
    public void QuoteReferenceIsOurReference()
    {
        Assert.Equal("CUQ-26-000042", PowwrPricingMapper.ToElectricRequest(Electric(), Licence, BusinessTypes).QuoteDetails!.QuoteReference);
    }

    [Fact]
    public void SecurityDetailsCarryTheLicenceAndTheAgentsName()
    {
        var security = PowwrPricingMapper.ToElectricRequest(Electric(), Licence, BusinessTypes).QuoteDetails!.SecurityDetails!;
        Assert.Equal(Licence, security.LicenceCode);
        Assert.Equal("Alex Agent", security.Mascaradeuser);
    }

    [Fact]
    public void StoredCopyMasksTheLicenceCode()
    {
        var masked = PowwrPricingMapper.SerializeMasked(PowwrPricingMapper.ToElectricRequest(Electric(), Licence, BusinessTypes));
        Assert.DoesNotContain(Licence, masked);
        using var json = JsonDocument.Parse(masked);
        Assert.Equal("***", json.RootElement.GetProperty("quoteDetails").GetProperty("SecurityDetails").GetProperty("LicenceCode").GetString());
        Assert.Equal("Alex Agent", json.RootElement.GetProperty("quoteDetails").GetProperty("SecurityDetails").GetProperty("mascaradeuser").GetString());
    }

    [Fact]
    public void JsonUsesPowwrsNamesAndLeavesOutNulls()
    {
        var json = PowwrPricingMapper.Serialize(PowwrPricingMapper.ToElectricRequest(Electric(), Licence, BusinessTypes));
        Assert.Contains("\"quoteDetails\":", json);
        Assert.Contains("\"MPANBottom\":\"1600000001013\"", json);
        Assert.Contains("\"PostCode\":\"ZZ1 1AA\"", json);
        Assert.DoesNotContain("\"GasSupply\"", json);
        Assert.DoesNotContain("\"PlanType\"", json);
        Assert.DoesNotContain("null", json);
    }

    [Fact]
    public void PostcodeIsSent()
    {
        Assert.Equal("ZZ1 1AA", PowwrPricingMapper.ToElectricRequest(Electric(), Licence, BusinessTypes).QuoteDetails!.PostCode);
    }

    [Fact]
    public void ShowNoQuoteReasonIsAlwaysSentAndTheAppSortsForItself()
    {
        var details = PowwrPricingMapper.ToElectricRequest(Electric(), Licence, BusinessTypes).QuoteDetails!;
        var setting = Assert.Single(details.Settings!);
        Assert.Equal("ShowNoQuoteReason", setting.Key);
        Assert.Equal("true", setting.Value);
        Assert.False(details.SortByCommission);
    }

    [Fact]
    public void RenewalSetsRenewalOnly()
    {
        var details = PowwrPricingMapper.ToElectricRequest(Electric(), Licence, BusinessTypes).QuoteDetails!;
        Assert.True(details.Renewal);
        Assert.False(details.COT);
        Assert.Null(details.CotDate);
    }

    [Fact]
    public void ChangeOfTenancySetsCotAndItsDate()
    {
        var details = PowwrPricingMapper.ToElectricRequest(
            Electric(b => { b.QuoteType = QuoteType.ChangeOfTenancy; b.CotDate = new DateOnly(2026, 10, 1); }), Licence, BusinessTypes).QuoteDetails!;
        Assert.False(details.Renewal);
        Assert.True(details.COT);
        Assert.Equal("2026-10-01", details.CotDate);
    }

    [Fact]
    public void OutOfContractIsSent()
    {
        Assert.True(PowwrPricingMapper.ToElectricRequest(Electric(), Licence, BusinessTypes).QuoteDetails!.OutOfContract);
    }

    [Fact]
    public void CurrentSupplierIsThePricingName()
    {
        var request = PowwrPricingMapper.ToElectricRequest(Electric(), Licence, BusinessTypes);
        Assert.Equal("British Gas", request.QuoteDetails!.CurrentSupplier);
        Assert.Equal("British Gas", request.QuoteDetails.ElectricSupply!.CurrentSupplier);
    }

    [Theory]
    [InlineData(PaymentMethod.DirectDebit, PaymentFrequency.Monthly, null, "Monthly Direct Debit")]
    [InlineData(PaymentMethod.DirectDebit, PaymentFrequency.Quarterly, PaymentAmount.Fixed, "Quarterly Fixed Direct Debit")]
    [InlineData(PaymentMethod.DirectDebit, PaymentFrequency.Monthly, PaymentAmount.Variable, "Monthly Variable Direct Debit")]
    [InlineData(PaymentMethod.DirectDebit, null, null, "Direct Debit")]
    [InlineData(PaymentMethod.Bacs, PaymentFrequency.Monthly, null, "BACS")]
    [InlineData(PaymentMethod.CashCheque, null, null, "Cash/Cheque")]
    public void PaymentMethodTextReadsLikePowwrsExamples(PaymentMethod method, PaymentFrequency? frequency, PaymentAmount? amount, string expected)
    {
        Assert.Equal(expected, PaymentMethodText.For(method, frequency, amount));
    }

    [Fact]
    public void PaymentMethodIsSentAsGiven()
    {
        Assert.Equal("Monthly Direct Debit", PowwrPricingMapper.ToElectricRequest(Electric(), Licence, BusinessTypes).QuoteDetails!.PaymentMethod);
    }

    [Fact]
    public void BusinessTypeGoesThroughTheConfigurationMap()
    {
        Assert.Equal("Ltd Company", PowwrPricingMapper.ToElectricRequest(Electric(), Licence, BusinessTypes).QuoteDetails!.BusinessType);
        Assert.Equal("Charity", PowwrPricingMapper.ToGasRequest(Gas(), Licence, BusinessTypes).QuoteDetails!.BusinessType);
    }

    [Fact]
    public void QuoteLevelUpliftIsTheDefault()
    {
        var details = PowwrPricingMapper.ToElectricRequest(Electric(), Licence, BusinessTypes).QuoteDetails!;
        Assert.Equal(1.5m, details.Uplift);
        Assert.Equal(5m, details.StandingChargeUplift);
    }

    [Fact]
    public void OneQuoteDefinitionPerSupplierPlanAndOnePlanPerTerm()
    {
        // Which suppliers are sent (active, supplying the fuel, passing include/exclude) is decided by
        // the request builder (US1); the mapper sends each one it's given.
        var definitions = PowwrPricingMapper.ToElectricRequest(Electric(), Licence, BusinessTypes).QuoteDetails!.QuoteDefinitions!;
        Assert.Equal(["EDF", "British Gas"], definitions.Select(d => d.Supplier));

        var edf = definitions[0].Plans!;
        Assert.Equal([12, 24], edf.Select(p => p.Duration!.Value));
        Assert.Equal([1.5m, 2.0m], edf.Select(p => p.Uplift!.Value));
        Assert.All(edf, p => Assert.Equal(5m, p.StandingChargeUplift));
        Assert.All(edf, p => { Assert.Null(p.PlanType); Assert.Null(p.Id); });
    }

    // ── ElectricSupply ──

    [Fact]
    public void ProfileClassAndMtcAreIntegers()
    {
        var supply = PowwrPricingMapper.ToElectricRequest(Electric(), Licence, BusinessTypes).QuoteDetails!.ElectricSupply!;
        Assert.Equal(3, supply.ProfileClass);
        Assert.Equal(801, supply.MeterTimeswitchClass);
        Assert.Equal("631", supply.LineLossFactorClass);
        Assert.Equal("0393", supply.StandardSettlementConfig);
        Assert.Equal("A", supply.MeasurementClass);
        Assert.Equal("N", supply.MeterType);
        Assert.True(supply.SmartMeter);
    }

    [Fact]
    public void TopLineAndCoreArePassedThrough()
    {
        var supply = PowwrPricingMapper.ToElectricRequest(Electric(), Licence, BusinessTypes).QuoteDetails!.ElectricSupply!;
        Assert.Equal("03801631", supply.MPANTop);
        Assert.Equal("1600000001013", supply.MPANBottom);
    }

    [Fact]
    public void NineCharacterTopLineIsPassedThrough()
    {
        var supply = PowwrPricingMapper.ToElectricRequest(Electric(b => b.TopLine = "040151661"), Licence, BusinessTypes).QuoteDetails!.ElectricSupply!;
        Assert.Equal("040151661", supply.MPANTop);
    }

    [Fact]
    public void ForcedRateStructureIsSentOnlyWhenChosen()
    {
        Assert.Null(PowwrPricingMapper.ToElectricRequest(Electric(), Licence, BusinessTypes).QuoteDetails!.ElectricSupply!.ForcedRateStructure);

        var supply = PowwrPricingMapper.ToElectricRequest(Electric(b => b.Forced = ForcedRateStructure.Economy7), Licence, BusinessTypes).QuoteDetails!.ElectricSupply!;
        Assert.Equal("Economy 7", supply.ForcedRateStructure);
    }

    [Fact]
    public void RegisterConsumptionIsSentForTheRegistersGiven()
    {
        var single = PowwrPricingMapper.ToElectricRequest(Electric(), Licence, BusinessTypes).QuoteDetails!.ElectricSupply!;
        Assert.Equal(18_000, single.DayConsumption!.Amount);
        Assert.Null(single.NightConsumption);
        Assert.Null(single.WendConsumption);

        var threeRate = PowwrPricingMapper.ToElectricRequest(Electric(b => { b.NightKwh = 9_000; b.WendKwh = 3_000; }), Licence, BusinessTypes).QuoteDetails!.ElectricSupply!;
        Assert.Equal(9_000, threeRate.NightConsumption!.Amount);
        Assert.Equal(3_000, threeRate.WendConsumption!.Amount);
    }

    [Fact]
    public void KvaAndKvarhAreSentForHalfHourlyMeters()
    {
        var plain = PowwrPricingMapper.ToElectricRequest(Electric(), Licence, BusinessTypes).QuoteDetails!.ElectricSupply!;
        Assert.Null(plain.KvaConsumption);
        Assert.Null(plain.KvarhConsumption);

        var halfHourly = PowwrPricingMapper.ToElectricRequest(Electric(b => { b.Kva = 200; b.Kvarh = 12_000; }), Licence, BusinessTypes).QuoteDetails!.ElectricSupply!;
        Assert.Equal(200, halfHourly.KvaConsumption!.Amount);
        Assert.Equal(12_000, halfHourly.KvarhConsumption!.Amount);
    }

    [Fact]
    public void ContractRenewalDateIsTheNewStartDate()
    {
        var supply = PowwrPricingMapper.ToElectricRequest(Electric(), Licence, BusinessTypes).QuoteDetails!.ElectricSupply!;
        Assert.Equal("2026-12-01", supply.ContractRenewalDate);
        Assert.Equal("2026-11-30", supply.ContractEndDate);
    }

    [Fact]
    public void NewContractEndDateOnlyWhenOneTermIsRequested()
    {
        Assert.Null(PowwrPricingMapper.ToElectricRequest(Electric(), Licence, BusinessTypes).QuoteDetails!.ElectricSupply!.NewContractEndDate);

        var oneTerm = PowwrPricingMapper.ToElectricRequest(Electric(b => b.Terms = [24]), Licence, BusinessTypes).QuoteDetails!.ElectricSupply!;
        Assert.Equal("2028-11-30", oneTerm.NewContractEndDate);   // start + 24 months − 1 day
    }

    // ── GasSupply ──

    [Fact]
    public void GasSendsMprConsumptionAndSmartMeter()
    {
        var details = PowwrPricingMapper.ToGasRequest(Gas(), Licence, BusinessTypes).QuoteDetails!;
        Assert.Null(details.ElectricSupply);
        Assert.Equal("1234567", details.GasSupply!.MPR);
        Assert.Equal(40_000, details.GasSupply.Consumption!.Amount);
        Assert.True(details.GasSupply.SmartMeter);
        Assert.Equal("2027-01-01", details.GasSupply.ContractRenewalDate);
        Assert.Equal("2029-12-31", details.GasSupply.NewContractEndDate);   // one term: 36 months
        Assert.False(details.Renewal);
        Assert.False(details.COT);
    }

    // ── /prompts and /pricechange requests ──

    [Fact]
    public void PromptsSendTheTopLineOnlyWhenItIsLegacy()
    {
        var legacy = PowwrPricingMapper.ToPromptsRequest(new RateStructureRequest { MpanTopLine = "03801631", MpanCore = "1600000001013" }, Licence).PromptsSearch!;
        Assert.Equal("03801631", legacy.MPANTop);
        Assert.Equal("1600000001013", legacy.MPANBottom);
        Assert.Null(legacy.Suppliers);

        var mhhs = PowwrPricingMapper.ToPromptsRequest(new RateStructureRequest
        {
            MpanTopLine = "040151661", MpanCore = "1600000001022", ProfileClass = 4, StandardSettlementConfig = "0151",
            DuosTariffId = "661", MhhsMigrated = true,
        }, Licence).PromptsSearch!;
        Assert.Null(mhhs.MPANTop);
        Assert.Equal(4, mhhs.ProfileClass);
        Assert.Equal("0151", mhhs.StandardSettlementConfig);
        Assert.Equal("661", mhhs.DuosTariffId);
        Assert.True(mhhs.MhhsMigrated);
    }

    [Fact]
    public void PriceChangeAsksForEverySupplierWithoutFutureChanges()
    {
        var request = PowwrPricingMapper.ToPriceChangeRequest(null, Licence).PriceChangeRequest!;
        Assert.Null(request.SupplierName);
        Assert.False(request.IncludeFuture);
        Assert.Equal(Licence, request.SecurityDetails!.LicenceCode);
    }
}
