namespace PricingTool.Services.Pricing;

// FR-029, FR-030: a site is low use at or below the fuel's daily threshold (33 kWh electricity, 145 kWh
// gas, admin settings), worked out from its annual consumption. The agent can't choose it.
public static class LowUseTest
{
    public static bool IsLowUse(int annualKwh, decimal thresholdKwhPerDay) => annualKwh / 365m <= thresholdKwhPerDay;
}
