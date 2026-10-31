using Microsoft.Extensions.Options;

namespace PricingTool.Options;

// Options classes that check themselves. A failure names the configuration KEY, never its value, so a
// missing or wrong secret stops startup without leaking anything (constitution Principle I).
public interface IValidatableOptions
{
    IEnumerable<string> Validate();
}

internal sealed class SelfValidatingOptions<T> : IValidateOptions<T> where T : class, IValidatableOptions
{
    public ValidateOptionsResult Validate(string? name, T options)
    {
        var failures = options.Validate().ToList();
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}

public static class OptionsRegistration
{
    // Binds every operational section (contracts/configuration.md) with ValidateOnStart(), so a wrong
    // setting stops the app at startup rather than on the first request that needs it.
    public static IServiceCollection AddPricingToolOptions(this IServiceCollection services, IConfiguration config)
    {
        Add<AuthenticationOptions>(services, config, AuthenticationOptions.Section);
        services.AddSingleton<IValidateOptions<AuthenticationOptions>, AuthenticationEnvironmentValidator>();
        Add<OutlookOptions>(services, config, OutlookOptions.Section);
        services.AddSingleton<IValidateOptions<OutlookOptions>, OutlookModeValidator>();
        Add<PowwrPricingOptions>(services, config, PowwrPricingOptions.Section);
        Add<PowwrMockOptions>(services, config, PowwrMockOptions.Section);
        Add<MeterLookupOptions>(services, config, MeterLookupOptions.Section);
        Add<StorageOptions>(services, config, StorageOptions.Section);
        Add<CompanyOptions>(services, config, CompanyOptions.Section);
        Add<RetentionOptions>(services, config, RetentionOptions.Section);
        Add<PdfOptions>(services, config, PdfOptions.Section);
        Add<UploadOptions>(services, config, UploadOptions.Section);
        Add<ReportingOptions>(services, config, ReportingOptions.Section);
        return services;
    }

    private static void Add<T>(IServiceCollection services, IConfiguration config, string section)
        where T : class, IValidatableOptions
    {
        services.AddOptions<T>().Bind(config.GetSection(section)).ValidateOnStart();
        services.AddSingleton<IValidateOptions<T>, SelfValidatingOptions<T>>();
    }

    internal static bool IsMissing(string? value) => string.IsNullOrWhiteSpace(value);

    internal static bool IsAbsoluteHttps(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
}
