namespace PricingTool.Powwr.Pricing;

// The only way the app reaches POWWR's UK Pricing API (constitution Principle VI). Implementations:
// MockPowwrPricingClient (the default) and PowwrPricingHttpClient (SwaggerHub and Live), chosen by
// Powwr:Pricing:Mode. It takes and returns domain types, never DTOs, HTTP types or secrets.
public interface IPowwrPricingClient
{
    Task<SitePriceResponse> GetElectricPricesAsync(ElectricPriceRequest request, CancellationToken ct);

    Task<SitePriceResponse> GetGasPricesAsync(GasPriceRequest request, CancellationToken ct);

    Task<RateStructureResponse> GetRateStructureAsync(RateStructureRequest request, CancellationToken ct);

    Task<IReadOnlyList<SupplierPriceChange>> GetLatestPriceChangesAsync(string? supplierName, CancellationToken ct);

    Task<bool> PingAsync(CancellationToken ct);
}
