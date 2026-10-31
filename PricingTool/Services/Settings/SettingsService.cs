using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PricingTool.Data;
using PricingTool.Data.Entities;
using PricingTool.Domain;
using PricingTool.Services.Pricing;

namespace PricingTool.Services.Settings;

// Reads the admin settings (research R19): the current version of each SettingVersion key, the
// effective-dated rates, and the snapshot a pricing run is calculated and stored with.
public sealed class SettingsService(PricingDbContext db)
{
    // ── Versioned settings ──

    public Task<SettingVersion?> GetCurrentVersionAsync(string key, CancellationToken ct) =>
        db.SettingVersions.AsNoTracking()
            .Where(s => s.Key == key)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(ct);

    // The highest version of every key.
    public async Task<IReadOnlyDictionary<string, SettingVersion>> GetAllCurrentAsync(CancellationToken ct)
    {
        var all = await db.SettingVersions.AsNoTracking().ToListAsync(ct);
        return all.GroupBy(s => s.Key)
                  .ToDictionary(g => g.Key, g => g.MaxBy(s => s.Version)!, StringComparer.Ordinal);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct)
    {
        var current = await GetCurrentVersionAsync(key, ct);
        return current is null ? default : Deserialize<T>(current);
    }

    public Task<decimal?> GetDecimalAsync(string key, CancellationToken ct) => GetAsync<decimal?>(key, ct);

    public Task<int?> GetIntAsync(string key, CancellationToken ct) => GetAsync<int?>(key, ct);

    public Task<string?> GetStringAsync(string key, CancellationToken ct) => GetAsync<string?>(key, ct);

    public static T? Deserialize<T>(SettingVersion version) => JsonSerializer.Deserialize<T>(version.ValueJson);

    // ── Effective-dated rates ──

    public async Task<decimal> GetEffectiveRateAsync(EffectiveRateKind kind, DateOnly date, CancellationToken ct) =>
        EffectiveRateLookup.Find(await db.EffectiveRates.AsNoTracking().Where(r => r.Kind == kind).ToListAsync(ct), kind, date);

    // ── The pricing snapshot ──

    // Everything the pricing engine needs for one run: rates for each site's start date, the
    // microbusiness threshold, the commission basis, each supplier's payment model and maximums, and
    // the per-fuel maximums. Stored on PricingRun.SettingsSnapshot.
    public async Task<PricingSettingsSnapshot> GetPricingSnapshotAsync(Fuel fuel, IEnumerable<DateOnly> siteStartDates, CancellationToken ct)
    {
        var settings = await GetAllCurrentAsync(ct);
        var rates = await db.EffectiveRates.AsNoTracking().ToListAsync(ct);
        var suppliers = await db.Suppliers.AsNoTracking().OrderBy(s => s.PricingName).ToListAsync(ct);

        var (cclKind, lowUseKind) = fuel == Fuel.Electricity
            ? (EffectiveRateKind.CclElectricity, EffectiveRateKind.LowUseElectricityKwhPerDay)
            : (EffectiveRateKind.CclGas, EffectiveRateKind.LowUseGasKwhPerDay);

        var ratesByDate = siteStartDates.Distinct().Order().Select(date => new SiteRates(
            date,
            EffectiveRateLookup.Find(rates, EffectiveRateKind.VatStandard, date),
            EffectiveRateLookup.Find(rates, EffectiveRateKind.VatReduced, date),
            EffectiveRateLookup.Find(rates, cclKind, date),
            EffectiveRateLookup.Find(rates, lowUseKind, date),
            EffectiveRateLookup.TryFind(rates, EffectiveRateKind.QualifyingWholeSupplyPercent, date))).ToList();

        var basis = Value<string>(settings, SettingKeys.CommissionBasis) is { } basisName
            ? Enum.Parse<CommissionBasis>(basisName)
            : CommissionBasis.PerYear;

        return new PricingSettingsSnapshot(
            fuel,
            basis,
            ratesByDate,
            Value<decimal?>(settings, SettingKeys.MicrobusinessThresholdKwh(fuel))
                ?? throw new InvalidOperationException($"The setting {SettingKeys.MicrobusinessThresholdKwh(fuel)} isn't set."),
            Value<decimal?>(settings, SettingKeys.UnitRateUpliftLimit(fuel)),
            Value<decimal?>(settings, SettingKeys.StandingChargeUpliftLimit(fuel)),
            suppliers.Select(s => new SupplierSettings(
                s.Id,
                s.PricingName,
                s.UpfrontPercent,
                fuel == Fuel.Electricity ? s.MaxUnitRateUpliftElectricity : s.MaxUnitRateUpliftGas,
                fuel == Fuel.Electricity ? s.MaxStandingChargeUpliftElectricity : s.MaxStandingChargeUpliftGas)).ToList());
    }

    private static T? Value<T>(IReadOnlyDictionary<string, SettingVersion> settings, string key) =>
        settings.TryGetValue(key, out var version) ? Deserialize<T>(version) : default;
}
