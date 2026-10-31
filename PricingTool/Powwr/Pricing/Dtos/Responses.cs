namespace PricingTool.Powwr.Pricing.Dtos;

// Response types, mirroring the definition's schemas one for one.

public sealed class GetElectricRatesResultWrapper
{
    public QuoteResult? GetElectricRatesResult { get; set; }
}

public sealed class GetGasRatesResultWrapper
{
    public QuoteResult? GetGasRatesResult { get; set; }
}

public sealed class GetNumberOfPromptsResultWrapper
{
    public Prompts? GetNumberOfPromptsResult { get; set; }
}

public sealed class GetLatestPriceChangeResultWrapper
{
    public PriceChangeResult? GetLatestPriceChangeResult { get; set; }
}

public sealed class PingResultWrapper
{
    public string? PingResult { get; set; }
}

public sealed class QuoteResult
{
    public List<Rate>? Rates { get; set; }
    public string? Reference { get; set; }
    public string? FooterNotes { get; set; }
    public Error? Error { get; set; }
}

public sealed class Error
{
    public string? Message { get; set; }
    public string? ErrorDetail { get; set; }
}

// One supplier product and term. Strings are kept exactly as returned; Raw* are exact decimals.
public sealed class Rate
{
    public string? Supplier { get; set; }
    public string? Term { get; set; }
    public string? PlanType { get; set; }
    public string? StandingCharge { get; set; }
    public string? DayUnitrate { get; set; }
    public string? NightUnitrate { get; set; }
    public string? WendUnitrate { get; set; }
    public string? Fits { get; set; }
    public string? Stod1 { get; set; }
    public string? Stod2 { get; set; }
    public string? Stod3 { get; set; }
    public string? Stod4 { get; set; }
    public string? Stod5 { get; set; }
    public string? Stod6 { get; set; }
    public string? DirectDebitDiscount { get; set; }
    public decimal? FlatDirectDebitDiscount { get; set; }
    public string? ChequeSurcharge { get; set; }
    public string? ExtraInfo { get; set; }
    public string? FooterMessage { get; set; }
    public string? TariffCode { get; set; }
    public string? AnnualPrice { get; set; }
    public decimal? RawAnnualPrice { get; set; }
    public string? BaseAnnualPrice { get; set; }
    public decimal? RawBaseAnnualPrice { get; set; }
    public string? AnnualPriceInclusive { get; set; }
    public decimal? RawAnnualPriceInclusive { get; set; }
    public string? Ref { get; set; }
    public string? Period { get; set; }
    public string? SC { get; set; }
    public string? CapacityCharge { get; set; }
    public string? ReactiveRate { get; set; }
    public string? ExcessCapacityCharge { get; set; }
    public string? Uplift { get; set; }
    public string? FixedFee { get; set; }
    public string? FixedFeeUplift { get; set; }
    public string? FixedFeeSCUplift { get; set; }
    public string? StandingChargeUplift { get; set; }
    public bool? Renewal { get; set; }
    public decimal? RawCommission { get; set; }
    public string? Commission { get; set; }
    public bool? OutsidePricingWindow { get; set; }
    public string? NoQuoteReason { get; set; }
    public string? ContractEndDate { get; set; }
    public string? TransportationCharge { get; set; }
    public string? PricebookVersion { get; set; }
    public string? QuoteRateReference { get; set; }
    public int? Id { get; set; }
}

public sealed class Prompts
{
    public int? NumberOfPrompts { get; set; }
    public List<string>? ThePrompts { get; set; }
    public Error? Error { get; set; }
}

public sealed class PriceChangeResult
{
    public List<LatestPriceChange>? PriceChanges { get; set; }
}

public sealed class LatestPriceChange
{
    public string? Supplier { get; set; }
    public string? Utility { get; set; }
    public bool? HalfHourly { get; set; }
    public int? Duration { get; set; }
    public string? Product { get; set; }
    public string? PlanType { get; set; }
    public string? Description { get; set; }
    public string? Version { get; set; }
    public string? LastPriceChange { get; set; }
}
