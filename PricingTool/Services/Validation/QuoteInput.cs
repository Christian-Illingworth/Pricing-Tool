using PricingTool.Domain;

namespace PricingTool.Services.Validation;

// What the agent entered, as the form posts it and as an upload row becomes. Everything is nullable
// where the agent might leave it blank, so the validator, not model binding, decides what's missing.
// Property names are the field paths in QuoteInputValidator's errors.
public sealed class QuoteInput
{
    public Fuel Fuel { get; set; }
    public QuoteType? QuoteType { get; set; }
    public DateOnly? CotDate { get; set; }
    public bool OutOfContract { get; set; }
    public CustomerInput Customer { get; set; } = new();
    public List<SiteInput> Sites { get; set; } = [];
    public ContractInput Contract { get; set; } = new();
    public VatCclTreatment VatCclTreatment { get; set; } = VatCclTreatment.Standard;
    public decimal? QualifyingPercent { get; set; }
    public bool IncludeVatCcl { get; set; }
}

public sealed class CustomerInput
{
    public string? BusinessName { get; set; }
    public BusinessType? BusinessType { get; set; }
    public string? CompanyNumber { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }

    // The microbusiness declaration: the customer's two yes/no answers (FR-027).
    public bool DeclaredFewerThan10Employees { get; set; }
    public bool DeclaredTurnoverOrBalanceUnder2m { get; set; }
}

public sealed class SiteInput
{
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? AddressLine3 { get; set; }
    public string? AddressLine4 { get; set; }
    public string? Town { get; set; }
    public string? Postcode { get; set; }

    // Optional per-site override of the contract start date (FR-071).
    public DateOnly? StartDate { get; set; }

    public string? LookupMeterAddressId { get; set; }

    // Current supply (FR-023): all optional.
    public int? CurrentSupplierId { get; set; }
    public string? CurrentSupplierText { get; set; }
    public decimal? CurrentDayRate { get; set; }
    public decimal? CurrentNightRate { get; set; }
    public decimal? CurrentWendRate { get; set; }
    public decimal? CurrentStandingCharge { get; set; }
    public DateOnly? CurrentContractEndDate { get; set; }

    // Electricity consumption (FR-022): per register, or a total plus a percentage split.
    public ConsumptionEntryMode ConsumptionEntryMode { get; set; } = ConsumptionEntryMode.PerRegister;
    public int? ConsumptionDayKwh { get; set; }
    public int? ConsumptionNightKwh { get; set; }
    public int? ConsumptionWendKwh { get; set; }
    public int? ConsumptionTotalKwh { get; set; }
    public decimal? SplitDayPct { get; set; }
    public decimal? SplitNightPct { get; set; }
    public decimal? SplitWendPct { get; set; }

    // Gas annual quantity (FR-043).
    public int? GasAnnualQuantityKwh { get; set; }

    public MeterInput Meter { get; set; } = new();

    // Warnings from the lookup, with the agent's confirmation of a domestic meter (FR-012).
    public List<SiteWarning> Warnings { get; set; } = [];
}

public sealed class MeterInput
{
    // ── Electricity (FR-015 to FR-021) ──
    public string? MpanTopLine { get; set; }
    public string? MpanCore { get; set; }
    public int? ProfileClass { get; set; }
    public int? MeterTimeswitchClass { get; set; }
    public string? LineLossFactorClass { get; set; }
    public string? StandardSettlementConfig { get; set; }
    public string? DuosTariffId { get; set; }
    public bool? MhhsMigrated { get; set; }
    public string? MeasurementClass { get; set; }
    public string? MeterType { get; set; }
    public string? SmetsVersion { get; set; }
    public string? GspId { get; set; }
    public int? KvaCapacity { get; set; }
    public int? KvarhConsumption { get; set; }
    public List<Register> RateRegisters { get; set; } = [Register.Day];
    public ForcedRateStructure? ForcedRateStructure { get; set; }

    // ── Both ──
    public bool SmartMeter { get; set; }

    // ── Gas (FR-043 to FR-046) ──
    public string? Mprn { get; set; }
    public string? Ldz { get; set; }
    public string? ExitZone { get; set; }
    public bool IsIgt { get; set; }
    public int? MeterCapacity { get; set; }
    public int? DailyMeteredQuantity { get; set; }
    public string? AmrIndicator { get; set; }
}

public sealed class ContractInput
{
    public DateOnly? StartDate { get; set; }
    public List<int> TermsMonths { get; set; } = [];
    public PaymentMethod? PaymentMethod { get; set; }
    public PaymentFrequency? PaymentFrequency { get; set; }
    public PaymentAmount? PaymentAmount { get; set; }
    public SupplierFilterMode SupplierFilterMode { get; set; } = SupplierFilterMode.All;
    public List<int> SupplierFilterIds { get; set; } = [];
    public bool NoStandingChargeOnly { get; set; }

    // p/kWh and p/day, prefilled from the fuel's defaults (FR-025).
    public decimal? DefaultUnitRateUplift { get; set; }
    public decimal? DefaultStandingChargeUplift { get; set; }
}
