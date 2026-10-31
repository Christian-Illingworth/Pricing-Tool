using System.Text.Json;
using PricingTool.Data.Entities;
using PricingTool.Domain;
using PricingTool.Services.Settings;

namespace PricingTool.Data.Seed;

// The starting data the first migration puts in the database (research R19). Every value here is a
// starting point TO CONFIRM with the business before go-live: uplift maximums and defaults (sales
// management), CCL rates (finance, F-1), payment models (finance, F-2) and all wording (compliance,
// C-1). Supplier aliases are made up. Admins change all of it later through Settings (US8), which
// adds new versions rather than editing these rows.
public static class SeedData
{
    private static readonly DateTime SeededUtc = new(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc);

    // Rates that have applied for years are given a date well before any quote can start.
    private static readonly DateOnly LongStanding = new(2020, 1, 1);

    // Starting maximum uplifts, the same for every supplier and both fuels' settings. TO CONFIRM.
    // A blank maximum allows no uplift at all (FR-060), so each one must have a value.
    private const decimal ElectricityMaxUnitRate = 3.0m;          // p/kWh
    private const decimal ElectricityMaxStandingCharge = 25m;     // p/day
    private const decimal GasMaxUnitRate = 2.0m;                  // p/kWh
    private const decimal GasMaxStandingCharge = 25m;             // p/day

    public static readonly Supplier[] Suppliers =
    [
        Supplier(1, "British Gas"),
        Supplier(2, "EDF"),
        Supplier(3, "E.ON Next"),
        Supplier(4, "Octopus Energy"),
        Supplier(5, "Opus Energy"),
        Supplier(6, "Scottish Power"),
        Supplier(7, "Smartest Energy"),
        Supplier(8, "Valda Energy"),
        Supplier(9, "Crown Gas & Power"),
        Supplier(10, "Yu Energy"),
    ];

    // Made-up lookup identifiers, apart from the shapes POWWR's own examples use (research R5).
    public static readonly SupplierAlias[] SupplierAliases =
    [
        new() { Id = 1, SupplierId = 1, PowwrId = "BRITISHG-001-S", Mpid = "ENRD", LookupName = "British Gas Business" },
        new() { Id = 2, SupplierId = 2, PowwrId = "EDFENERG-001-S", LookupName = "EDF Energy" },
        new() { Id = 3, SupplierId = 3, PowwrId = "EONNEXT-001-S", LookupName = "E.On Next" },
        new() { Id = 4, SupplierId = 4, PowwrId = "OCTOPUSE-001-S", LookupName = "Octopus Energy" },
        new() { Id = 5, SupplierId = 5, PowwrId = "OPUSENER-001-S", LookupName = "Opus Energy" },
        new() { Id = 6, SupplierId = 6, PowwrId = "SCOTTISH-001-S", Mpid = "SCT", LookupName = "Scottish Power" },
        new() { Id = 7, SupplierId = 7, PowwrId = "SMARTEST-001-S", LookupName = "Smartest Energy" },
        new() { Id = 8, SupplierId = 8, PowwrId = "VALDAENE-001-S", LookupName = "Valda Energy" },
        new() { Id = 9, SupplierId = 9, PowwrId = "CROWNGAS-001-S", LookupName = "Crown Gas & Power" },
        new() { Id = 10, SupplierId = 10, PowwrId = "YUENERGY-001-S", LookupName = "Yu Energy" },
    ];

    public static readonly SettingVersion[] SettingVersions = BuildSettings();

    public static readonly EffectiveRate[] EffectiveRates =
    [
        Rate(1, EffectiveRateKind.VatStandard, 20m, LongStanding),
        Rate(2, EffectiveRateKind.VatReduced, 5m, LongStanding),
        // HMRC main rates in p/kWh. TO CONFIRM with finance (F-1), who may add a row for the current year.
        Rate(3, EffectiveRateKind.CclElectricity, 0.801m, new DateOnly(2025, 4, 1)),
        Rate(4, EffectiveRateKind.CclGas, 0.801m, new DateOnly(2025, 4, 1)),
        Rate(5, EffectiveRateKind.LowUseElectricityKwhPerDay, 33m, LongStanding),
        Rate(6, EffectiveRateKind.LowUseGasKwhPerDay, 145m, LongStanding),
    ];

    private static Supplier Supplier(int id, string name) => new()
    {
        Id = id,
        PricingName = name,
        DisplayName = name,
        SuppliesElectricity = true,
        SuppliesGas = true,
        IsActive = true,
        UpfrontPercent = 0m,   // all monthly until finance gives the payment models (F-2)
        MaxUnitRateUpliftElectricity = ElectricityMaxUnitRate,
        MaxStandingChargeUpliftElectricity = ElectricityMaxStandingCharge,
        MaxUnitRateUpliftGas = GasMaxUnitRate,
        MaxStandingChargeUpliftGas = GasMaxStandingCharge,
    };

