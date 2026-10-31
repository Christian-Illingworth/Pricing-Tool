using PricingTool.Domain;

namespace PricingTool.Powwr.Pricing;

// The domain side of the POWWR UK Pricing adapter (contracts/adapters.md). Nothing here is a DTO, an
// HTTP type or a secret.

// p/kWh and p/day.
public sealed record UpliftPair(decimal UnitRate, decimal StandingCharge);

// The uplift for one term of one supplier, already capped at the supplier's maximum (FR-062).
public sealed record PlanUplift(int TermMonths, decimal UnitRateUplift, decimal StandingChargeUplift);

// One QuoteDefinition: a supplier's pricing name and a plan for each requested term.
public sealed record SupplierPlan(string SupplierPricingName, IReadOnlyList<PlanUplift> Plans);

// The quote-level fields both fuels send.
public abstract record PriceRequest
{
    public required string QuoteReference { get; init; }

    // Sent as SecurityDetails.mascaradeuser (open question Q13).
    public required string AgentName { get; init; }

    public required string Postcode { get; init; }
    public required QuoteType QuoteType { get; init; }
    public DateOnly? CotDate { get; init; }
    public bool OutOfContract { get; init; }

    // Built by PaymentMethodText, for example "Monthly Direct Debit".
    public required string PaymentMethod { get; init; }

    // Turned into POWWR's string through Powwr:Pricing:BusinessTypes.
    public required BusinessType BusinessType { get; init; }

    public string? CurrentSupplierPricingName { get; init; }
    public required DateOnly StartDate { get; init; }
    public required IReadOnlyList<int> TermsMonths { get; init; }
    public required UpliftPair DefaultUplift { get; init; }
    public required IReadOnlyList<SupplierPlan> SupplierPlans { get; init; }
    public DateOnly? CurrentContractEndDate { get; init; }
    public bool SmartMeter { get; init; }
}

public sealed record ElectricPriceRequest : PriceRequest
{
    public string? MpanTopLine { get; init; }
    public required string MpanCore { get; init; }
    public int? ProfileClass { get; init; }
    public int? MeterTimeswitchClass { get; init; }
    public string? LineLossFactorClass { get; init; }
    public string? StandardSettlementConfig { get; init; }
    public string? DuosTariffId { get; init; }
    public bool? MhhsMigrated { get; init; }
    public string? MeasurementClass { get; init; }
    public string? MeterType { get; init; }
    public int? DayKwh { get; init; }
    public int? NightKwh { get; init; }
    public int? WendKwh { get; init; }
    public int? KvaCapacity { get; init; }
    public int? KvarhConsumption { get; init; }
    public ForcedRateStructure? ForcedRateStructure { get; init; }
}

public sealed record GasPriceRequest : PriceRequest
{
    public required string Mprn { get; init; }
    public required int AnnualQuantityKwh { get; init; }
}

// The result for one site. POWWR's own errors, timeouts, rate limits and bad data come back here with
// Succeeded = false and a category, rather than as exceptions, so a multi-site run can record each
// site's failure. Only programming errors throw.
public sealed record SitePriceResponse
{
    public required bool Succeeded { get; init; }
    public ErrorCategory? ErrorCategory { get; init; }

    // Safe to show the agent; never contains secrets or raw responses.
    public string? ErrorMessage { get; init; }

    public string? PowwrReference { get; init; }

    // Agent-only.
    public string? FooterNotes { get; init; }

    public IReadOnlyList<ReturnedRate> Rates { get; init; } = [];
    public IReadOnlyList<SupplierNoQuote> NoQuotes { get; init; } = [];

    // The request as sent, with LicenceCode masked as ***.
    public required string RequestJsonMasked { get; init; }

    public string? ResponseJson { get; init; }
    public required DateTime SentUtc { get; init; }
    public required DateTime ReceivedUtc { get; init; }
}

// Every Rate field: strings exactly as returned, Raw* exact. Term is parsed to whole months.
public sealed record ReturnedRate
{
    public required string Supplier { get; init; }
    public required int TermMonths { get; init; }
    public string? PlanType { get; init; }
    public string? StandingCharge { get; init; }
    public string? DayUnitrate { get; init; }
    public string? NightUnitrate { get; init; }
    public string? WendUnitrate { get; init; }
    public string? Fits { get; init; }
    public string? Stod1 { get; init; }
    public string? Stod2 { get; init; }
    public string? Stod3 { get; init; }
    public string? Stod4 { get; init; }
    public string? Stod5 { get; init; }
    public string? Stod6 { get; init; }
    public string? DirectDebitDiscount { get; init; }
    public decimal? FlatDirectDebitDiscount { get; init; }
    public string? ChequeSurcharge { get; init; }
    public string? ExtraInfo { get; init; }
    public string? FooterMessage { get; init; }
    public string? TariffCode { get; init; }
    public string? AnnualPrice { get; init; }
    public required decimal RawAnnualPrice { get; init; }
    public string? BaseAnnualPrice { get; init; }
    public decimal? RawBaseAnnualPrice { get; init; }
    public string? AnnualPriceInclusive { get; init; }
    public decimal? RawAnnualPriceInclusive { get; init; }
    public string? Ref { get; init; }
    public string? Period { get; init; }
    public string? SC { get; init; }
    public string? CapacityCharge { get; init; }
    public string? ReactiveRate { get; init; }
    public string? ExcessCapacityCharge { get; init; }
    public string? Uplift { get; init; }
    public string? FixedFee { get; init; }
    public string? FixedFeeUplift { get; init; }
    public string? FixedFeeSCUplift { get; init; }
    public string? StandingChargeUplift { get; init; }
    public bool? Renewal { get; init; }
    public decimal? RawCommission { get; init; }
    public string? Commission { get; init; }
    public bool OutsidePricingWindow { get; init; }
    public string? ContractEndDate { get; init; }
    public string? TransportationCharge { get; init; }
    public string? PricebookVersion { get; init; }
    public string? QuoteRateReference { get; init; }
    public int? Id { get; init; }
}

// A supplier that didn't price: POWWR's NoQuoteReason exactly (FR-036), or a rate we couldn't trust,
// marked IsInvalidResponse ("couldn't be priced: invalid response", research R4).
public sealed record SupplierNoQuote(string SupplierName, int? TermMonths, string Reason, bool IsInvalidResponse = false);

// /prompts: the rate structure for an electricity meter (FR-020, research R16).
public sealed record RateStructureRequest
{
    public string? MpanTopLine { get; init; }
    public required string MpanCore { get; init; }
    public int? ProfileClass { get; init; }
    public int? MeterTimeswitchClass { get; init; }
    public string? LineLossFactorClass { get; init; }
    public string? StandardSettlementConfig { get; init; }
    public string? DuosTariffId { get; init; }
    public bool? MhhsMigrated { get; init; }
}

public sealed record RateStructureResponse(
    bool Succeeded,
    IReadOnlyList<Register> Registers,
    IReadOnlyList<string> RawPrompts,
    string? ErrorMessage,
    IReadOnlyList<string> UnknownPrompts);

// /pricechange: a supplier's latest pricebook for a utility, term and half-hourly status (R14).
public sealed record SupplierPriceChange(
    string Supplier,
    string Utility,
    bool HalfHourly,
    int DurationMonths,
    string? Product,
    string? PlanType,
    string? Version,
    DateOnly? LastPriceChange);
