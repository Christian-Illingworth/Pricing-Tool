using System.Globalization;

namespace PricingTool.Services.Pricing;

// Base rates, which are agent-only (FR-037, FR-100): each returned rate minus the uplift POWWR reports
// (Rate.Uplift for unit rates, Rate.StandingChargeUplift for the standing charge), at the returned
// string's number of decimal places (research R7).
public static class BaseRates
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static decimal? Compute(string? returnedRate, string? uplift)
    {
        if (!TryParse(returnedRate, out var rate)) return null;
        var upliftValue = TryParse(uplift, out var u) ? u : 0m;
        return Math.Round(rate - upliftValue, DecimalPlaces(returnedRate!), MidpointRounding.AwayFromZero);
    }

    public static string Format(decimal baseRate, string returnedRate) =>
        baseRate.ToString("F" + DecimalPlaces(returnedRate), Invariant);

    public static int DecimalPlaces(string value)
    {
        var trimmed = value.Trim();
        var point = trimmed.IndexOf('.');
        return point < 0 ? 0 : trimmed.Length - point - 1;
    }

    private static bool TryParse(string? value, out decimal number) =>
        decimal.TryParse(value?.Trim(), NumberStyles.Number, Invariant, out number);
}
