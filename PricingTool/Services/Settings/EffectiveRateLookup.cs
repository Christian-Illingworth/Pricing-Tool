using PricingTool.Data.Entities;
using PricingTool.Domain;

namespace PricingTool.Services.Settings;

// FR-031: the rate in effect on a date is the latest row with EffectiveFrom on or before it.
public static class EffectiveRateLookup
{
    public static decimal? TryFind(IEnumerable<EffectiveRate> rows, EffectiveRateKind kind, DateOnly date) =>
        rows.Where(r => r.Kind == kind && r.EffectiveFrom <= date)
            .OrderByDescending(r => r.EffectiveFrom)
            .Select(r => (decimal?)r.Value)
            .FirstOrDefault();

    // A required rate (VAT, CCL, low-use thresholds) missing for the date is a settings error, not zero.
    public static decimal Find(IEnumerable<EffectiveRate> rows, EffectiveRateKind kind, DateOnly date) =>
        TryFind(rows, kind, date)
        ?? throw new InvalidOperationException($"No {kind} rate is in effect on {date:yyyy-MM-dd}. An admin needs to add one in Settings.");
}
