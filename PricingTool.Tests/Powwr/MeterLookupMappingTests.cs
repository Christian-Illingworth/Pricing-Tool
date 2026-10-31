using System.Text.Json;
using PricingTool.Domain;
using PricingTool.Powwr.MeterLookup;
using PricingTool.Powwr.MeterLookup.Dtos;
using PricingTool.Services.Settings;

namespace PricingTool.Tests.Powwr;

// contracts/powwr-meter-lookup-mapping.md. The bodies below are POWWR's own example responses from
// docs/powwr/meter-lookup-api-2.5.0.json, casing and all.
public class MeterLookupMappingTests
{
    private const string ElectricExample = """
        {"electricMeters":[{"mpan":"1610028163273","gspId":"_G","energisationStatus":"E","energisationStatusEffectiveDate":"2024-02-14","profileClass":"03","meterTimeswitchClass":"801","lineLossFactor":"631","standardSettlementConfiguration":"0393","measurementClass":"A","isDomesticConsumer":false,"serialNumber":"D07W551948","meterType":"N","currentSupplierName":"","currentSupplier":{"mpid":"ENRD","name":"British Gas Business","effectiveDate":"2024-02-14","powwrId":"BRITISHG-001-S"},"annualConsumption":12345,"meterPointLocation":{"fullAddress":"1ST FLOOR EAST WING, PARKWAY HOUSE, PALATINE ROAD, MANCHESTER, LANCASHIRE, M22 4DB","AddressLine1":"","AddressLine2":"1ST FLOOR EAST WING","AddressLine3":"PARKWAY HOUSE","AddressLine4":"","AddressLine5":"PALATINE ROAD","AddressLine6":"","AddressLine7":"","AddressLine8":"MANCHESTER","AddressLine9":"LANCASHIRE","Postcode":"M22 4DB"},"greenDealStatus":"Active","tradingStatus":"T","smetsVersion":"SMETS1","additionalInformation":{"dataAggregatorMpid":"ACCU","dataCollectorMpid":"ACCU","meterOperatorMpid":"OESL"}}]}
        """;

    private const string GasExample = """
        {"gasMeters":[{"mprn":"14097001","isIGT":false,"exitZone":"NW2","ldz":"NW","serialNumber":"M040K0283013D6","meterType":"Synthetic","meterCapacity":6,"meterMechanismCode":"CR","dailyMeteredQuantity":100,"currentSupplierName":"Scottish Power Energy Retail Limited","currentSupplier":{"shortCode":"SCT","name":"Scottish Power","effectiveDate":"2022-03-20","powwrId":"SCOTTISH-001-S"},"incomingSupplier":{"shortCode":"EOD","name":"E.On Next","powwrId":"EONNEXT-001-S","effectiveDate":"2024-03-18"},"annualConsumption":54321,"meterPointLocation":{"subBuilding":"","buildingName":"PARKWAY HOUSE","buildingNumber":"","dependentThoroughfare":"","thoroughfare":"PALATINE ROAD","dependentLocality":"","locality":"MANCHESTER","postCode":"M22 4DB","fullAddress":"PARKWAY HOUSE, PALATINE ROAD, MANCHESTER, M22 4DB,"},"additionalInformation":{"amrIndicator":"N","meterDeviceStatus":"LI","meterNumberOfDials":"05","previousSupplierName":"Octopus Energy Limited"}}]}
        """;

    private const string AddressExample = """
        {"meterAddresses":[{"meterAddressId":"U10023061032A0085759074G1M1610028163246N2PM224DB","fullAddress":"PARKWAY HOUSE, PALATINE ROAD, MANCHESTER, M22 4DB","mpanCount":2,"mpans":["**********273","**********231"],"mprnCount":1,"mprns":["*****001"]}]}
        """;

    private sealed class Matcher : ISupplierMatcher
    {
        private static readonly SupplierAliasEntry[] Aliases =
        [
            new(1, "British Gas", "BRITISHG-001-S", "ENRD", "British Gas Business"),
            new(6, "Scottish Power", "SCOTTISH-001-S", "SCT", "Scottish Power"),
            new(3, "E.ON Next", "EONNEXT-001-S", null, "E.On Next"),
        ];

        public SupplierMatch Match(string? powwrId, string? mpid, string? shortCode, string? name) =>
            SupplierMatcher.Match(Aliases, powwrId, mpid, shortCode, name);

        public void Invalidate() { }
    }

    private static MeterSearchResults Read(string json) => JsonSerializer.Deserialize<MeterSearchResults>(json, MeterLookupJson.Options)!;

    private static LookupElectricMeter Electric(Action<ElectricMeter>? change = null)
    {
        var dto = Read(ElectricExample).ElectricMeters![0];
        change?.Invoke(dto);
        return MeterLookupMapper.MapElectric(dto, new Matcher());
    }

