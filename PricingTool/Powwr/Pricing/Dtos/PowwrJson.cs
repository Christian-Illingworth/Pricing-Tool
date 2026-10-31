using System.Text.Json;
using System.Text.Json.Serialization;

namespace PricingTool.Powwr.Pricing.Dtos;

// POWWR UK Pricing API 1.2.0 JSON (docs/powwr/uk-pricing-api-1.2.0.json): PascalCase names exactly as
// defined, so no naming policy; nulls left out of requests. Numbers are read straight into decimal,
// never through double, so Raw* figures arrive exact (research R4).
public static class PowwrJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };
}
