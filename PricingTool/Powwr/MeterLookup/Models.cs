using PricingTool.Domain;

namespace PricingTool.Powwr.MeterLookup;

// The domain side of the POWWR Meter Lookup adapter (contracts/adapters.md). The lookup never blocks
// quoting (SC-005): every outcome other than Found leaves the agent to enter the details themselves.
public enum LookupStatus { Found, NotFound, Unavailable, Rejected }

public sealed record LookupResult<T>(LookupStatus Status, T? Value, string? ProblemSummary)
{
    public static LookupResult<T> Found(T value) => new(LookupStatus.Found, value, null);
    public static LookupResult<T> NotFound(string summary) => new(LookupStatus.NotFound, default, summary);
    public static LookupResult<T> Unavailable(string summary) => new(LookupStatus.Unavailable, default, summary);
    public static LookupResult<T> Rejected(string summary) => new(LookupStatus.Rejected, default, summary);
}

// An address from a postcode search. The numbers are hints only: POWWR may redact them to the last
// three digits, so the full number always comes from a meter search (FR-010).
public sealed record LookupAddress(
    string MeterAddressId,
    string FullAddress,
    int ElectricMeterCount,
    int GasMeterCount,
    IReadOnlyList<string> RedactedNumbers)
{
    public int MeterCountFor(Fuel fuel) => fuel == Fuel.Electricity ? ElectricMeterCount : GasMeterCount;
}

public sealed record LookupMeters(IReadOnlyList<LookupElectricMeter> Electric, IReadOnlyList<LookupGasMeter> Gas);

// A meter's site address, as best the lookup's lines allow. The agent can change any of it (FR-011).
public sealed record LookupSiteAddress(string? Line1, string? Line2, string? Line3, string? Line4, string? Town, string? Postcode, string? FullAddress);

// The current (or incoming) supplier: the admin supplier it matched, and the name as returned.
public sealed record LookupSupplier(int? SupplierId, string? PricingName, string? NameAsReturned);

public sealed record LookupElectricMeter
{
    public required string MpanCore { get; init; }

    // False when POWWR returned a number that fails our check; it isn't prefilled then.
    public required bool IsMpanValid { get; init; }

    public int? ProfileClass { get; init; }
    public int? MeterTimeswitchClass { get; init; }
    public string? LineLossFactorClass { get; init; }
    public string? StandardSettlementConfig { get; init; }
    public string? MeasurementClass { get; init; }
    public string? MeterType { get; init; }
    public string? SmetsVersion { get; init; }
    public bool SmartMeterSuggested { get; init; }
    public string? GspId { get; init; }
    public string? EnergisationStatus { get; init; }

    // Stored only; its meaning is unknown (open item P-16).
    public string? TradingStatus { get; init; }

    public int? AnnualConsumptionKwh { get; init; }

    // PC:00 + MTC:000 + LLFC when all three are present, marked as derived (the lookup has no top line).
    public string? DerivedTopLine { get; init; }

    public LookupSiteAddress? Address { get; init; }
    public LookupSupplier? CurrentSupplier { get; init; }
    public IReadOnlyList<SiteWarning> Warnings { get; init; } = [];

    // Values the lookup returned that couldn't be used, said in words for the agent.
    public IReadOnlyList<string> FieldNotes { get; init; } = [];
}

public sealed record LookupGasMeter
{
    public required string Mprn { get; init; }
    public required bool IsMprnValid { get; init; }
    public string? Ldz { get; init; }
    public string? ExitZone { get; init; }
    public bool IsIgt { get; init; }
    public int? MeterCapacity { get; init; }
    public int? DailyMeteredQuantity { get; init; }
    public int? AnnualQuantityKwh { get; init; }
    public string? AmrIndicator { get; init; }
    public bool SmartMeterSuggested { get; init; }
    public string? MeterType { get; init; }
    public string? MeterModel { get; init; }
    public string? MeterMechanismCode { get; init; }
    public LookupSiteAddress? Address { get; init; }
    public LookupSupplier? CurrentSupplier { get; init; }
    public LookupSupplier? IncomingSupplier { get; init; }
    public IReadOnlyList<SiteWarning> Warnings { get; init; } = [];
    public IReadOnlyList<string> FieldNotes { get; init; } = [];
}

// Exactly one of the three. Numbers are validated before they can be searched for (research R15).
public sealed record MeterSearch
{
    public string? MeterAddressId { get; private init; }
    public string? Mpan { get; private init; }
    public string? Mprn { get; private init; }

    public static MeterSearch ByAddress(string meterAddressId) => new() { MeterAddressId = meterAddressId };
    public static MeterSearch ByMpan(MpanCore core) => new() { Mpan = core.Value };
    public static MeterSearch ByMprn(Mprn mprn) => new() { Mprn = mprn.Value };
}
