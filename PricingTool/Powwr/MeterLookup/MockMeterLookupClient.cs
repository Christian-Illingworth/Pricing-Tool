using System.Text.Json;
using PricingTool.Domain;
using PricingTool.Powwr.MeterLookup.Dtos;
using PricingTool.Services.Settings;

namespace PricingTool.Powwr.MeterLookup;

// The default meter lookup: the made-up addresses and meters in the contract's mock table
// (contracts/powwr-meter-lookup-mapping.md). Every postcode is in the made-up ZZ1 or ZZ9 areas. It
// builds POWWR-shaped meters and maps them with the same MeterLookupMapper as the real client.
public sealed class MockMeterLookupClient(ISupplierMatcher suppliers) : IMeterLookupClient
{
    public const string NoAddressesPostcode = "ZZ9 9ZZ";
    public const string UnavailablePostcode = "ZZ9 9ZY";

    private sealed record MockAddress(string Id, string Postcode, string Line1, IReadOnlyList<ElectricMeter> Electric, IReadOnlyList<GasMeter> Gas);

    private static readonly Supplier BritishGas = new() { Mpid = "ENRD", Name = "British Gas Business", PowwrId = "BRITISHG-001-S" };
    private static readonly Supplier Edf = new() { Name = "EDF Energy", PowwrId = "EDFENERG-001-S" };
    private static readonly Supplier ScottishPower = new() { ShortCode = "SCT", Name = "Scottish Power", PowwrId = "SCOTTISH-001-S" };
    private static readonly Supplier EonNext = new() { ShortCode = "EOD", Name = "E.On Next", PowwrId = "EONNEXT-001-S" };

    private static readonly IReadOnlyList<MockAddress> Addresses =
    [
        new("MOCK-ZZ11AA-U1", "ZZ1 1AA", "Unit 1, Test Park",
            [Elec("1600000001013", "03", "801", "631", "0393", 18_000, BritishGas)],
            [GasMeter("1234567", 40_000, amr: "Y", supplier: ScottishPower)]),
        new("MOCK-ZZ11AB-U2", "ZZ1 1AB", "Unit 2, Test Park",
            [Elec("1600000001022", "04", "845", "661", "0151", 90_000, Edf, smets: "SMETS2")], []),
        new("MOCK-ZZ11AC-U3", "ZZ1 1AC", "Unit 3, Test Park",
            [Elec("1600000001031", "03", "801", "631", "0393", 10_950, BritishGas)], []),
        new("MOCK-ZZ11AD-U4", "ZZ1 1AD", "Unit 4, Test Park",
            [Elec("1600000001040", "03", "801", "631", "0393", 15_000, BritishGas, domestic: true)], []),
        new("MOCK-ZZ11AD-U5", "ZZ1 1AD", "Unit 5, Test Park",
            [Elec("1600000001050", "03", "801", "631", "0393", 15_000, BritishGas, energisation: "D")], []),
        new("MOCK-ZZ11AD-U6", "ZZ1 1AD", "Unit 6, Test Park",
            [Elec("1600000001069", "03", "801", "631", "0393", 15_000, BritishGas, greenDeal: "Active")], []),
        new("MOCK-ZZ11AE-U7", "ZZ1 1AE", "Unit 7, Test Park",
            [Elec("1200000000011", "00", "845", "510", null, 450_000, Edf, measurementClass: "C", smets: "SMETS2")], []),
        new("MOCK-ZZ11AF-U8", "ZZ1 1AF", "Unit 8, Test Park",
            [Elec("1200000000020", "04", "845", "661", null, 25_000, Edf)], []),
        new("MOCK-ZZ11AG-U9", "ZZ1 1AG", "Unit 9, Test Park",
            [],
            [
                GasMeter("2345678", 30_000, amr: "N", supplier: ScottishPower, igt: true),
                GasMeter("3456789", 35_000, amr: "Y", supplier: ScottishPower, incoming: EonNext),
                GasMeter("7654321", 800_000, amr: "N", supplier: BritishGas, capacity: 160, dmq: 2_500),
            ]),
    ];

    // An electricity meter with no address, for the "every supplier declines" pricing scenario.
    private static readonly ElectricMeter Unaddressed = Elec("2300000000019", "03", "801", "631", "0393", 20_000, BritishGas, line1: "Unit 10, Test Park", postcode: "ZZ1 1AH");

    public Task<LookupResult<IReadOnlyList<LookupAddress>>> SearchAddressesAsync(NormalisedPostcode postcode, string userId, CancellationToken ct)
    {
        if (postcode.Value == UnavailablePostcode)
            return Task.FromResult(LookupResult<IReadOnlyList<LookupAddress>>.Unavailable(MeterLookupHttpClient.UnavailableMessage));

        var found = Addresses.Where(a => a.Postcode == postcode.Value).Select(a => new MeterAddress
        {
            MeterAddressId = a.Id,
            FullAddress = $"{a.Line1.ToUpperInvariant()}, TESTVILLE, {a.Postcode}",
            MpanCount = a.Electric.Count,
            Mpans = a.Electric.Select(m => Redact(m.Mpan!)).ToList(),
            MprnCount = a.Gas.Count,
            Mprns = a.Gas.Select(m => Redact(m.Mprn!)).ToList(),
        }).ToList();

        var addresses = MeterLookupMapper.MapAddresses(new AddressSearchResults { MeterAddresses = found });
        return Task.FromResult(addresses.Count == 0
            ? LookupResult<IReadOnlyList<LookupAddress>>.NotFound($"No addresses found for {postcode.Value}. Check it, or enter the details yourself.")
            : LookupResult<IReadOnlyList<LookupAddress>>.Found(addresses));
    }

