using System.Text.Json;
using System.Text.Json.Serialization;

namespace PricingTool.Powwr.MeterLookup.Dtos;

// POWWR Meter Lookup Service 2.5.0 (docs/powwr/meter-lookup-api-2.5.0.json). camelCase, read
// case-insensitively, because POWWR's own examples mix "postCode", "Postcode" and "AddressLine1".
public static class MeterLookupJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };
}

public sealed class CallerIdentification
{
    public string? OrganisationPowwrId { get; set; }
    public string? ApplicationId { get; set; }
    public string? UserId { get; set; }
}

public sealed class AddressSearchRequest
{
    public CallerIdentification? CallerIdentification { get; set; }
    public string? PostCode { get; set; }
}

public sealed class AddressSearchResults
{
    public List<MeterAddress>? MeterAddresses { get; set; }
}

public sealed class MeterAddress
{
    public string? MeterAddressId { get; set; }
    public string? FullAddress { get; set; }
    public int? MpanCount { get; set; }
    public List<string>? Mpans { get; set; }
    public int? MprnCount { get; set; }
    public List<string>? Mprns { get; set; }
}

public sealed class MeterSearchRequest
{
    public CallerIdentification? CallerIdentification { get; set; }
    public string? MeterAddressId { get; set; }
    public string? Mpan { get; set; }
    public string? Mprn { get; set; }
}

public sealed class MeterSearchResults
{
    public List<ElectricMeter>? ElectricMeters { get; set; }
    public List<GasMeter>? GasMeters { get; set; }
}

public sealed class MeterPointLocation
{
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? AddressLine3 { get; set; }
    public string? AddressLine4 { get; set; }
    public string? AddressLine5 { get; set; }
    public string? AddressLine6 { get; set; }
    public string? AddressLine7 { get; set; }
    public string? AddressLine8 { get; set; }
    public string? AddressLine9 { get; set; }
    public string? DeliveryPointAlias { get; set; }
    public string? SubBuilding { get; set; }
    public string? BuildingName { get; set; }
    public string? BuildingNumber { get; set; }
    public string? DependentThoroughfare { get; set; }
    public string? Thoroughfare { get; set; }
    public string? DependentLocality { get; set; }
    public string? Locality { get; set; }
    public string? AdministrativeArea { get; set; }
    public string? PostCode { get; set; }
    public string? FullAddress { get; set; }
}

public sealed class Supplier
{
    public string? Mpid { get; set; }
    public string? ShortCode { get; set; }
    public string? Name { get; set; }
    public string? EffectiveDate { get; set; }
    public string? PowwrId { get; set; }
}

public sealed class ElectricMeter
{
    public MeterPointLocation? MeterPointLocation { get; set; }
    public string? Mpan { get; set; }
    public string? GspId { get; set; }
    public string? EnergisationStatus { get; set; }
    public string? EnergisationStatusEffectiveDate { get; set; }
    public string? ProfileClass { get; set; }
    public string? MeterTimeswitchClass { get; set; }
    public string? LineLossFactor { get; set; }
    public string? StandardSettlementConfiguration { get; set; }
    public string? MeasurementClass { get; set; }
    public bool? IsDomesticConsumer { get; set; }
    public string? SerialNumber { get; set; }
    public string? MeterType { get; set; }
    public string? CurrentSupplierName { get; set; }
    public Supplier? CurrentSupplier { get; set; }
    public int? AnnualConsumption { get; set; }
    public string? GreenDealStatus { get; set; }
    public string? TradingStatus { get; set; }
    public string? SmetsVersion { get; set; }
    public Dictionary<string, JsonElement>? AdditionalInformation { get; set; }
}

public sealed class GasMeter
{
    public MeterPointLocation? MeterPointLocation { get; set; }
    public string? Mprn { get; set; }

    [JsonPropertyName("isIGT")] public bool? IsIgt { get; set; }

    public string? Ldz { get; set; }
    public string? ExitZone { get; set; }
    public string? SerialNumber { get; set; }
    public string? MeterType { get; set; }
    public int? MeterCapacity { get; set; }
    public string? MeterMechanismCode { get; set; }
    public int? DailyMeteredQuantity { get; set; }
    public string? CurrentSupplierName { get; set; }
    public Supplier? CurrentSupplier { get; set; }
    public Supplier? IncomingSupplier { get; set; }
    public int? AnnualConsumption { get; set; }
    public Dictionary<string, JsonElement>? AdditionalInformation { get; set; }
    public string? MeterModel { get; set; }
}

// RFC 7807, as POWWR returns it for a 400.
public sealed class ProblemDetails
{
    public string? Type { get; set; }
    public string? Title { get; set; }
    public int? Status { get; set; }
    public string? Detail { get; set; }
    public ProblemExtensions? Extensions { get; set; }
}

public sealed class ProblemExtensions
{
    public List<Issue>? Errors { get; set; }
    public List<Issue>? Warnings { get; set; }
}

public sealed class Issue
{
    public string? Entity { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public List<EntityProperty>? Properties { get; set; }
}

public sealed class EntityProperty
{
    public string? Path { get; set; }

    // Never logged: it may be personal data (contracts/powwr-meter-lookup-mapping.md).
    public JsonElement? ProvidedValue { get; set; }
}

// OAuth2 client credentials token response.
public sealed class TokenResponse
{
    [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
    [JsonPropertyName("expires_in")] public int? ExpiresIn { get; set; }
    [JsonPropertyName("token_type")] public string? TokenType { get; set; }
}