    private static EffectiveRate Rate(int id, EffectiveRateKind kind, decimal value, DateOnly from) => new()
    {
        Id = id,
        Kind = kind,
        Value = value,
        EffectiveFrom = from,
        CreatedUtc = SeededUtc,
    };

    private static SettingVersion[] BuildSettings()
    {
        var settings = new List<SettingVersion>();

        void Add(string key, object? value, bool isPlaceholder = false) => settings.Add(new SettingVersion
        {
            Id = settings.Count + 1,
            Key = key,
            Version = 1,
            ValueJson = JsonSerializer.Serialize(value),
            IsPlaceholder = isPlaceholder,
            CreatedUtc = SeededUtc,
        });

        // Default uplifts, each within its fuel's maximum below. TO CONFIRM.
        Add(SettingKeys.DefaultUpliftElectricityUnitRate, 1.0m);
        Add(SettingKeys.DefaultUpliftElectricityStandingCharge, 5m);
        Add(SettingKeys.DefaultUpliftGasUnitRate, 0.5m);
        Add(SettingKeys.DefaultUpliftGasStandingCharge, 5m);

        // Per-fuel maximums for everyone. TO CONFIRM.
        Add(SettingKeys.UpliftLimitElectricityUnitRate, ElectricityMaxUnitRate);
        Add(SettingKeys.UpliftLimitElectricityStandingCharge, ElectricityMaxStandingCharge);
        Add(SettingKeys.UpliftLimitGasUnitRate, GasMaxUnitRate);
        Add(SettingKeys.UpliftLimitGasStandingCharge, GasMaxStandingCharge);

        Add(SettingKeys.CommissionBasis, nameof(CommissionBasis.PerYear));
        Add(SettingKeys.PaymentDefault, new PaymentDefault(PaymentMethod.DirectDebit, PaymentFrequency.Monthly, null));

        Add(SettingKeys.MicrobusinessThresholdElectricityKwh, 100_000);
        Add(SettingKeys.MicrobusinessThresholdGasKwh, 293_000);

        // Blank: not checked until an admin sets them (FR-045).
        Add(SettingKeys.LargeGasLimitAnnualQuantityKwh, null);
        Add(SettingKeys.LargeGasLimitMeterCapacity, null);
        Add(SettingKeys.LargeGasLimitDailyMeteredQuantity, null);

        Add(SettingKeys.RetentionYears, 6);

        // Customer wording: every item is a visibly marked placeholder until compliance approves it.
        Add(SettingKeys.WordingValidityStatement,
            "PLACEHOLDER: These prices were fetched on {PricesFetchedDate} and are valid until the end of that day.", isPlaceholder: true);
        Add(SettingKeys.WordingIndicativeNotice,
            "PLACEHOLDER: These prices are indicative and may change until a contract is agreed with the supplier.", isPlaceholder: true);
        Add(SettingKeys.WordingCreditCheck,
            "All prices are subject to supplier credit check and acceptance", isPlaceholder: true);
        Add(SettingKeys.WordingCommissionDisclosure,
            "PLACEHOLDER: Commission disclosure to be supplied by compliance. It may include the commission for the recommended option, such as {CommissionTotal}.", isPlaceholder: true);
        Add(SettingKeys.WordingMicrobusinessCommissionDisclosure,
            "PLACEHOLDER: Microbusiness commission disclosure to be supplied by compliance.", isPlaceholder: true);
        Add(SettingKeys.WordingMicrobusinessDisputeResolution,
            "PLACEHOLDER: Dispute resolution scheme (the Energy Ombudsman) wording to be supplied by compliance.", isPlaceholder: true);
        Add(SettingKeys.WordingMicrobusinessCoolingOff,
            "PLACEHOLDER: Cooling-off period wording to be supplied by compliance.", isPlaceholder: true);
        Add(SettingKeys.WordingVatCclNote,
            "PLACEHOLDER: VAT and Climate Change Levy note to be supplied by compliance.", isPlaceholder: true);
        Add(SettingKeys.WordingTerms,
            "PLACEHOLDER: Terms and conditions statement to be supplied by compliance.", isPlaceholder: true);

        Add(SettingKeys.TermsUrl, "https://www.example.com/terms-placeholder", isPlaceholder: true);

        return [.. settings];
    }
}

// The admin's default payment method (FR-024, FR-081), stored as the Payment.Default setting.
public sealed record PaymentDefault(PaymentMethod Method, PaymentFrequency? Frequency, PaymentAmount? Amount);
