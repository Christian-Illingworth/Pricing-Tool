using System.Globalization;
using System.Text.Json;
using PricingTool.Domain;
using PricingTool.Powwr.MeterLookup.Dtos;
using PricingTool.Services.Settings;

namespace PricingTool.Powwr.MeterLookup;

// Turns Meter Lookup responses into the domain models (contracts/powwr-meter-lookup-mapping.md,
// research R5). The lookup's data is external input (Principle III): numbers are validated, strings
// converted, and anything unusable is left blank with a note rather than prefilled.
public static class MeterLookupMapper
{
    public const string RejectedMessage = "The meter lookup couldn't use that search. You can enter the details yourself.";

    public static IReadOnlyList<LookupAddress> MapAddresses(AddressSearchResults? results) =>
        (results?.MeterAddresses ?? [])
            .Where(a => !string.IsNullOrWhiteSpace(a.MeterAddressId))
            .Select(a => new LookupAddress(
                a.MeterAddressId!,
                a.FullAddress?.Trim() ?? "",
                a.MpanCount ?? a.Mpans?.Count ?? 0,
                a.MprnCount ?? a.Mprns?.Count ?? 0,
                [.. a.Mpans ?? [], .. a.Mprns ?? []]))
            .ToList();

    public static LookupMeters MapMeters(MeterSearchResults? results, ISupplierMatcher suppliers) => new(
        (results?.ElectricMeters ?? []).Where(m => !string.IsNullOrWhiteSpace(m.Mpan)).Select(m => MapElectric(m, suppliers)).ToList(),
        (results?.GasMeters ?? []).Where(m => !string.IsNullOrWhiteSpace(m.Mprn)).Select(m => MapGas(m, suppliers)).ToList());

    public static LookupElectricMeter MapElectric(ElectricMeter meter, ISupplierMatcher suppliers)
    {
        var notes = new List<string>();
        var warnings = new List<SiteWarning>();

        var isMpanValid = MpanCore.TryParse(meter.Mpan, out var core, out _);
        if (!isMpanValid)
            notes.Add($"The lookup returned MPAN {meter.Mpan}, which fails validation, so it wasn't filled in.");

        var profileClass = ParseNumber(meter.ProfileClass, "profile class", notes);
        var mtc = ParseNumber(meter.MeterTimeswitchClass, "meter time-switch code", notes);
        var llfc = Blank(meter.LineLossFactor);

        if (meter.IsDomesticConsumer == true)
            warnings.Add(new SiteWarning(SiteWarningCode.Domestic,
                "The lookup says this is a domestic meter. Confirm it's being supplied to a business before pricing."));
        if (string.Equals(meter.EnergisationStatus?.Trim(), "D", StringComparison.OrdinalIgnoreCase))
            warnings.Add(new SiteWarning(SiteWarningCode.DeEnergised, "The meter is de-energised."));
        if (string.Equals(meter.GreenDealStatus?.Trim(), "Active", StringComparison.OrdinalIgnoreCase))
            warnings.Add(new SiteWarning(SiteWarningCode.GreenDealActive, "There's an active Green Deal plan on this meter."));

        var supplier = MatchSupplier(meter.CurrentSupplier, meter.CurrentSupplierName, suppliers, warnings);

        return new LookupElectricMeter
        {
            MpanCore = core?.Value ?? meter.Mpan!.Trim(),
            IsMpanValid = isMpanValid,
            ProfileClass = profileClass,
            MeterTimeswitchClass = mtc,
            LineLossFactorClass = llfc,
            StandardSettlementConfig = Blank(meter.StandardSettlementConfiguration),
            MeasurementClass = Blank(meter.MeasurementClass),
            MeterType = Blank(meter.MeterType),
            SmetsVersion = Blank(meter.SmetsVersion),
            SmartMeterSuggested = !string.IsNullOrWhiteSpace(meter.SmetsVersion),
            GspId = Blank(meter.GspId),
            EnergisationStatus = Blank(meter.EnergisationStatus),
            TradingStatus = Blank(meter.TradingStatus),
            AnnualConsumptionKwh = meter.AnnualConsumption is > 0 ? meter.AnnualConsumption : null,
            DerivedTopLine = profileClass is >= 0 and <= 99 && mtc is >= 0 and <= 999 && llfc is { Length: 3 }
                ? $"{profileClass:00}{mtc:000}{llfc.ToUpperInvariant()}"
                : null,
            Address = MapAddress(meter.MeterPointLocation),
            CurrentSupplier = supplier,
            Warnings = warnings,
            FieldNotes = notes,
        };
    }