    private static LookupGasMeter Gas(Action<GasMeter>? change = null)
    {
        var dto = Read(GasExample).GasMeters![0];
        change?.Invoke(dto);
        return MeterLookupMapper.MapGas(dto, new Matcher());
    }

    // ── Addresses ──

    [Fact]
    public void AddressesKeepTheirIdsCountsAndRedactedHints()
    {
        var results = JsonSerializer.Deserialize<AddressSearchResults>(AddressExample, MeterLookupJson.Options);
        var address = Assert.Single(MeterLookupMapper.MapAddresses(results));

        Assert.Equal("U10023061032A0085759074G1M1610028163246N2PM224DB", address.MeterAddressId);
        Assert.Equal("PARKWAY HOUSE, PALATINE ROAD, MANCHESTER, M22 4DB", address.FullAddress);
        Assert.Equal(2, address.ElectricMeterCount);
        Assert.Equal(1, address.GasMeterCount);
        Assert.Equal(["**********273", "**********231", "*****001"], address.RedactedNumbers);
        Assert.Equal(2, address.MeterCountFor(Fuel.Electricity));
    }

    // ── Electricity ──

    [Fact]
    public void ProfileClassAndMtcBecomeIntegers()
    {
        var meter = Electric();
        Assert.Equal(3, meter.ProfileClass);
        Assert.Equal(801, meter.MeterTimeswitchClass);
        Assert.Equal("631", meter.LineLossFactorClass);
        Assert.Equal("0393", meter.StandardSettlementConfig);
        Assert.Equal("A", meter.MeasurementClass);
        Assert.Empty(meter.FieldNotes);
    }

    [Fact]
    public void NonNumericProfileClassIsBlankWithANote()
    {
        var meter = Electric(m => m.ProfileClass = "X3");
        Assert.Null(meter.ProfileClass);
        Assert.Single(meter.FieldNotes);
        Assert.Null(meter.DerivedTopLine);
    }

    [Fact]
    public void FullMpanComesFromTheMeterSearchAndIsValidated()
    {
        var meter = Electric();
        Assert.Equal("1610028163273", meter.MpanCore);
        Assert.True(meter.IsMpanValid);

        var bad = Electric(m => m.Mpan = "1610028163274");
        Assert.False(bad.IsMpanValid);
        Assert.NotEmpty(bad.FieldNotes);
    }

    [Fact]
    public void LegacyTopLineIsDerivedWhenAllItsPartsArePresent()
    {
        Assert.Equal("03801631", Electric().DerivedTopLine);
        Assert.Null(Electric(m => m.LineLossFactor = null).DerivedTopLine);
    }

    [Fact]
    public void SmetsVersionSuggestsASmartMeter()
    {
        Assert.True(Electric().SmartMeterSuggested);
        Assert.False(Electric(m => m.SmetsVersion = "").SmartMeterSuggested);
    }

    [Fact]
    public void BlankSscBecomesNull()
    {
        Assert.Null(Electric(m => m.StandardSettlementConfiguration = "").StandardSettlementConfig);
    }

    [Fact]
    public void CurrentSupplierIsMatchedToThePricingName()
    {
        var supplier = Electric().CurrentSupplier!;
        Assert.Equal(1, supplier.SupplierId);
        Assert.Equal("British Gas", supplier.PricingName);
        Assert.Equal("British Gas Business", supplier.NameAsReturned);
    }

    [Fact]
    public void UnmatchedSupplierIsWarned()
    {
        var meter = Electric(m => m.CurrentSupplier = new Supplier { Name = "Nobody Energy", PowwrId = "NOBODY-001-S" });
        Assert.Null(meter.CurrentSupplier!.SupplierId);
        Assert.Equal("Nobody Energy", meter.CurrentSupplier.NameAsReturned);
        Assert.Contains(meter.Warnings, w => w.Code == SiteWarningCode.SupplierNotMapped && w.Message.Contains("Nobody Energy"));
    }

    [Fact]
    public void GreenDealActiveIsWarned()
    {
        Assert.Contains(Electric().Warnings, w => w.Code == SiteWarningCode.GreenDealActive);
        Assert.DoesNotContain(Electric(m => m.GreenDealStatus = "Expired").Warnings, w => w.Code == SiteWarningCode.GreenDealActive);
    }

    [Fact]
    public void DomesticMeterIsWarnedAndNeedsConfirmation()
    {
        var warning = Assert.Single(Electric(m => { m.IsDomesticConsumer = true; m.GreenDealStatus = "None"; }).Warnings);
        Assert.Equal(SiteWarningCode.Domestic, warning.Code);
        Assert.False(warning.ConfirmedByAgent);
    }

    [Fact]
    public void DeEnergisedMeterIsWarned()
    {
        Assert.Contains(Electric(m => m.EnergisationStatus = "D").Warnings, w => w.Code == SiteWarningCode.DeEnergised);
        Assert.DoesNotContain(Electric().Warnings, w => w.Code == SiteWarningCode.DeEnergised);
    }

