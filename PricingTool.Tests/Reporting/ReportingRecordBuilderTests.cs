using System.Text.Json;
using PricingTool.Data.Entities;
using PricingTool.Domain;
using PricingTool.Services.Reporting;
using PricingTool.Tests.Support;

namespace PricingTool.Tests.Reporting;

// FR-084 and contracts/reporting-record.md: every event carries the full quote snapshot, money as
// { value, display } pairs, and never the customer's contact details or a secret. Made-up data only.
public class ReportingRecordBuilderTests
{
    private static readonly Guid RecordId = Guid.Parse("8d4c6a3e-2f1b-4c9a-9e57-1b2c3d4e5f60");
    private static readonly DateTime Occurred = new(2026, 10, 30, 10, 15, 4, 123, DateTimeKind.Utc);

    // Distinctive contact details, so the test can prove they never appear.
    private const string ContactName = "Jo Contactperson";
    private const string ContactEmail = "jo.private.contact@example.com";
    private const string ContactPhone = "01632960999";

    private static readonly StaffMember Agent = new() { Id = 12, DisplayName = "Alex Agent", Email = "alex.agent@example.com", EntraObjectId = "oid-1" };
    private static readonly Supplier Edf = new() { Id = 2, PricingName = "EDF" };

    private static Quote BuildQuote()
    {
        var quote = new Quote
        {
            Id = 42,
            Reference = "CUQ-26-000042",
            OwnerId = Agent.Id,
            Owner = Agent,
            Fuel = Fuel.Electricity,
            QuoteType = QuoteType.Renewal,
            Customer = new QuoteCustomer
            {
                BusinessName = "Acme Trading Ltd", BusinessType = BusinessType.LimitedCompany, CompanyNumber = "12345678",
                ContactName = ContactName, ContactEmail = ContactEmail, ContactPhone = ContactPhone,
            },
            Contract = new QuoteContract
            {
                StartDate = new DateOnly(2026, 12, 1), TermsMonths = "12,24,36", PaymentMethod = PaymentMethod.DirectDebit,
                PaymentFrequency = PaymentFrequency.Monthly, DefaultUnitRateUplift = 1.5m, DefaultStandingChargeUplift = 5m,
            },
            VatCclTreatment = VatCclTreatment.Standard,
        };
        quote.Sites.Add(new Site
        {
            Id = 7, QuoteId = 42, Ordinal = 1, Postcode = "ZZ1 1AB", AddressLine1 = "Unit 2, Test Park",
            ConsumptionDayKwh = 63_000, ConsumptionNightKwh = 27_000,
            CurrentDayRate = 30.1m, CurrentNightRate = 19.2m, CurrentStandingCharge = 75m, CurrentSupplierText = "British Gas",
            Meter = new SiteMeter
            {
                MpanTopLine = "04845661", MpanCore = "1600000001022", DistributorId = "16", RegionName = "North Western",
                ProfileClass = 4, MeterTimeswitchClass = 845, LineLossFactorClass = "661", StandardSettlementConfig = "0151",
                SmartMeter = true, RateRegisters = "Day,Night",
            },
        });
        quote.UpliftOverrides.Add(new QuoteUpliftOverride { QuoteId = 42, SupplierId = 2, Supplier = Edf, TermMonths = 36, UnitRateUplift = 2.0m, StandingChargeUplift = 5m });
        return quote;
    }

