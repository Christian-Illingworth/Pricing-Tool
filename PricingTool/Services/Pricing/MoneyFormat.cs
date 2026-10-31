using System.Globalization;

namespace PricingTool.Services.Pricing;

// Money as the screen, PDF and CSV show it: "£1,234.56", "-£2.50". Always through Rounding.ToPennies,
// so every place shows the same pennies (SC-004). Built without a culture lookup, so it doesn't depend
// on the server's installed culture data.
public static class MoneyFormat
{
    public static string Pounds(decimal value)
    {
        var pennies = Rounding.ToPennies(value);
        var digits = Math.Abs(pennies).ToString("#,##0.00", CultureInfo.InvariantCulture);
        return pennies < 0 ? $"-£{digits}" : $"£{digits}";
    }
}