    public static LookupGasMeter MapGas(GasMeter meter, ISupplierMatcher suppliers)
    {
        var notes = new List<string>();
        var warnings = new List<SiteWarning>();

        var isMprnValid = Mprn.TryParse(meter.Mprn, out var mprn, out _);
        if (!isMprnValid)
            notes.Add($"The lookup returned MPRN {meter.Mprn}, which fails validation, so it wasn't filled in.");

        if (meter.IsIgt == true)
            warnings.Add(new SiteWarning(SiteWarningCode.Igt,
                "This site is on an independent gas transporter (IGT) network. Some suppliers may not price it."));

        var current = MatchSupplier(meter.CurrentSupplier, meter.CurrentSupplierName, suppliers, warnings);

        LookupSupplier? incoming = null;
        if (meter.IncomingSupplier is { } incomingDto && HasAny(incomingDto))
        {
            var match = suppliers.Match(incomingDto.PowwrId, incomingDto.Mpid, incomingDto.ShortCode, incomingDto.Name);
            incoming = new LookupSupplier(match.SupplierId, match.PricingName, Blank(incomingDto.Name));
            warnings.Add(new SiteWarning(SiteWarningCode.SwitchInProgress,
                $"A switch to {match.PricingName ?? incoming.NameAsReturned ?? "another supplier"} is already in progress."));
        }

        var amr = Text(meter.AdditionalInformation, "amrIndicator");

        return new LookupGasMeter
        {
            Mprn = mprn?.Value ?? meter.Mprn!.Trim(),
            IsMprnValid = isMprnValid,
            Ldz = Blank(meter.Ldz),
            ExitZone = Blank(meter.ExitZone),
            IsIgt = meter.IsIgt == true,
            MeterCapacity = meter.MeterCapacity,
            DailyMeteredQuantity = meter.DailyMeteredQuantity,
            AnnualQuantityKwh = meter.AnnualConsumption is > 0 ? meter.AnnualConsumption : null,
            AmrIndicator = amr,
            SmartMeterSuggested = string.Equals(amr, "Y", StringComparison.OrdinalIgnoreCase),
            MeterType = Blank(meter.MeterType),
            MeterModel = Blank(meter.MeterModel),
            MeterMechanismCode = Blank(meter.MeterMechanismCode),
            Address = MapAddress(meter.MeterPointLocation),
            CurrentSupplier = current,
            IncomingSupplier = incoming,
            Warnings = warnings,
            FieldNotes = notes,
        };
    }

    // A 400 from the lookup: a fixed, safe message for the agent, and the title, detail, error
    // descriptions and property paths for the log. Provided values are never logged: they may be
    // personal data.
    public static (string AgentMessage, string LogDetail) SummariseProblem(string? body)
    {
        ProblemDetails? problem = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(body)) problem = JsonSerializer.Deserialize<ProblemDetails>(body, MeterLookupJson.Options);
        }
        catch (JsonException)
        {
        }

        if (problem is null) return (RejectedMessage, "The meter lookup rejected the request with a body that isn't RFC 7807.");

        var issues = (problem.Extensions?.Errors ?? [])
            .Select(e => $"{e.Description} ({string.Join(", ", (e.Properties ?? []).Select(p => p.Path))})");
        return (RejectedMessage, $"{problem.Title}: {problem.Detail} {string.Join("; ", issues)}".Trim());
    }

    // Electricity locations come as numbered lines (the last two are town and county); gas locations
    // as named parts. Either way the agent can correct the result (FR-011).
    private static LookupSiteAddress? MapAddress(MeterPointLocation? location)
    {
        if (location is null) return null;

        var numbered = new[]
            {
                location.AddressLine1, location.AddressLine2, location.AddressLine3, location.AddressLine4,
                location.AddressLine5, location.AddressLine6, location.AddressLine7,
            }
            .Select(Blank).OfType<string>().ToList();

        var lines = numbered.Count > 0
            ? numbered
            : new[]
                {
                    location.SubBuilding, location.BuildingName,
                    Blank($"{location.BuildingNumber} {location.Thoroughfare}"),
                    location.DependentThoroughfare, location.DependentLocality,
                }
                .Select(Blank).OfType<string>().ToList();

        return new LookupSiteAddress(
            lines.ElementAtOrDefault(0),
            lines.ElementAtOrDefault(1),
            lines.ElementAtOrDefault(2),
            lines.ElementAtOrDefault(3),
            Blank(location.AddressLine8) ?? Blank(location.Locality),
            Blank(location.PostCode),
            Blank(location.FullAddress?.TrimEnd(',', ' ')));
    }

    private static LookupSupplier? MatchSupplier(Supplier? supplier, string? deprecatedName, ISupplierMatcher suppliers, List<SiteWarning> warnings)
    {
        var name = Blank(supplier?.Name) ?? Blank(deprecatedName);
        if ((supplier is null || !HasAny(supplier)) && name is null) return null;

        var match = suppliers.Match(supplier?.PowwrId, supplier?.Mpid, supplier?.ShortCode, name);
        if (match.SupplierId is null)
            warnings.Add(new SiteWarning(SiteWarningCode.SupplierNotMapped,
                $"The current supplier \"{name ?? supplier?.PowwrId}\" isn't in the supplier list. Ask an admin to map it, or pick the supplier yourself."));
        return new LookupSupplier(match.SupplierId, match.PricingName, name);
    }

    private static bool HasAny(Supplier s) =>
        !string.IsNullOrWhiteSpace(s.PowwrId) || !string.IsNullOrWhiteSpace(s.Mpid)
        || !string.IsNullOrWhiteSpace(s.ShortCode) || !string.IsNullOrWhiteSpace(s.Name);

    // "03" → 3. Anything that isn't a number is treated as missing and the agent is told.
    private static int? ParseNumber(string? value, string label, List<string> notes)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (int.TryParse(value.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var number)) return number;
        notes.Add($"The lookup's {label} \"{value}\" isn't a number, so it was left blank.");
        return null;
    }

    private static string? Text(Dictionary<string, JsonElement>? values, string key) =>
        values?.FirstOrDefault(kv => string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase)).Value is { ValueKind: JsonValueKind.String } element
            ? Blank(element.GetString())
            : null;

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
