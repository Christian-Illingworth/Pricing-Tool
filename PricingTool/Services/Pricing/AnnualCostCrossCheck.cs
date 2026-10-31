using System.Globalization;
using PricingTool.Domain;
using PricingTool.Powwr.Pricing;

namespace PricingTool.Services.Pricing;

// Difference = POWWR's RawAnnualPrice − our recomputed cost.
public sealed record CrossCheckResult(decimal Recomputed, decimal Difference, bool IsSignificant);

// Research R7: recomputes the annual cost from the returned rates and compares it with POWWR's
// RawAnnualPrice. A difference over £1 or over 1% is logged. It never changes the figures shown: the
// annual cost is always POWWR's (FR-034).
public static class AnnualCostCrossCheck
{
    public static CrossCheckResult Check(ReturnedRate rate, IReadOnlyDictionary<Register, int> registerKwh, int? kva, int? kvarh)
    {
        var recomputed = 0m;
        foreach (var (register, kwh) in registerKwh)
        {
            var unitRate = register switch
            {
                Register.Day => rate.DayUnitrate,
                Register.Night => rate.NightUnitrate,
                _ => rate.WendUnitrate,
            };
            recomputed += Number(unitRate) * kwh / 100m;
        }

        recomputed += Number(rate.StandingCharge) * 365m / 100m;
        if (kva is > 0) recomputed += Number(rate.CapacityCharge) * kva.Value * 365m / 100m;
        if (kvarh is > 0) recomputed += Number(rate.ReactiveRate) * kvarh.Value / 100m;
        // A fixed fee is treated as pence per day, like a standing charge. Its unit isn't documented (Q4),
        // which only affects this logged check. Seasonal (STOD) rates have no consumption split to use.
        recomputed += Number(rate.FixedFee) * 365m / 100m;

        var difference = rate.RawAnnualPrice - recomputed;
        var size = Math.Abs(difference);
        return new CrossCheckResult(recomputed, difference, size > 1m || size > Math.Abs(rate.RawAnnualPrice) * 0.01m);
    }

    private static decimal Number(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) ? number : 0m;
}
