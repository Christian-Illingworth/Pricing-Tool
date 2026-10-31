using PricingTool.Domain;
using PricingTool.Powwr.MeterLookup;
using PricingTool.Services.Settings;
using PricingTool.Services.Validation;

namespace PricingTool.Tests.Powwr;

// The mock lookup's table (contracts/powwr-meter-lookup-mapping.md), which the quickstart scenarios rely on.
public class MockMeterLookupClientTests
{
    private sealed class SeedMatcher : ISupplierMatcher
    {
        private static readonly SupplierAliasEntry[] Aliases =
        [
            new(1, "British Gas", "BRITISHG-001-S", "ENRD", "British Gas Business"),
            new(2, "EDF", "EDFENERG-001-S", null, "EDF Energy"),
            new(3, "E.ON Next", "EONNEXT-001-S", null, "E.On Next"),
            new(6, "Scottish Power", "SCOTTISH-001-S", "SCT", "Scottish Power"),
        ];

        public SupplierMatch Match(string? powwrId, string? mpid, string? shortCode, string? name) =>
            SupplierMatcher.Match(Aliases, powwrId, mpid, shortCode, name);

        public void Invalidate() { }
    }

    private static readonly MockMeterLookupClient Client = new(new SeedMatcher());

    private static NormalisedPostcode Postcode(string value) => Services.Validation.Postcode.Normalise(value)!;

    private static async Task<LookupMeters> MetersAt(string postcode)
    {
        var addresses = (await Client.SearchAddressesAsync(Postcode(postcode), "user", default)).Value!;
        var all = new List<LookupMeters>();
        foreach (var address in addresses)
            all.Add((await Client.SearchMetersAsync(MeterSearch.ByAddress(address.MeterAddressId), "user", default)).Value!);
        return new LookupMeters(all.SelectMany(m => m.Electric).ToList(), all.SelectMany(m => m.Gas).ToList());
    }

    [Fact]
    public async Task StandardSiteMatchesTheQuickstart()
    {
        // Quickstart US1-1: PC 03, MTC 801, LLFC 631, SSC 0393, North Western, British Gas, 18,000 kWh.
        var meter = Assert.Single((await MetersAt("zz11aa")).Electric);
        Assert.Equal("1600000001013", meter.MpanCore);
        Assert.True(meter.IsMpanValid);
        Assert.Equal(3, meter.ProfileClass);
        Assert.Equal(801, meter.MeterTimeswitchClass);
        Assert.Equal("631", meter.LineLossFactorClass);
        Assert.Equal("0393", meter.StandardSettlementConfig);
        Assert.Equal("British Gas", meter.CurrentSupplier!.PricingName);
        Assert.Equal(18_000, meter.AnnualConsumptionKwh);
        Assert.Equal("ZZ1 1AA", meter.Address!.Postcode);
    }

    [Fact]
    public async Task StandardSiteAlsoHasAGasMeter()
    {
        var gas = Assert.Single((await MetersAt("ZZ1 1AA")).Gas);
        Assert.Equal("1234567", gas.Mprn);
        Assert.Equal(40_000, gas.AnnualQuantityKwh);
        Assert.Equal("NW", gas.Ldz);
        Assert.True(gas.SmartMeterSuggested);
    }

    [Fact]
    public async Task AddressListShowsRedactedHintsOnly()
    {
        var address = Assert.Single((await Client.SearchAddressesAsync(Postcode("ZZ1 1AA"), "user", default)).Value!);
        Assert.Equal(1, address.ElectricMeterCount);
        Assert.Equal(1, address.GasMeterCount);
        Assert.All(address.RedactedNumbers, n => Assert.StartsWith("*", n));
    }

    [Fact]
    public async Task WarningSitesCarryTheirWarnings()
    {
        var meters = (await MetersAt("ZZ1 1AD")).Electric;
        Assert.Equal(3, meters.Count);
        Assert.Contains(meters.Single(m => m.MpanCore == "1600000001040").Warnings, w => w.Code == SiteWarningCode.Domestic);
        Assert.Contains(meters.Single(m => m.MpanCore == "1600000001050").Warnings, w => w.Code == SiteWarningCode.DeEnergised);
        Assert.Contains(meters.Single(m => m.MpanCore == "1600000001069").Warnings, w => w.Code == SiteWarningCode.GreenDealActive);
    }

    [Fact]
    public async Task HalfHourlySiteHasProfileClassZero()
    {
        var meter = Assert.Single((await MetersAt("ZZ1 1AE")).Electric);
        Assert.Equal(0, meter.ProfileClass);
        Assert.True(Meters.IsHalfHourly(meter.ProfileClass, meter.MeasurementClass));
    }

    [Fact]
    public async Task GasSitesHaveIgtSwitchingAndLargeMeters()
    {
        var gas = (await MetersAt("ZZ1 1AG")).Gas;
        Assert.Contains(gas.Single(g => g.Mprn == "2345678").Warnings, w => w.Code == SiteWarningCode.Igt);
        Assert.Contains(gas.Single(g => g.Mprn == "3456789").Warnings, w => w.Code == SiteWarningCode.SwitchInProgress && w.Message.Contains("E.ON Next"));
        var large = gas.Single(g => g.Mprn == "7654321");
        Assert.Equal(800_000, large.AnnualQuantityKwh);
        Assert.Equal(160, large.MeterCapacity);
        Assert.Equal(2_500, large.DailyMeteredQuantity);
    }

    [Fact]
    public async Task NoAddressesAndUnavailablePostcodes()
    {
        Assert.Equal(LookupStatus.NotFound, (await Client.SearchAddressesAsync(Postcode("ZZ9 9ZZ"), "user", default)).Status);
        var unavailable = await Client.SearchAddressesAsync(Postcode("ZZ9 9ZY"), "user", default);
        Assert.Equal(LookupStatus.Unavailable, unavailable.Status);
        Assert.NotNull(unavailable.ProblemSummary);
    }

    [Fact]
    public async Task MetersCanBeFoundByNumber()
    {
        Assert.True(MpanCore.TryParse("2300000000019", out var unaddressed, out _));
        Assert.Single((await Client.SearchMetersAsync(MeterSearch.ByMpan(unaddressed!), "user", default)).Value!.Electric);

        Assert.True(Mprn.TryParse("7654321", out var mprn, out _));
        Assert.Single((await Client.SearchMetersAsync(MeterSearch.ByMprn(mprn!), "user", default)).Value!.Gas);

        Assert.True(MpanCore.TryParse("1610028163273", out var unknown, out _));
        Assert.Equal(LookupStatus.NotFound, (await Client.SearchMetersAsync(MeterSearch.ByMpan(unknown!), "user", default)).Status);
    }
}
