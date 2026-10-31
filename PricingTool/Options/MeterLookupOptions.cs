using System.Text.RegularExpressions;

namespace PricingTool.Options;

public enum MeterLookupMode { Mock, SwaggerHub, Sandbox, Live }

public sealed partial class MeterLookupOptions : IValidatableOptions
{
    public const string Section = "Powwr:MeterLookup";

    public MeterLookupMode Mode { get; set; } = MeterLookupMode.Mock;

    // Required in Live mode (POWWR gives it, Q2). The other modes have fixed addresses.
    public string? BaseUrl { get; set; }

    public string? TokenUrl { get; set; }

    public string? ClientId { get; set; }

    // SECRET: user secrets locally, Key Vault later.
    public string? ClientSecret { get; set; }

    public string? OrganisationPowwrId { get; set; }

    [GeneratedRegex(@"^[A-Z0-9]{1,8}-\d{3}-[SB]$")]
    internal static partial Regex OrganisationPowwrIdPattern();

    public IEnumerable<string> Validate()
    {
        if (Mode == MeterLookupMode.Mock) yield break;

        if (OrganisationPowwrId is null || !OrganisationPowwrIdPattern().IsMatch(OrganisationPowwrId))
            yield return "Powwr:MeterLookup:OrganisationPowwrId must match ^[A-Z0-9]{1,8}-\\d{3}-[SB]$ when Powwr:MeterLookup:Mode is not Mock.";

        if (Mode is MeterLookupMode.Sandbox or MeterLookupMode.Live)
        {
            if (OptionsRegistration.IsMissing(ClientId))
                yield return $"Powwr:MeterLookup:ClientId is required when Powwr:MeterLookup:Mode is {Mode}.";
            if (OptionsRegistration.IsMissing(ClientSecret))
                yield return $"Powwr:MeterLookup:ClientSecret is required when Powwr:MeterLookup:Mode is {Mode}. Set it with dotnet user-secrets.";
            if (!OptionsRegistration.IsAbsoluteHttps(TokenUrl))
                yield return $"Powwr:MeterLookup:TokenUrl must be an absolute https URL when Powwr:MeterLookup:Mode is {Mode}.";
        }

        if (Mode == MeterLookupMode.Live && !OptionsRegistration.IsAbsoluteHttps(BaseUrl))
            yield return "Powwr:MeterLookup:BaseUrl must be an absolute https URL when Powwr:MeterLookup:Mode is Live.";
    }
}
