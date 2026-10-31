using System.Globalization;
using System.Text.Json.Nodes;
using PricingTool.Data.Entities;
using PricingTool.Domain;
using PricingTool.Powwr.Pricing;
using PricingTool.Services.Pricing;

namespace PricingTool.Services.Reporting;

// One quote event to record (FR-084). The quote is loaded with its sites (and their meters) and its
// uplift overrides (with suppliers); the run, when there is one, with its sites, no-quotes and options.
public sealed record ReportingEvent(
    ReportingEventType EventType,
    Quote Quote,
    StaffMember Agent,
    PricingRun? Run,
    ReportingPdf? Pdf = null,
    DraftMethod? OutlookMethod = null,
    ReportingFailure? Failure = null);

public sealed record ReportingPdf(int PdfId, string FileName);

// Stage is Pricing, Pdf or Outlook. Message is the safe message the agent saw, never a raw response.
public sealed record ReportingFailure(string Stage, string Category, string Message);

// Builds the self-contained reporting record, schema version 1 (contracts/reporting-record.md and
// reporting-record.v1.schema.json). It only copies and formats stored figures: money is the engine's
// full-precision value plus Rounding.ToPennies for display, so reports match the screen and PDF
// (SC-004). It never includes the customer's contact details, secrets, POWWR's raw request or
// response, or supplier notes.
public static class ReportingRecordBuilder
{
    public const int SchemaVersion = 1;

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static ReportingRecord Build(ReportingEvent e, Guid recordId, DateTime occurredUtc)
    {
        var quote = e.Quote;
        var run = e.Run;
        var ordinals = quote.Sites.ToDictionary(s => s.Id, s => s.Ordinal);
        var runSiteOrdinals = (run?.Sites ?? []).ToDictionary(s => s.Id, s => ordinals.GetValueOrDefault(s.SiteId));
        var treatments = (run?.Sites ?? []).GroupBy(s => s.SiteId).ToDictionary(g => g.Key, g => g.OrderBy(s => s.Attempt).Last().Treatment);
        var recommended = run?.Options.FirstOrDefault(o => o.IsRecommended);

        var payload = new JsonObject
        {
            ["schemaVersion"] = SchemaVersion,
            ["recordId"] = recordId.ToString(),
            ["eventType"] = e.EventType.ToString(),
            ["occurredUtc"] = Instant(occurredUtc),
            ["agent"] = new JsonObject { ["id"] = e.Agent.Id, ["name"] = e.Agent.DisplayName, ["email"] = e.Agent.Email },
            ["quote"] = new JsonObject
            {
                ["reference"] = quote.Reference,
                ["powwrReferences"] = Array((run?.Sites ?? []).Select(s => s.PowwrReference).OfType<string>().Distinct()),
                ["quoteType"] = quote.QuoteType.ToString(),
                ["outOfContract"] = quote.OutOfContract,
                ["cotDate"] = Date(quote.CotDate),
                ["fuel"] = quote.Fuel.ToString(),
                ["isMicrobusiness"] = run?.IsMicrobusiness ?? false,
                ["microbusinessSource"] = run?.MicrobusinessSource.ToString(),
            },
            ["customer"] = new JsonObject
            {
                ["businessName"] = quote.Customer.BusinessName,
                ["businessType"] = quote.Customer.BusinessType.ToString(),
                ["companyNumber"] = quote.Customer.CompanyNumber,
                ["postcode"] = quote.Sites.OrderBy(s => s.Ordinal).FirstOrDefault()?.Postcode ?? "",
            },
            ["contract"] = Contract(quote),
            ["sites"] = Array(quote.Sites.OrderBy(s => s.Ordinal).Select(site => Site(quote, site,
                treatments.TryGetValue(site.Id, out var t) ? t : DeclaredTreatment(quote)))),
            ["pricingRun"] = run is null ? null : new JsonObject
            {
                ["runNumber"] = run.RunNumber,
                ["fetchedUtc"] = run.FetchedUtc is { } fetched ? Instant(fetched) : null,
                ["status"] = run.Status.ToString(),
                ["commissionBasis"] = run.CommissionBasis.ToString(),
            },
            ["prices"] = Array((run?.Options ?? []).SelectMany(option => option.Sites.Select(site => Price(option, site, runSiteOrdinals)))),
            ["noQuotes"] = Array((run?.Sites ?? []).SelectMany(site => site.NoQuotes.Select(n => (JsonNode)new JsonObject
            {
                ["siteOrdinal"] = runSiteOrdinals.GetValueOrDefault(site.Id),
                ["supplier"] = n.SupplierName,
                ["termMonths"] = n.TermMonths,
                ["reason"] = n.Reason,
            }))),
            ["options"] = Array((run?.Options ?? []).Select(o => (JsonNode)new JsonObject
            {
                ["supplier"] = o.SupplierName,
                ["planType"] = o.PlanType,
                ["termMonths"] = o.TermMonths,
                ["isPartial"] = o.IsPartial,
                ["selected"] = o.IsSelected,
                ["recommended"] = o.IsRecommended,
                ["annualCostExVatCcl"] = Money(o.TotalAnnualCostExVatCcl),
                ["commissionContractTotal"] = Money(o.CommissionContractTotal),
                ["cappedAtSupplierMax"] = o.IsCappedAtSupplierMax,
            })),
            ["pdf"] = new JsonObject { ["generated"] = e.Pdf is not null, ["pdfId"] = e.Pdf?.PdfId, ["fileName"] = e.Pdf?.FileName },
            ["outlook"] = new JsonObject { ["opened"] = e.OutlookMethod is not null, ["method"] = e.OutlookMethod?.ToString() },
            ["failure"] = e.Failure is null ? null : new JsonObject
            {
                ["stage"] = e.Failure.Stage,
                ["category"] = e.Failure.Category,
                ["message"] = e.Failure.Message,
            },
        };

        var regions = quote.Sites.OrderBy(s => s.Ordinal)
            .Select(s => quote.Fuel == Fuel.Electricity ? s.Meter.RegionName : s.Meter.Ldz)
            .OfType<string>().Distinct();
        var suppliers = (run?.Options.Select(o => o.SupplierName) ?? [])
            .Concat((run?.Sites ?? []).SelectMany(s => s.NoQuotes.Select(n => n.SupplierName)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => new ReportingRecordSupplier { ReportingRecordId = recordId, SupplierName = Fit(name, 100) })
            .ToList();

        return new ReportingRecord
        {
            Id = recordId,
            SchemaVersion = SchemaVersion,
            EventType = e.EventType,
            OccurredUtc = occurredUtc,
            QuoteId = quote.Id,
            QuoteReference = quote.Reference,
            AgentId = e.Agent.Id,
            AgentName = Fit(e.Agent.DisplayName, 200),
            AgentEmail = Fit(e.Agent.Email, 320),
            Fuel = quote.Fuel,
            QuoteType = quote.QuoteType,
            Regions = Fit(string.Join(",", regions), 200),
            RecommendedCommissionTotal = recommended?.CommissionContractTotal,
            PdfGenerated = e.Pdf is not null,
            OpenedInOutlook = e.OutlookMethod is not null,
            Payload = payload.ToJsonString(),
            Suppliers = suppliers,
        };
    }

    private static JsonObject Contract(Quote quote)
    {
        var contract = quote.Contract;
        return new JsonObject
        {
            ["startDate"] = Date(contract.StartDate),
            ["termsMonths"] = Array(contract.Terms.Select(t => (JsonNode)t)),
            ["paymentMethod"] = PaymentMethodText.For(contract.PaymentMethod, contract.PaymentFrequency, contract.PaymentAmount),
            ["defaultUplift"] = Uplift(contract.DefaultUnitRateUplift, contract.DefaultStandingChargeUplift),
            ["upliftOverrides"] = Array(quote.UpliftOverrides.OrderBy(o => o.Supplier?.PricingName).ThenBy(o => o.TermMonths).Select(o =>
            {
                var node = Uplift(o.UnitRateUplift, o.StandingChargeUplift);
                node["supplier"] = o.Supplier?.PricingName ?? o.SupplierId.ToString(Invariant);
                node["termMonths"] = o.TermMonths;
                return (JsonNode)node;
            })),
            ["noStandingChargeOnly"] = contract.NoStandingChargeOnly,
            ["vatCclTreatment"] = quote.VatCclTreatment.ToString(),
            ["qualifyingPercent"] = quote.QualifyingPercent,
            ["includeVatCcl"] = quote.IncludeVatCcl,
        };
    }

    private static JsonNode Site(Quote quote, Site site, SiteTreatment treatment)
    {
        var meter = site.Meter;
        var isElectricity = quote.Fuel == Fuel.Electricity;
        var registers = new[] { site.ConsumptionDayKwh, site.ConsumptionNightKwh, site.ConsumptionWendKwh };
        var total = isElectricity
            ? (registers.Any(r => r is > 0) ? registers.Sum(r => r ?? 0) : site.ConsumptionTotalKwh ?? 0)
            : site.GasAnnualQuantityKwh ?? 0;
        var hasCurrentRates = site.CurrentDayRate is not null || site.CurrentNightRate is not null
            || site.CurrentWendRate is not null || site.CurrentStandingCharge is not null;

        return new JsonObject
        {
            ["ordinal"] = site.Ordinal,
            ["postcode"] = site.Postcode,
            ["startDate"] = Date(site.StartDate),
            ["electricity"] = !isElectricity ? null : new JsonObject
            {
                ["mpanTopLine"] = meter.MpanTopLine,
                ["mpanCore"] = meter.MpanCore ?? "",
                ["profileClass"] = meter.ProfileClass,
                ["meterTimeswitchClass"] = meter.MeterTimeswitchClass,
                ["lineLossFactorClass"] = meter.LineLossFactorClass,
                ["standardSettlementConfig"] = meter.StandardSettlementConfig,
                ["duosTariffId"] = meter.DuosTariffId,
                ["mhhsMigrated"] = meter.MhhsMigrated,
                ["distributorId"] = meter.DistributorId ?? "",
                ["region"] = meter.RegionName ?? "",
                ["isHalfHourly"] = meter.IsHalfHourly,
                ["kvaCapacity"] = meter.KvaCapacity,
                ["smartMeter"] = meter.SmartMeter,
            },
            ["gas"] = isElectricity ? null : new JsonObject
            {
                ["mprn"] = meter.Mprn ?? "",
                ["ldz"] = meter.Ldz,
                ["smartMeter"] = meter.SmartMeter,
                ["isIgt"] = meter.IsIgt,
            },
            ["consumption"] = new JsonObject
            {
                ["dayKwh"] = isElectricity ? site.ConsumptionDayKwh : null,
                ["nightKwh"] = isElectricity ? site.ConsumptionNightKwh : null,
                ["eveningWeekendKwh"] = isElectricity ? site.ConsumptionWendKwh : null,
                ["annualQuantityKwh"] = isElectricity ? null : site.GasAnnualQuantityKwh,
                ["totalKwh"] = total,
            },
            ["currentSupplier"] = site.CurrentSupplier?.PricingName ?? site.CurrentSupplierText,
            ["currentRates"] = !hasCurrentRates ? null : new JsonObject
            {
                ["dayPencePerKwh"] = isElectricity ? site.CurrentDayRate : null,
                ["nightPencePerKwh"] = site.CurrentNightRate,
                ["eveningWeekendPencePerKwh"] = site.CurrentWendRate,
                ["unitRatePencePerKwh"] = isElectricity ? null : site.CurrentDayRate,
                ["standingChargePencePerDay"] = site.CurrentStandingCharge,
            },
            ["treatment"] = treatment.ToString(),
        };
    }

    private static JsonNode Price(PriceOption option, PriceOptionSite site, IReadOnlyDictionary<int, int> runSiteOrdinals)
    {
        var returned = new JsonObject();
        foreach (var (name, value) in ReturnedStrings(site)) if (value is not null) returned[name] = value;

        var baseRates = new JsonObject();
        AddBase(baseRates, "dayUnitrate", site.BaseDayRate, site.DayUnitrate);
        AddBase(baseRates, "nightUnitrate", site.BaseNightRate, site.NightUnitrate);
        AddBase(baseRates, "wendUnitrate", site.BaseWendRate, site.WendUnitrate);
        AddBase(baseRates, "standingCharge", site.BaseStandingCharge, site.StandingCharge);

        return new JsonObject
        {
            ["siteOrdinal"] = runSiteOrdinals.GetValueOrDefault(site.PricingRunSiteId),
            ["supplier"] = option.SupplierName,
            ["planType"] = option.PlanType,
            ["termMonths"] = option.TermMonths,
            ["returned"] = returned,
            ["base"] = baseRates,
            ["annualCostExVatCcl"] = Money(site.AnnualCostExVatCcl),
            ["ccl"] = Money(site.Ccl),
            ["vat"] = Money(site.Vat),
            ["annualCostIncVatCcl"] = Money(site.AnnualCostIncVatCcl),
            ["savingExVatCcl"] = site.SavingExVatCcl is { } savingEx ? Money(savingEx) : null,
            ["savingIncVatCcl"] = site.SavingIncVatCcl is { } savingInc ? Money(savingInc) : null,
            ["commission"] = new JsonObject
            {
                ["powwr"] = Money(site.RawCommission ?? 0m),
                ["contractTotal"] = Money(site.CommissionContractTotal),
                ["upfront"] = Money(site.CommissionUpfront),
                ["monthly"] = Money(site.CommissionMonthly),
            },
            ["outsidePricingWindow"] = site.OutsidePricingWindow,
            ["cappedAtSupplierMax"] = option.IsCappedAtSupplierMax,
            ["pricebookVersion"] = site.PricebookVersion,
        };
    }

    private static IEnumerable<(string Name, string? Value)> ReturnedStrings(PriceOptionSite s) =>
    [
        ("dayUnitrate", s.DayUnitrate), ("nightUnitrate", s.NightUnitrate), ("wendUnitrate", s.WendUnitrate),
        ("standingCharge", s.StandingCharge), ("stod1", s.Stod1), ("stod2", s.Stod2), ("stod3", s.Stod3),
        ("stod4", s.Stod4), ("stod5", s.Stod5), ("stod6", s.Stod6), ("capacityCharge", s.CapacityCharge),
        ("excessCapacityCharge", s.ExcessCapacityCharge), ("reactiveRate", s.ReactiveRate), ("fixedFee", s.FixedFee),
        ("directDebitDiscount", s.DirectDebitDiscount), ("chequeSurcharge", s.ChequeSurcharge),
        ("transportationCharge", s.TransportationCharge), ("uplift", s.Uplift), ("standingChargeUplift", s.StandingChargeUplift),
        ("annualPrice", s.AnnualPrice), ("tariffCode", s.TariffCode), ("contractEndDate", s.ContractEndDate),
    ];

    private static void AddBase(JsonObject node, string name, decimal? baseRate, string? returned)
    {
        if (baseRate is { } value && returned is not null) node[name] = BaseRates.Format(value, returned);
    }

    // The engine's full-precision figure and the pennies every display shows (SC-004).
    private static JsonObject Money(decimal value) => new() { ["value"] = value, ["display"] = Rounding.ToPennies(value) };

    private static JsonObject Uplift(decimal unitRate, decimal standingCharge) =>
        new() { ["unitRatePencePerKwh"] = unitRate, ["standingChargePencePerDay"] = standingCharge };

    private static SiteTreatment DeclaredTreatment(Quote quote) =>
        quote.VatCclTreatment == VatCclTreatment.Qualifying ? SiteTreatment.Qualifying : SiteTreatment.Standard;

    private static JsonArray Array(IEnumerable<JsonNode?> nodes) => new([.. nodes]);

    private static JsonArray Array(IEnumerable<string> values) => new([.. values.Select(v => (JsonNode)v)]);

    private static string Instant(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", Invariant);

    private static string? Date(DateOnly? date) => date?.ToString("yyyy-MM-dd", Invariant);

    private static string Fit(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];
}