    public Task<LookupResult<LookupMeters>> SearchMetersAsync(MeterSearch search, string userId, CancellationToken ct)
    {
        var results = new MeterSearchResults { ElectricMeters = [], GasMeters = [] };
        if (search.MeterAddressId is { } id && Addresses.FirstOrDefault(a => a.Id == id) is { } address)
        {
            results.ElectricMeters.AddRange(address.Electric);
            results.GasMeters.AddRange(address.Gas);
        }
        else if (search.Mpan is { } mpan)
        {
            results.ElectricMeters.AddRange(Addresses.SelectMany(a => a.Electric).Append(Unaddressed).Where(m => m.Mpan == mpan));
        }
        else if (search.Mprn is { } mprn)
        {
            results.GasMeters.AddRange(Addresses.SelectMany(a => a.Gas).Where(m => m.Mprn == mprn));
        }

        var meters = MeterLookupMapper.MapMeters(results, suppliers);
        return Task.FromResult(meters.Electric.Count + meters.Gas.Count == 0
            ? LookupResult<LookupMeters>.NotFound("No meters found. Check the number, or enter the details yourself.")
            : LookupResult<LookupMeters>.Found(meters));
    }

    private static string Redact(string number) => new string('*', number.Length - 3) + number[^3..];

    private static ElectricMeter Elec(
        string mpan, string profileClass, string mtc, string llfc, string? ssc, int annualKwh, Supplier supplier,
        string measurementClass = "A", string? smets = null, bool domestic = false, string energisation = "E",
        string greenDeal = "None", string? line1 = null, string? postcode = null)
    {
        return new ElectricMeter
        {
            Mpan = mpan,
            GspId = "_G",
            EnergisationStatus = energisation,
            ProfileClass = profileClass,
            MeterTimeswitchClass = mtc,
            LineLossFactor = llfc,
            StandardSettlementConfiguration = ssc ?? "",
            MeasurementClass = measurementClass,
            IsDomesticConsumer = domestic,
            MeterType = smets is null ? "N" : "S2",
            CurrentSupplier = supplier,
            AnnualConsumption = annualKwh,
            GreenDealStatus = greenDeal,
            TradingStatus = "T",
            SmetsVersion = smets ?? "",
            MeterPointLocation = Location(line1 ?? UnitFor(mpan), postcode ?? PostcodeFor(mpan)),
        };
    }

    private static GasMeter GasMeter(
        string mprn, int aq, string amr, Supplier supplier, bool igt = false, Supplier? incoming = null, int capacity = 6, int dmq = 110) => new()
    {
        Mprn = mprn,
        IsIgt = igt,
        Ldz = "NW",
        ExitZone = "NW1",
        MeterType = "Synthetic",
        MeterCapacity = capacity,
        MeterMechanismCode = "CR",
        DailyMeteredQuantity = dmq,
        CurrentSupplier = supplier,
        IncomingSupplier = incoming,
        AnnualConsumption = aq,
        AdditionalInformation = new Dictionary<string, JsonElement> { ["amrIndicator"] = JsonSerializer.SerializeToElement(amr) },
        MeterPointLocation = new MeterPointLocation
        {
            BuildingName = mprn == "1234567" ? "UNIT 1" : "UNIT 9",
            Thoroughfare = "TEST PARK",
            Locality = "TESTVILLE",
            PostCode = mprn == "1234567" ? "ZZ1 1AA" : "ZZ1 1AG",
        },
    };

    private static MeterPointLocation Location(string line1, string postcode) => new()
    {
        AddressLine1 = line1.ToUpperInvariant(),
        AddressLine8 = "TESTVILLE",
        PostCode = postcode,
        FullAddress = $"{line1.ToUpperInvariant()}, TESTVILLE, {postcode}",
    };

    // Each mock MPAN's unit and postcode, so its meter point location matches its address.
    private static string UnitFor(string mpan) => mpan switch
    {
        "1600000001013" => "Unit 1, Test Park",
        "1600000001022" => "Unit 2, Test Park",
        "1600000001031" => "Unit 3, Test Park",
        "1600000001040" => "Unit 4, Test Park",
        "1600000001050" => "Unit 5, Test Park",
        "1600000001069" => "Unit 6, Test Park",
        "1200000000011" => "Unit 7, Test Park",
        "1200000000020" => "Unit 8, Test Park",
        _ => "Test Park",
    };

    private static string PostcodeFor(string mpan) => mpan switch
    {
        "1600000001013" => "ZZ1 1AA",
        "1600000001022" => "ZZ1 1AB",
        "1600000001031" => "ZZ1 1AC",
        "1600000001040" or "1600000001050" or "1600000001069" => "ZZ1 1AD",
        "1200000000011" => "ZZ1 1AE",
        "1200000000020" => "ZZ1 1AF",
        _ => "ZZ1 1AH",
    };
}
