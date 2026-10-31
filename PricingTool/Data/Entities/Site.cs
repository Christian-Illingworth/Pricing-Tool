using PricingTool.Domain;

namespace PricingTool.Data.Entities;

// An address with exactly one meter (FR-071). All of a quote's sites match the quote's fuel.
public class Site
{
    public int Id { get; set; }

    public int QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;

    // Order shown, starting at 1.
    public int Ordinal { get; set; }

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? AddressLine3 { get; set; }
    public string? AddressLine4 { get; set; }
    public string? Town { get; set; }

    // Validated and normalised with a space (FR-008).
    public string Postcode { get; set; } = "";

    // Optional per-site override of the contract start date (FR-071).
    public DateOnly? StartDate { get; set; }

    public string? LookupMeterAddressId { get; set; }

    public int? CurrentSupplierId { get; set; }
    public Supplier? CurrentSupplier { get; set; }

    // As returned or typed, when it isn't mapped.
    public string? CurrentSupplierText { get; set; }

    // p/kWh and p/day, optional (FR-023).
    public decimal? CurrentDayRate { get; set; }
    public decimal? CurrentNightRate { get; set; }
    public decimal? CurrentWendRate { get; set; }
    public decimal? CurrentStandingCharge { get; set; }
    public DateOnly? CurrentContractEndDate { get; set; }

    public int? ConsumptionDayKwh { get; set; }
    public int? ConsumptionNightKwh { get; set; }
    public int? ConsumptionWendKwh { get; set; }
    public ConsumptionEntryMode ConsumptionEntryMode { get; set; }
    public int? ConsumptionTotalKwh { get; set; }
    public decimal? SplitDayPct { get; set; }
    public decimal? SplitNightPct { get; set; }
    public decimal? SplitWendPct { get; set; }

    public int? GasAnnualQuantityKwh { get; set; }

    public SiteMeter Meter { get; set; } = new();

    // JSON: [{ code, message, confirmedByAgent }] (FR-012, FR-045, FR-046).
    public List<SiteWarning> Warnings { get; set; } = [];

    // JSON: field → Lookup, Agent or AgentChanged (FR-011).
    public Dictionary<string, FieldSource> FieldSources { get; set; } = [];

    // JSON: the mapped lookup response, without secrets, for audit.
    public string? LookupSnapshot { get; set; }
}

// Owned by Site, stored in its table. Electricity fields are blank on a gas site and vice versa.
public class SiteMeter
{
    // ── Electricity ──
    public string? MpanTopLine { get; set; }
    public string? MpanCore { get; set; }
    public string? DistributorId { get; set; }
    public string? RegionName { get; set; }
    public string? GspId { get; set; }
    public int? ProfileClass { get; set; }
    public int? MeterTimeswitchClass { get; set; }
    public string? LineLossFactorClass { get; set; }
    public string? StandardSettlementConfig { get; set; }
    public string? DuosTariffId { get; set; }
    public bool? MhhsMigrated { get; set; }
    public string? MeasurementClass { get; set; }
    public string? SmetsVersion { get; set; }
    public bool IsHalfHourly { get; set; }
    public int? KvaCapacity { get; set; }
    public int? KvarhConsumption { get; set; }

    // For example "Day,Night", from /prompts or the forced structure (FR-020).
    public string RateRegisters { get; set; } = "";

    public ForcedRateStructure? ForcedRateStructure { get; set; }

    // ── Both ──
    public string? MeterType { get; set; }
    public bool SmartMeter { get; set; }

    // ── Gas ──
    public string? Mprn { get; set; }
    public string? Ldz { get; set; }
    public string? ExitZone { get; set; }
    public bool IsIgt { get; set; }
    public int? MeterCapacity { get; set; }
    public int? DailyMeteredQuantity { get; set; }
    public string? AmrIndicator { get; set; }
    public bool IsLargeGasSite { get; set; }

    public IReadOnlyList<Register> Registers =>
        RateRegisters.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                     .Select(Enum.Parse<Register>)
                     .ToList();
}
