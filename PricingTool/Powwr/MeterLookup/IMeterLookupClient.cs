using PricingTool.Domain;

namespace PricingTool.Powwr.MeterLookup;

// The only way the app reaches POWWR's Meter Lookup Service. Implementations: MockMeterLookupClient
// (the default) and MeterLookupHttpClient (SwaggerHub, Sandbox and Live), chosen by
// Powwr:MeterLookup:Mode. userId is the agent's Entra object ID, sent for POWWR's usage reports.
public interface IMeterLookupClient
{
    Task<LookupResult<IReadOnlyList<LookupAddress>>> SearchAddressesAsync(NormalisedPostcode postcode, string userId, CancellationToken ct);

    Task<LookupResult<LookupMeters>> SearchMetersAsync(MeterSearch search, string userId, CancellationToken ct);
}