    [Fact]
    public void NoElectricitySwitchingWarningUntilPowwrNamesAField()
    {
        // FR-012 and open item P-16: tradingStatus isn't explained, so it's stored, not interpreted.
        var meter = Electric();
        Assert.Equal("T", meter.TradingStatus);
        Assert.DoesNotContain(meter.Warnings, w => w.Code == SiteWarningCode.SwitchInProgress);
    }

    [Fact]
    public void AddressLinesAndPostcodeComeFromTheMeterPointLocation()
    {
        var address = Electric().Address!;
        Assert.Equal("1ST FLOOR EAST WING", address.Line1);
        Assert.Equal("PARKWAY HOUSE", address.Line2);
        Assert.Equal("PALATINE ROAD", address.Line3);
        Assert.Equal("MANCHESTER", address.Town);
        Assert.Equal("M22 4DB", address.Postcode);
        Assert.Equal(12345, Electric().AnnualConsumptionKwh);
    }

    // ── Gas ──

    [Fact]
    public void GasMeterFieldsAreMapped()
    {
        var meter = Gas();
        Assert.Equal("14097001", meter.Mprn);
        Assert.True(meter.IsMprnValid);
        Assert.Equal("NW", meter.Ldz);
        Assert.Equal("NW2", meter.ExitZone);
        Assert.Equal(6, meter.MeterCapacity);
        Assert.Equal(100, meter.DailyMeteredQuantity);
        Assert.Equal(54321, meter.AnnualQuantityKwh);
        Assert.Equal("Scottish Power", meter.CurrentSupplier!.PricingName);
        Assert.Equal("PARKWAY HOUSE", meter.Address!.Line1);
        Assert.Equal("MANCHESTER", meter.Address.Town);
    }

    [Fact]
    public void AmrIndicatorYSuggestsASmartMeter()
    {
        Assert.False(Gas().SmartMeterSuggested);
        var smart = Gas(m => m.AdditionalInformation!["amrIndicator"] = JsonSerializer.SerializeToElement("Y"));
        Assert.True(smart.SmartMeterSuggested);
        Assert.Equal("Y", smart.AmrIndicator);
    }

    [Fact]
    public void IgtSiteIsWarned()
    {
        Assert.DoesNotContain(Gas().Warnings, w => w.Code == SiteWarningCode.Igt);
        var igt = Gas(m => m.IsIgt = true);
        Assert.True(igt.IsIgt);
        Assert.Contains(igt.Warnings, w => w.Code == SiteWarningCode.Igt);
    }

    [Fact]
    public void IncomingSupplierMeansASwitchIsInProgress()
    {
        var warning = Assert.Single(Gas().Warnings, w => w.Code == SiteWarningCode.SwitchInProgress);
        Assert.Contains("E.ON Next", warning.Message);
        Assert.DoesNotContain(Gas(m => m.IncomingSupplier = null).Warnings, w => w.Code == SiteWarningCode.SwitchInProgress);
    }

    [Fact]
    public void MetersForBothFuelsComeBackSeparately()
    {
        var both = MeterLookupMapper.MapMeters(new MeterSearchResults
        {
            ElectricMeters = Read(ElectricExample).ElectricMeters,
            GasMeters = Read(GasExample).GasMeters,
        }, new Matcher());
        Assert.Single(both.Electric);
        Assert.Single(both.Gas);
    }

    // ── RFC 7807 ──

    [Fact]
    public void ProblemDetailsGiveASafeSummaryAndALogDetailWithoutProvidedValues()
    {
        const string body = """
            {"type":"https://httpstatuses.com/400","title":"Bad Request","status":400,"detail":"Error validating request against JSON Schema for type AddressSearchRequest.","extensions":{"errors":[{"entity":"AddressSearchRequest","description":"Schema Error: (2, 18) StringExpected","type":"ValueIsInvalid","properties":[{"path":"$.postCode","providedValue":"ZZ9 PRIVATE"}]}]}}
            """;

        var (agentMessage, logDetail) = MeterLookupMapper.SummariseProblem(body);

        Assert.Equal(MeterLookupMapper.RejectedMessage, agentMessage);
        Assert.Contains("Bad Request", logDetail);
        Assert.Contains("Schema Error: (2, 18) StringExpected", logDetail);
        Assert.Contains("$.postCode", logDetail);
        Assert.DoesNotContain("ZZ9 PRIVATE", logDetail);
        Assert.DoesNotContain("ZZ9 PRIVATE", agentMessage);
    }

    [Fact]
    public void UnreadableProblemStillGivesTheSafeSummary()
    {
        var (agentMessage, _) = MeterLookupMapper.SummariseProblem("<html>oops</html>");
        Assert.Equal(MeterLookupMapper.RejectedMessage, agentMessage);
    }
}
