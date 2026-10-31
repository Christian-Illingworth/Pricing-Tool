using PricingTool.Domain;

namespace PricingTool.Powwr.Pricing;

// POWWR's BusinessType string for our business type. POWWR hasn't said which values it accepts
// (open question Q4), so the map is configuration (Powwr:Pricing:BusinessTypes): an answer needs no
// code change. These are the starting labels, used if a type is missing from the map.
public static class BusinessTypeText
{
    public static readonly IReadOnlyDictionary<string, string> DefaultLabels = new Dictionary<string, string>
    {
        [nameof(BusinessType.SoleTrader)] = "Sole Trader",
        [nameof(BusinessType.Partnership)] = "Partnership",
        [nameof(BusinessType.LimitedCompany)] = "Limited Company",
        [nameof(BusinessType.Llp)] = "LLP",
        [nameof(BusinessType.Plc)] = "PLC",
        [nameof(BusinessType.Charity)] = "Charity",
        [nameof(BusinessType.PublicSector)] = "Public Sector",
    };

    public static string For(BusinessType type, IReadOnlyDictionary<string, string> map) =>
        map.TryGetValue(type.ToString(), out var text) && !string.IsNullOrWhiteSpace(text)
            ? text
            : DefaultLabels[type.ToString()];
}
