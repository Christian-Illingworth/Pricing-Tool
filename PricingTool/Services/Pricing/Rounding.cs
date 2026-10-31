namespace PricingTool.Services.Pricing;

// The one rounding rule (research R7, constitution Principle VI). Figures are stored at full precision
// and rounded only when displayed (screen, PDF, CSV) or written into a reporting record's display
// field. Totals are summed first, then rounded. Rates are never rounded: they're shown as returned.
public static class Rounding
{
    public static decimal ToPennies(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