    private static PricingRun BuildRun(Quote quote)
    {
        var runSite = new PricingRunSite
        {
            Id = 70, SiteId = 7, Site = quote.Sites[0], Status = PricingRunSiteStatus.Succeeded, PowwrReference = "MOCK-REF-0001",
            RequestJson = "{\"quoteDetails\":{\"SecurityDetails\":{\"LicenceCode\":\"***\"}}}", ResponseJson = "{\"raw\":\"response\"}",
            Treatment = SiteTreatment.Standard, AnnualKwh = 90_000, StartDateUsed = new DateOnly(2026, 12, 1),
        };
        runSite.NoQuotes.Add(new NoQuoteResult { SupplierName = "Valda Energy", Reason = "Meter type not supported" });

        var edf = new PriceOption
        {
            Id = 100, SupplierName = "EDF", SupplierId = 2, TermMonths = 24, SitesCovered = 1, SitesTotal = 1,
            TotalAnnualCostExVatCcl = 21_892.36m, TotalCcl = 720.9m, TotalVat = 4_522.652m, TotalAnnualCostIncVatCcl = 27_135.912m,
            TotalSavingExVatCcl = 2_528.39m, CommissionContractTotal = 2_736.5m, CommissionUpfront = 0m, CommissionMonthly = 114.0208333333m,
            UnitRateUplift = 1.5m, StandingChargeUplift = 5m, RequestedUnitRateUplift = 1.5m, RequestedStandingChargeUplift = 5m,
            IsSelected = true, IsRecommended = true,
        };
        edf.Sites.Add(new PriceOptionSite
        {
            PricingRunSiteId = 70, PricingRunSite = runSite, DayUnitrate = "26.912", NightUnitrate = "17.450", StandingCharge = "62.00",
            Uplift = "1.5", StandingChargeUplift = "5", RawAnnualPrice = 21_892.36m, RawCommission = 1_368.25m,
            BaseDayRate = 25.412m, BaseNightRate = 15.950m, BaseStandingCharge = 57.00m, PricebookVersion = "MOCK-20261030",
            AnnualCostExVatCcl = 21_892.36m, Ccl = 720.9m, Vat = 4_522.652m, AnnualCostIncVatCcl = 27_135.912m, SavingExVatCcl = 2_528.39m,
            CommissionContractTotal = 2_736.5m, CommissionUpfront = 0m, CommissionMonthly = 114.0208333333m,
            ExtraInfo = "Agent note", FooterMessage = "Supplier footer",
        });

        var britishGas = new PriceOption
        {
            Id = 101, SupplierName = "British Gas", SupplierId = 1, TermMonths = 12, SitesCovered = 1, SitesTotal = 1,
            TotalAnnualCostExVatCcl = 22_000m, CommissionContractTotal = 1_200m, IsCappedAtSupplierMax = true,
            UnitRateUplift = 1.0m, RequestedUnitRateUplift = 1.5m,
        };
        britishGas.Sites.Add(new PriceOptionSite { PricingRunSiteId = 70, PricingRunSite = runSite, DayUnitrate = "25.000", StandingCharge = "50.00", AnnualCostExVatCcl = 22_000m });

        var run = new PricingRun
        {
            Id = 5, QuoteId = 42, RunNumber = 1, Status = PricingRunStatus.Succeeded, CommissionBasis = CommissionBasis.PerYear,
            FetchedUtc = new DateTime(2026, 10, 30, 10, 15, 2, 900, DateTimeKind.Utc), IsMicrobusiness = true,
            MicrobusinessSource = MicrobusinessSource.Consumption, CombinedAnnualKwh = 90_000,
        };
        run.Sites.Add(runSite);
        run.Options.AddRange([edf, britishGas]);
        return run;
    }

    private static ReportingRecord Build(ReportingEventType type, bool withRun = true, ReportingPdf? pdf = null,
        DraftMethod? outlook = null, ReportingFailure? failure = null)
    {
        var quote = BuildQuote();
        return ReportingRecordBuilder.Build(
            new ReportingEvent(type, quote, Agent, withRun ? BuildRun(quote) : null, pdf, outlook, failure), RecordId, Occurred);
    }

    private static JsonElement Payload(ReportingRecord record) => JsonDocument.Parse(record.Payload).RootElement;

    public static TheoryData<ReportingEventType> EventTypes() =>
        [.. Enum.GetValues<ReportingEventType>()];

    [Theory]
    [MemberData(nameof(EventTypes))]
    public void EveryEventTypeHasTheSchemasRequiredFields(ReportingEventType type)
    {
        var record = type switch
        {
            ReportingEventType.QuoteRequested => Build(type, withRun: false),
            ReportingEventType.PdfGenerated => Build(type, pdf: new ReportingPdf(9, "Acme_ElectricityQuote_2026-10-30.pdf")),
            ReportingEventType.OpenedInOutlook => Build(type, pdf: new ReportingPdf(9, "Acme_ElectricityQuote_2026-10-30.pdf"), outlook: DraftMethod.Eml),
            ReportingEventType.Failed => Build(type, failure: new ReportingFailure("Pricing", "Timeout", "POWWR took too long to answer.")),
            _ => Build(type),
        };

        using var schema = SchemaRequiredChecker.LoadSchema("reporting-record.v1.schema.json");
        Assert.Empty(SchemaRequiredChecker.Problems(schema.RootElement, Payload(record)));
        Assert.Equal(type.ToString(), Payload(record).GetProperty("eventType").GetString());
    }

