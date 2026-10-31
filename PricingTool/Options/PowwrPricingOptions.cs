namespace PricingTool.Options;

public enum PowwrPricingMode { Mock, SwaggerHub, Live }

public sealed class PowwrPricingOptions : IValidatableOptions
{
    public const string Section = "Powwr:Pricing";

    public PowwrPricingMode Mode { get; set; } = PowwrPricingMode.Mock;

    // Used in Live mode. SwaggerHub mode uses POWWR's virtserver URL unless this is set.
    public string? BaseUrl { get; set; }

    // SECRET: user secrets locally, Key Vault later. Required only in Live mode.
    public string? LicenceCode { get; set; }

    public int MaxConcurrentRequests { get; set; } = 4;

    public int TimeoutSeconds { get; set; } = 45;

    // Our business type → POWWR's BusinessType string (open question Q4).
    public Dictionary<string, string> BusinessTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<string> Validate()
    {
        if (Mode == PowwrPricingMode.Live)
        {
            if (OptionsRegistration.IsMissing(LicenceCode))
                yield return "Powwr:Pricing:LicenceCode is required when Powwr:Pricing:Mode is Live. Set it with dotnet user-secrets.";
            if (!OptionsRegistration.IsAbsoluteHttps(BaseUrl))
                yield return "Powwr:Pricing:BaseUrl must be an absolute https URL when Powwr:Pricing:Mode is Live.";
        }
        if (MaxConcurrentRequests is < 1 or > 16)
            yield return "Powwr:Pricing:MaxConcurrentRequests must be between 1 and 16.";
        if (TimeoutSeconds is < 5 or > 300)
            yield return "Powwr:Pricing:TimeoutSeconds must be between 5 and 300.";
    }
}

// Mock-only settings (contracts/configuration.md).
public sealed class PowwrMockOptions : IValidatableOptions
{
    public const string Section = "Powwr:Mock";

    // Suppliers whose /pricechange entries report a change, to exercise FR-052.
    public string[] ChangedSuppliers { get; set; } = [];

    public IEnumerable<string> Validate() => [];
}
