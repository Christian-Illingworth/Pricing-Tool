using Microsoft.Extensions.DependencyInjection;
using PricingTool.Powwr.MeterLookup;
using PricingTool.Powwr.Pricing;
using PricingTool.Tests.Support;

namespace PricingTool.Tests.Powwr;

// T052: the adapters are chosen by configuration, and the mocks are the default (Principle VI).
// Resolving a client makes no network call.
public class PowwrRegistrationTests
{
    [Fact]
    public void MocksAreTheDefault()
    {
        using var factory = PricingToolFactory.Development();
        using var scope = factory.Services.CreateScope();

        Assert.IsType<MockPowwrPricingClient>(scope.ServiceProvider.GetRequiredService<IPowwrPricingClient>());
        Assert.IsType<MockMeterLookupClient>(scope.ServiceProvider.GetRequiredService<IMeterLookupClient>());
    }

    [Fact]
    public void LiveAndSandboxModesUseTheHttpClients()
    {
        var settings = new Dictionary<string, string?>(PricingToolFactory.DevelopmentSettings)
        {
            ["Powwr:Pricing:Mode"] = "Live",
            ["Powwr:Pricing:LicenceCode"] = "not-a-real-licence",
            ["Powwr:MeterLookup:Mode"] = "Sandbox",
            ["Powwr:MeterLookup:ClientId"] = "not-a-real-client",
            ["Powwr:MeterLookup:ClientSecret"] = "not-a-real-secret",
            ["Powwr:MeterLookup:OrganisationPowwrId"] = "TESTORG-001-B",
        };
        using var factory = new PricingToolFactory("Development", settings);
        using var scope = factory.Services.CreateScope();

        Assert.IsType<PowwrPricingHttpClient>(scope.ServiceProvider.GetRequiredService<IPowwrPricingClient>());
        Assert.IsType<MeterLookupHttpClient>(scope.ServiceProvider.GetRequiredService<IMeterLookupClient>());
    }

    [Fact]
    public void LiveModeWithoutTheLicenceFailsAtStartupNamingTheKey()
    {
        var settings = new Dictionary<string, string?>(PricingToolFactory.DevelopmentSettings) { ["Powwr:Pricing:Mode"] = "Live" };
        using var factory = new PricingToolFactory("Development", settings);

        var error = Assert.ThrowsAny<Exception>(() => factory.Services);
        Assert.Contains("Powwr:Pricing:LicenceCode", error.ToString());
    }
}
