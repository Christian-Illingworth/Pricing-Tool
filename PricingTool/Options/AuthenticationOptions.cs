using Microsoft.Extensions.Options;

namespace PricingTool.Options;

public enum AuthenticationMode { Entra, Development }

public sealed class AuthenticationOptions : IValidatableOptions
{
    public const string Section = "Authentication";

    // Entra (Microsoft sign-in) or Development (the made-up staff test sign-in, FR-095).
    public AuthenticationMode Mode { get; set; } = AuthenticationMode.Entra;

    // Absolute lifetime of the sign-in cookie, with no sliding renewal (FR-094).
    public int SessionHours { get; set; } = 8;

    public IEnumerable<string> Validate()
    {
        if (SessionHours is < 1 or > 8)
            yield return "Authentication:SessionHours must be between 1 and 8 (FR-094).";
    }
}

// The checks that need more than the Authentication section: the hosting environment, and the
// AzureAd keys Entra mode depends on. Messages name keys only.
internal sealed class AuthenticationEnvironmentValidator(IHostEnvironment env, IConfiguration config)
    : IValidateOptions<AuthenticationOptions>
{
    public ValidateOptionsResult Validate(string? name, AuthenticationOptions options)
    {
        var failures = new List<string>();

        if (options.Mode == AuthenticationMode.Development && !env.IsDevelopment())
            failures.Add($"Authentication:Mode=Development is only allowed in the Development environment, not '{env.EnvironmentName}'.");

        if (options.Mode == AuthenticationMode.Entra)
        {
            foreach (var key in new[] { "AzureAd:TenantId", "AzureAd:ClientId", "AzureAd:ClientSecret", "AzureAd:AllowedGroupId" })
            {
                if (OptionsRegistration.IsMissing(config[key]))
                    failures.Add($"{key} is required when Authentication:Mode is Entra. Set it in configuration (user secrets for the client secret).");
            }
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