    [Fact]
    public void SchemaCheckCatchesMissingFieldsAndBadValues()
    {
        using var schema = SchemaRequiredChecker.LoadSchema("reporting-record.v1.schema.json");
        using var incomplete = JsonDocument.Parse("""{ "schemaVersion": 2, "eventType": "Invented" }""");

        var problems = SchemaRequiredChecker.Problems(schema.RootElement, incomplete.RootElement);

        Assert.Contains("$.recordId is missing", problems);
        Assert.Contains(problems, p => p.StartsWith("$.schemaVersion"));
        Assert.Contains(problems, p => p.StartsWith("$.eventType"));
    }

    [Fact]
    public void SchemaVersionIdAndTimeAreSet()
    {
        var record = Build(ReportingEventType.PricesReturned);
        var json = Payload(record);

        Assert.Equal(1, record.SchemaVersion);
        Assert.Equal(1, json.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(RecordId, record.Id);
        Assert.Equal(RecordId.ToString(), json.GetProperty("recordId").GetString());
        Assert.Equal("2026-10-30T10:15:04.123Z", json.GetProperty("occurredUtc").GetString());
    }

    [Fact]
    public void EveryFr084FieldIsPresent()
    {
        var json = Payload(Build(ReportingEventType.PricesReturned));

        Assert.Equal("Alex Agent", json.GetProperty("agent").GetProperty("name").GetString());
        Assert.Equal("alex.agent@example.com", json.GetProperty("agent").GetProperty("email").GetString());

        var quote = json.GetProperty("quote");
        Assert.Equal("CUQ-26-000042", quote.GetProperty("reference").GetString());
        Assert.Equal("MOCK-REF-0001", quote.GetProperty("powwrReferences")[0].GetString());
        Assert.Equal("Renewal", quote.GetProperty("quoteType").GetString());
        Assert.True(quote.GetProperty("isMicrobusiness").GetBoolean());
        Assert.Equal("Consumption", quote.GetProperty("microbusinessSource").GetString());

        var customer = json.GetProperty("customer");
        Assert.Equal("Acme Trading Ltd", customer.GetProperty("businessName").GetString());
        Assert.Equal("12345678", customer.GetProperty("companyNumber").GetString());
        Assert.Equal("ZZ1 1AB", customer.GetProperty("postcode").GetString());

        var contract = json.GetProperty("contract");
        Assert.Equal("2026-12-01", contract.GetProperty("startDate").GetString());
        Assert.Equal([12, 24, 36], contract.GetProperty("termsMonths").EnumerateArray().Select(t => t.GetInt32()));
        Assert.Equal("Monthly Direct Debit", contract.GetProperty("paymentMethod").GetString());
        Assert.Equal(1.5m, contract.GetProperty("defaultUplift").GetProperty("unitRatePencePerKwh").GetDecimal());
        Assert.Equal("EDF", contract.GetProperty("upliftOverrides")[0].GetProperty("supplier").GetString());

        var site = json.GetProperty("sites")[0];
        var electricity = site.GetProperty("electricity");
        Assert.Equal("1600000001022", electricity.GetProperty("mpanCore").GetString());
        Assert.Equal(4, electricity.GetProperty("profileClass").GetInt32());
        Assert.Equal("North Western", electricity.GetProperty("region").GetString());
        Assert.Equal(90_000, site.GetProperty("consumption").GetProperty("totalKwh").GetInt32());
        Assert.Equal(30.1m, site.GetProperty("currentRates").GetProperty("dayPencePerKwh").GetDecimal());
        Assert.Equal("Standard", site.GetProperty("treatment").GetString());

        Assert.Equal("Valda Energy", json.GetProperty("noQuotes")[0].GetProperty("supplier").GetString());
    }

    [Fact]
    public void MoneyIsAValueAndDisplayPair()
    {
        var price = Payload(Build(ReportingEventType.PricesReturned)).GetProperty("prices").EnumerateArray()
            .Single(p => p.GetProperty("supplier").GetString() == "EDF");

        var vat = price.GetProperty("vat");
        Assert.Equal(4_522.652m, vat.GetProperty("value").GetDecimal());
        Assert.Equal(4_522.65m, vat.GetProperty("display").GetDecimal());

        var monthly = price.GetProperty("commission").GetProperty("monthly");
        Assert.Equal(114.0208333333m, monthly.GetProperty("value").GetDecimal());
        Assert.Equal(114.02m, monthly.GetProperty("display").GetDecimal());

        Assert.Equal(1_368.25m, price.GetProperty("commission").GetProperty("powwr").GetProperty("value").GetDecimal());
    }

    [Fact]
    public void ReturnedAndBaseRatesAreStrings()
    {
        var price = Payload(Build(ReportingEventType.PricesReturned)).GetProperty("prices").EnumerateArray()
            .Single(p => p.GetProperty("supplier").GetString() == "EDF");

        Assert.Equal("26.912", price.GetProperty("returned").GetProperty("dayUnitrate").GetString());
        Assert.Equal("25.412", price.GetProperty("base").GetProperty("dayUnitrate").GetString());
        Assert.Equal("15.950", price.GetProperty("base").GetProperty("nightUnitrate").GetString());
        Assert.Equal("57.00", price.GetProperty("base").GetProperty("standingCharge").GetString());
        Assert.Equal("MOCK-20261030", price.GetProperty("pricebookVersion").GetString());
    }

    [Fact]
    public void CommissionBasisAndCappingAreRecorded()
    {
        var json = Payload(Build(ReportingEventType.PricesReturned));
        Assert.Equal("PerYear", json.GetProperty("pricingRun").GetProperty("commissionBasis").GetString());

        var options = json.GetProperty("options").EnumerateArray().ToList();
        Assert.True(options.Single(o => o.GetProperty("supplier").GetString() == "British Gas").GetProperty("cappedAtSupplierMax").GetBoolean());
        Assert.False(options.Single(o => o.GetProperty("supplier").GetString() == "EDF").GetProperty("cappedAtSupplierMax").GetBoolean());

        var prices = json.GetProperty("prices").EnumerateArray().ToList();
        Assert.True(prices.Single(p => p.GetProperty("supplier").GetString() == "British Gas").GetProperty("cappedAtSupplierMax").GetBoolean());
    }

    [Fact]
    public void SelectionAndRecommendationAreRecorded()
    {
        var record = Build(ReportingEventType.PricesReturned);
        var edf = Payload(record).GetProperty("options").EnumerateArray().Single(o => o.GetProperty("supplier").GetString() == "EDF");
        Assert.True(edf.GetProperty("selected").GetBoolean());
        Assert.True(edf.GetProperty("recommended").GetBoolean());
        Assert.Equal(2_736.5m, record.RecommendedCommissionTotal);
    }

    [Fact]
    public void ContactDetailsLicenceAndRawPowwrDataAreNeverIncluded()
    {
        var record = Build(ReportingEventType.OpenedInOutlook, pdf: new ReportingPdf(9, "Acme.pdf"), outlook: DraftMethod.Graph);

        foreach (var forbidden in new[] { ContactName, ContactEmail, ContactPhone, "LicenceCode", "***", "\"raw\":\"response\"", "Agent note", "Supplier footer" })
            Assert.DoesNotContain(forbidden, record.Payload);
    }

    [Fact]
    public void FilterColumnsAreDenormalised()
    {
        var record = Build(ReportingEventType.PdfGenerated, pdf: new ReportingPdf(9, "Acme.pdf"));
        Assert.Equal(ReportingEventType.PdfGenerated, record.EventType);
        Assert.Equal(42, record.QuoteId);
        Assert.Equal("CUQ-26-000042", record.QuoteReference);
        Assert.Equal(12, record.AgentId);
        Assert.Equal(Fuel.Electricity, record.Fuel);
        Assert.Equal(QuoteType.Renewal, record.QuoteType);
        Assert.Equal("North Western", record.Regions);
        Assert.True(record.PdfGenerated);
        Assert.False(record.OpenedInOutlook);
        Assert.Equal(["British Gas", "EDF", "Valda Energy"], record.Suppliers.Select(s => s.SupplierName).Order());
        Assert.All(record.Suppliers, s => Assert.Equal(RecordId, s.ReportingRecordId));
    }

    [Fact]
    public void QuoteRequestedHasNoPricingRunYet()
    {
        var json = Payload(Build(ReportingEventType.QuoteRequested, withRun: false));
        Assert.Equal(JsonValueKind.Null, json.GetProperty("pricingRun").ValueKind);
        Assert.Empty(json.GetProperty("prices").EnumerateArray());
        Assert.False(json.GetProperty("quote").GetProperty("isMicrobusiness").GetBoolean());
    }

    [Fact]
    public void FailureCarriesTheStageAndCategory()
    {
        var failure = Payload(Build(ReportingEventType.Failed, failure: new ReportingFailure("Pricing", "Timeout", "POWWR took too long to answer.")))
            .GetProperty("failure");
        Assert.Equal("Pricing", failure.GetProperty("stage").GetString());
        Assert.Equal("Timeout", failure.GetProperty("category").GetString());
    }
}
