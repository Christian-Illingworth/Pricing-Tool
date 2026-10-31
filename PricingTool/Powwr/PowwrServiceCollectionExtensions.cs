using Microsoft.Extensions.Options;
using PricingTool.Options;
using PricingTool.Powwr.MeterLookup;
using PricingTool.Powwr.Pricing;

namespace PricingTool.Powwr;

// Limits how many POWWR pricing calls run at once (Powwr:Pricing:MaxConcurrentRequests, research R6),
// across every quote being priced, until POWWR states its rate limits (Q1).
public sealed class PowwrConcurrency(IOptions<PowwrPricingOptions> options) : IDisposable
{
    public SemaphoreSlim Gate { get; } = new(options.Value.MaxConcurrentRequests, options.Value.MaxConcurrentRequests);

    public void Dispose() => Gate.Dispose();
}

public static class PowwrServiceCollectionExtensions
{
    private const string TokenClientName = "PowwrMeterLookupToken";

    // Both POWWR adapters, chosen by mode. Mock is the default for each, and tests never register an
    // HTTP client (constitution Principle VI).
    public static IServiceCollection AddPowwr(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<PowwrConcurrency>();

        var pricingMode = config.GetValue($"{PowwrPricingOptions.Section}:Mode", PowwrPricingMode.Mock);
        if (pricingMode == PowwrPricingMode.Mock)
        {
            services.AddSingleton<IPowwrPricingClient, MockPowwrPricingClient>();
        }
        else
        {
            services.AddTransient<PowwrRetryHandler>();
            services.AddHttpClient<IPowwrPricingClient, PowwrPricingHttpClient>((sp, http) =>
                {
                    var options = sp.GetRequiredService<IOptions<PowwrPricingOptions>>().Value;
                    http.BaseAddress = new Uri(PowwrPricingHttpClient.BaseUrlFor(options).TrimEnd('/') + "/");
                    http.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                })
                .AddHttpMessageHandler<PowwrRetryHandler>();
        }

        var lookupMode = config.GetValue($"{MeterLookupOptions.Section}:Mode", MeterLookupMode.Mock);
        if (lookupMode == MeterLookupMode.Mock)
        {
            services.AddSingleton<IMeterLookupClient, MockMeterLookupClient>();
        }
        else
        {
            services.AddHttpClient(TokenClientName, http => http.Timeout = TimeSpan.FromSeconds(20));
            services.AddSingleton(sp => new MeterLookupTokenProvider(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(TokenClientName),
                sp.GetRequiredService<IOptions<MeterLookupOptions>>(),
                sp.GetRequiredService<TimeProvider>()));
            services.AddHttpClient<IMeterLookupClient, MeterLookupHttpClient>((sp, http) =>
            {
                http.BaseAddress = new Uri(MeterLookupHttpClient.BaseUrlFor(sp.GetRequiredService<IOptions<MeterLookupOptions>>().Value));
                http.Timeout = TimeSpan.FromSeconds(20);
            });
        }

        return services;
    }
}
