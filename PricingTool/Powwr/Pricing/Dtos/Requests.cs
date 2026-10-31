using System.Text.Json.Serialization;

namespace PricingTool.Powwr.Pricing.Dtos;

// Request types, mirroring the definition's schemas one for one. Only PowwrPricingMapper and the HTTP
// client use them; nothing outside Powwr/Pricing sees a DTO.

// POST /electricprices
public sealed class GetElectricRates
{
    [JsonPropertyName("quoteDetails")] public QuoteDetails? QuoteDetails { get; set; }
}

// POST /gasprices
public sealed class GetGasRates
{
    [JsonPropertyName("quoteDetails")] public QuoteDetails? QuoteDetails { get; set; }
}

// POST /prompts
public sealed class GetNumberOfPrompts
{
    [JsonPropertyName("promptsSearch")] public PromptsSearch? PromptsSearch { get; set; }
}

// POST /pricechange
public sealed class GetLatestPriceChange
{
    [JsonPropertyName("priceChangeRequest")] public PriceChangeRequest? PriceChangeRequest { get; set; }
}

public sealed class SecurityDetails
{
    // SECRET. Masked as *** in every stored copy and log (research R4).
    public string? LicenceCode { get; set; }

    [JsonPropertyName("mascaradeuser")] public string? Mascaradeuser { get; set; }
}

public sealed class QuoteDetails
{
    public string? QuoteReference { get; set; }
    public SecurityDetails? SecurityDetails { get; set; }
    public GasSupply? GasSupply { get; set; }
    public ElectricSupply? ElectricSupply { get; set; }
    public string? PostCode { get; set; }
    public bool? Renewal { get; set; }
    public string? CurrentSupplier { get; set; }
    public bool? COT { get; set; }
    public string? CotDate { get; set; }
    public bool? OutOfContract { get; set; }
    public decimal? Uplift { get; set; }
    public decimal? StandingChargeUplift { get; set; }
    public string? PaymentMethod { get; set; }
    public List<QuoteDefinition>? QuoteDefinitions { get; set; }
    public List<Setting>? Settings { get; set; }
    public bool? SortByCommission { get; set; }
    public string? BusinessType { get; set; }
}

public sealed class ElectricSupply
{
    public Consumption? DayConsumption { get; set; }
    public Consumption? NightConsumption { get; set; }
    public Consumption? WendConsumption { get; set; }
    public Consumption? KvaConsumption { get; set; }
    public Consumption? KvarhConsumption { get; set; }
    public string? MPANTop { get; set; }
    public string? MPANBottom { get; set; }
    public string? MeasurementClass { get; set; }
    public string? MeterType { get; set; }
    public string? ContractRenewalDate { get; set; }
    public string? ContractEndDate { get; set; }
    public string? NewContractEndDate { get; set; }
    public string? CurrentSupplier { get; set; }
    public bool? SmartMeter { get; set; }
    public string? StandardSettlementConfig { get; set; }
    public bool? MhhsMigrated { get; set; }
    public string? DuosTariffId { get; set; }
    public int? ProfileClass { get; set; }
    public int? MeterTimeswitchClass { get; set; }
    public string? LineLossFactorClass { get; set; }
    public string? ForcedRateStructure { get; set; }
}

public sealed class GasSupply
{
    public Consumption? Consumption { get; set; }
    public string? MPR { get; set; }
    public string? ContractRenewalDate { get; set; }
    public string? ContractEndDate { get; set; }
    public string? NewContractEndDate { get; set; }
    public string? CurrentSupplier { get; set; }
    public bool? SmartMeter { get; set; }
}

public sealed class Consumption
{
    public int? Amount { get; set; }
}

public sealed class QuoteDefinition
{
    public string? Supplier { get; set; }
    public List<Plan>? Plans { get; set; }
}

public sealed class Plan
{
    public int? Duration { get; set; }
    public string? PlanType { get; set; }
    public decimal? Uplift { get; set; }
    public decimal? StandingChargeUplift { get; set; }
    public int? Id { get; set; }
}

public sealed class Setting
{
    [JsonPropertyName("key")] public string? Key { get; set; }
    [JsonPropertyName("value")] public string? Value { get; set; }
}

public sealed class PromptsSearch
{
    public string? MPANTop { get; set; }
    public string? MPANBottom { get; set; }
    public string? StandardSettlementConfig { get; set; }
    public bool? MhhsMigrated { get; set; }
    public string? DuosTariffId { get; set; }
    public int? ProfileClass { get; set; }
    public int? MeterTimeswitchClass { get; set; }
    public string? LineLossFactorClass { get; set; }
    public List<string>? Suppliers { get; set; }
    public SecurityDetails? SecurityDetails { get; set; }
}

public sealed class PriceChangeRequest
{
    public SecurityDetails? SecurityDetails { get; set; }
    public string? SupplierName { get; set; }
    public bool? IncludeFuture { get; set; }
}
