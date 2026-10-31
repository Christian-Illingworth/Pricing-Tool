using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using PricingTool.Domain;
using PricingTool.Powwr.Pricing.Dtos;

namespace PricingTool.Powwr.Pricing;

// The response, mapped and checked: what the adapter hands back, plus warnings for the log.
public sealed record MappedQuoteResult(
    bool Succeeded,
    ErrorCategory? ErrorCategory,
    string? ErrorMessage,
    string? ErrorDetail,
    string? Reference,
    string? FooterNotes,
    IReadOnlyList<ReturnedRate> Rates,
    IReadOnlyList<SupplierNoQuote> NoQuotes,
    IReadOnlyList<string> Warnings);

// Turns our domain into POWWR's request and back (contracts/powwr-uk-pricing-mapping.md, research R4).
// Pure and static, so every mapping rule is unit-tested without HTTP.
public static class PowwrPricingMapper
{
    public const string MaskedLicence = "***";
    public const string InvalidResponseReason = "Couldn't be priced: invalid response";

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    // ── Requests ──

    public static GetElectricRates ToElectricRequest(ElectricPriceRequest request, string licenceCode, IReadOnlyDictionary<string, string> businessTypes)
    {
        var details = QuoteDetailsFor(request, licenceCode, businessTypes);
        details.ElectricSupply = new ElectricSupply
        {
            MPANTop = request.MpanTopLine,
            MPANBottom = request.MpanCore,
            ProfileClass = request.ProfileClass,
            MeterTimeswitchClass = request.MeterTimeswitchClass,
            LineLossFactorClass = request.LineLossFactorClass,
            StandardSettlementConfig = request.StandardSettlementConfig,
            DuosTariffId = request.DuosTariffId,
            MhhsMigrated = request.MhhsMigrated,
            ForcedRateStructure = request.ForcedRateStructure is { } forced ? ForcedRateStructureText(forced) : null,
            MeasurementClass = request.MeasurementClass,
            MeterType = request.MeterType,
            SmartMeter = request.SmartMeter,
            DayConsumption = Amount(request.DayKwh),
            NightConsumption = Amount(request.NightKwh),
            WendConsumption = Amount(request.WendKwh),
            KvaConsumption = Amount(request.KvaCapacity),
            KvarhConsumption = Amount(request.KvarhConsumption),
            ContractRenewalDate = Date(request.StartDate),
            ContractEndDate = Date(request.CurrentContractEndDate),
            NewContractEndDate = NewContractEndDate(request),
            CurrentSupplier = request.CurrentSupplierPricingName,
        };
        return new GetElectricRates { QuoteDetails = details };
    }

    public static GetGasRates ToGasRequest(GasPriceRequest request, string licenceCode, IReadOnlyDictionary<string, string> businessTypes)
    {
        var details = QuoteDetailsFor(request, licenceCode, businessTypes);
        details.GasSupply = new GasSupply
        {
            MPR = request.Mprn,
            Consumption = Amount(request.AnnualQuantityKwh),
            SmartMeter = request.SmartMeter,
            ContractRenewalDate = Date(request.StartDate),
            ContractEndDate = Date(request.CurrentContractEndDate),
            NewContractEndDate = NewContractEndDate(request),
            CurrentSupplier = request.CurrentSupplierPricingName,
        };
        return new GetGasRates { QuoteDetails = details };
    }

    // Research R16: the 8-digit top line goes as MPANTop; a 9-character one is left out (the definition
    // allows only 8 there, Q15) and its parts go in the individual fields.
    public static GetNumberOfPrompts ToPromptsRequest(RateStructureRequest request, string licenceCode) => new()
    {
        PromptsSearch = new PromptsSearch
        {
            SecurityDetails = new SecurityDetails { LicenceCode = licenceCode },
            MPANTop = request.MpanTopLine is { Length: 8 } legacy ? legacy : null,
            MPANBottom = request.MpanCore,
            ProfileClass = request.ProfileClass,
            MeterTimeswitchClass = request.MeterTimeswitchClass,
            LineLossFactorClass = request.LineLossFactorClass,
            StandardSettlementConfig = request.StandardSettlementConfig,
            DuosTariffId = request.DuosTariffId,
            MhhsMigrated = request.MhhsMigrated,
        },
    };

    public static GetLatestPriceChange ToPriceChangeRequest(string? supplierName, string licenceCode) => new()
    {
        PriceChangeRequest = new PriceChangeRequest
        {
            SecurityDetails = new SecurityDetails { LicenceCode = licenceCode },
            SupplierName = supplierName,
            IncludeFuture = false,
        },
    };

    public static string Serialize<T>(T request) => JsonSerializer.Serialize(request, PowwrJson.Options);

    // The copy that's stored and logged: every LicenceCode, at any depth, replaced with ***.
    public static string SerializeMasked<T>(T request)
    {
        var node = JsonSerializer.SerializeToNode(request, PowwrJson.Options);
        Mask(node);
        return node?.ToJsonString() ?? "null";
    }

    private static QuoteDetails QuoteDetailsFor(PriceRequest request, string licenceCode, IReadOnlyDictionary<string, string> businessTypes) => new()
    {
        QuoteReference = request.QuoteReference,
        SecurityDetails = new SecurityDetails { LicenceCode = licenceCode, Mascaradeuser = request.AgentName },
        PostCode = request.Postcode,
        Renewal = request.QuoteType == QuoteType.Renewal,
        COT = request.QuoteType == QuoteType.ChangeOfTenancy,
        CotDate = request.QuoteType == QuoteType.ChangeOfTenancy ? Date(request.CotDate) : null,
        OutOfContract = request.OutOfContract,
        CurrentSupplier = request.CurrentSupplierPricingName,
        PaymentMethod = request.PaymentMethod,
        BusinessType = BusinessTypeText.For(request.BusinessType, businessTypes),
        Uplift = request.DefaultUplift.UnitRate,
        StandingChargeUplift = request.DefaultUplift.StandingCharge,
        // PlanType and Id are left out (Q4), so every plan type comes back.
        QuoteDefinitions = request.SupplierPlans.Select(supplier => new QuoteDefinition
        {
            Supplier = supplier.SupplierPricingName,
            Plans = supplier.Plans.Select(plan => new Plan
            {
                Duration = plan.TermMonths,
                Uplift = plan.UnitRateUplift,
                StandingChargeUplift = plan.StandingChargeUplift,
            }).ToList(),
        }).ToList(),
        Settings = [new Setting { Key = "ShowNoQuoteReason", Value = "true" }],
        SortByCommission = false,
    };

    // Assumed to be the new contract's end (Q9): start + term − 1 day, sent only for a single term.
    private static string? NewContractEndDate(PriceRequest request) =>
        request.TermsMonths.Count == 1 ? Date(request.StartDate.AddMonths(request.TermsMonths[0]).AddDays(-1)) : null;

    // The spec's names for the structures (FR-021). POWWR's accepted strings are unconfirmed (Q4).
    private static string ForcedRateStructureText(ForcedRateStructure structure) => structure switch
    {
        ForcedRateStructure.Standard => "Standard",
        ForcedRateStructure.Economy7 => "Economy 7",
        ForcedRateStructure.EveningAndWeekend => "Evening and Weekend",
        ForcedRateStructure.EveningWeekendAndNight => "Evening Weekend and Night",
        ForcedRateStructure.OffPeak => "Off-peak",
        _ => throw new ArgumentOutOfRangeException(nameof(structure), structure, null),
    };

    private static Consumption? Amount(int? kwh) => kwh is > 0 ? new Consumption { Amount = kwh } : null;

    private static string? Date(DateOnly? date) => date?.ToString("yyyy-MM-dd", Invariant);

    private static void Mask(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(p => p.Key).ToList())
                {
                    if (string.Equals(key, nameof(SecurityDetails.LicenceCode), StringComparison.OrdinalIgnoreCase))
                        obj[key] = MaskedLicence;
                    else
                        Mask(obj[key]);
                }
                break;
            case JsonArray array:
                foreach (var item in array) Mask(item);
                break;
        }
    }

    // ── Responses ──

    // The Rate strings that hold numbers; one that doesn't parse makes the rate untrustworthy.
    private static readonly Func<Rate, string?>[] NumericStrings =
    [
        r => r.StandingCharge, r => r.DayUnitrate, r => r.NightUnitrate, r => r.WendUnitrate,
        r => r.Stod1, r => r.Stod2, r => r.Stod3, r => r.Stod4, r => r.Stod5, r => r.Stod6,
        r => r.CapacityCharge, r => r.ExcessCapacityCharge, r => r.ReactiveRate, r => r.FixedFee,
        r => r.TransportationCharge, r => r.Uplift, r => r.StandingChargeUplift,
    ];

    // The HTTP status can't be trusted (the definition only has a default response), so the Error
    // object is always checked. Each rate is checked before it can reach the engine (Principle III).
    public static MappedQuoteResult FromQuoteResult(QuoteResult? result)
    {
        if (result is null)
            return Failed(ErrorCategory.InvalidResponse, "POWWR's response couldn't be read.", null);

        if (!string.IsNullOrWhiteSpace(result.Error?.Message) || !string.IsNullOrWhiteSpace(result.Error?.ErrorDetail))
        {
            var message = string.IsNullOrWhiteSpace(result.Error!.Message) ? "POWWR couldn't price this site." : result.Error.Message.Trim();
            return Failed(Classify(message), message, result.Error.ErrorDetail);
        }

        var rates = new List<ReturnedRate>();
        var noQuotes = new List<SupplierNoQuote>();
        var warnings = new List<string>();

        foreach (var rate in result.Rates ?? [])
        {
            var supplier = rate.Supplier?.Trim();
            var term = int.TryParse(rate.Term, NumberStyles.Integer, Invariant, out var months) ? months : (int?)null;

            if (string.IsNullOrWhiteSpace(supplier))
            {
                warnings.Add("A rate with no supplier name was ignored.");
                continue;
            }
            if (!string.IsNullOrWhiteSpace(rate.NoQuoteReason))
            {
                noQuotes.Add(new SupplierNoQuote(supplier, term, rate.NoQuoteReason.Trim()));
                continue;
            }

            var problem = term is null ? $"its term '{rate.Term}' isn't a number"
                : rate.RawAnnualPrice is null ? "it has no RawAnnualPrice"
                : HasNegativeRawFigure(rate) ? "it has a negative raw figure"
                : NumericStrings.Select(f => f(rate)).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s) && !IsNumber(s)) is { } bad
                    ? $"a rate '{bad}' isn't a number"
                : null;

            if (problem is not null)
            {
                warnings.Add($"Rejected a rate from {supplier}: {problem}.");
                noQuotes.Add(new SupplierNoQuote(supplier, term, InvalidResponseReason, IsInvalidResponse: true));
                continue;
            }

            rates.Add(ToReturnedRate(rate, supplier, term!.Value));
        }

        return new MappedQuoteResult(true, null, null, null, result.Reference, result.FooterNotes, rates, noQuotes, warnings);
    }

    // Research R16: prompt names to registers, case-insensitively. Unknown names are reported, not guessed.
    public static RateStructureResponse FromPrompts(Prompts? prompts)
    {
        if (prompts is null)
            return new RateStructureResponse(false, [], [], "POWWR's rate structure response couldn't be read.", []);
        if (!string.IsNullOrWhiteSpace(prompts.Error?.Message))
            return new RateStructureResponse(false, [], [], prompts.Error.Message.Trim(), []);

        var raw = (prompts.ThePrompts ?? []).Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()).ToList();
        var registers = new HashSet<Register>();
        var unknown = new List<string>();
        foreach (var prompt in raw)
        {
            if (RegisterFor(prompt) is { } register) registers.Add(register);
            else unknown.Add(prompt);
        }

        return registers.Count == 0
            ? new RateStructureResponse(false, [], raw, "POWWR didn't return a rate structure for this meter.", unknown)
            : new RateStructureResponse(true, registers.Order().ToList(), raw, null, unknown);
    }

    public static IReadOnlyList<SupplierPriceChange> FromPriceChanges(PriceChangeResult? result) =>
        (result?.PriceChanges ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c.Supplier))
            .Select(c => new SupplierPriceChange(
                c.Supplier!.Trim(),
                (c.Utility ?? "").Trim().ToLowerInvariant(),
                c.HalfHourly ?? false,
                c.Duration ?? 0,
                c.Product,
                c.PlanType,
                c.Version,
                ParseDate(c.LastPriceChange)))
            .ToList();

    private static Register? RegisterFor(string prompt) => prompt.ToLowerInvariant() switch
    {
        "day" or "unit" or "standard" => Register.Day,
        "night" => Register.Night,
        "evening and weekend" or "evening & weekend" or "weekend" or "wend" => Register.Wend,
        _ => null,
    };

    // POWWR's wording for a rejected licence isn't documented; these words cover the likely messages.
    // Anything else is an ordinary POWWR error. Revisit when POWWR confirms (Q1).
    private static ErrorCategory Classify(string message)
    {
        var lower = message.ToLowerInvariant();
        return lower.Contains("licence") || lower.Contains("license") || lower.Contains("security details") || lower.Contains("unauthori")
            ? ErrorCategory.CredentialsRejected
            : ErrorCategory.PowwrError;
    }

    private static MappedQuoteResult Failed(ErrorCategory category, string message, string? detail) =>
        new(false, category, message, detail, null, null, [], [], []);

    private static bool HasNegativeRawFigure(Rate rate) =>
        rate.RawAnnualPrice < 0 || rate.RawBaseAnnualPrice < 0 || rate.RawAnnualPriceInclusive < 0 || rate.RawCommission < 0;

    private static bool IsNumber(string s) => decimal.TryParse(s.Trim(), NumberStyles.Number, Invariant, out _);

    private static DateOnly? ParseDate(string? value) =>
        DateTime.TryParse(value, Invariant, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed)
            ? DateOnly.FromDateTime(parsed)
            : null;

    private static ReturnedRate ToReturnedRate(Rate r, string supplier, int termMonths) => new()
    {
        Supplier = supplier,
        TermMonths = termMonths,
        PlanType = r.PlanType,
        StandingCharge = r.StandingCharge,
        DayUnitrate = r.DayUnitrate,
        NightUnitrate = r.NightUnitrate,
        WendUnitrate = r.WendUnitrate,
        Fits = r.Fits,
        Stod1 = r.Stod1,
        Stod2 = r.Stod2,
        Stod3 = r.Stod3,
        Stod4 = r.Stod4,
        Stod5 = r.Stod5,
        Stod6 = r.Stod6,
        DirectDebitDiscount = r.DirectDebitDiscount,
        FlatDirectDebitDiscount = r.FlatDirectDebitDiscount,
        ChequeSurcharge = r.ChequeSurcharge,
        ExtraInfo = r.ExtraInfo,
        FooterMessage = r.FooterMessage,
        TariffCode = r.TariffCode,
        AnnualPrice = r.AnnualPrice,
        RawAnnualPrice = r.RawAnnualPrice!.Value,
        BaseAnnualPrice = r.BaseAnnualPrice,
        RawBaseAnnualPrice = r.RawBaseAnnualPrice,
        AnnualPriceInclusive = r.AnnualPriceInclusive,
        RawAnnualPriceInclusive = r.RawAnnualPriceInclusive,
        Ref = r.Ref,
        Period = r.Period,
        SC = r.SC,
        CapacityCharge = r.CapacityCharge,
        ReactiveRate = r.ReactiveRate,
        ExcessCapacityCharge = r.ExcessCapacityCharge,
        Uplift = r.Uplift,
        FixedFee = r.FixedFee,
        FixedFeeUplift = r.FixedFeeUplift,
        FixedFeeSCUplift = r.FixedFeeSCUplift,
        StandingChargeUplift = r.StandingChargeUplift,
        Renewal = r.Renewal,
        RawCommission = r.RawCommission,
        Commission = r.Commission,
        OutsidePricingWindow = r.OutsidePricingWindow ?? false,
        ContractEndDate = r.ContractEndDate,
        TransportationCharge = r.TransportationCharge,
        PricebookVersion = r.PricebookVersion,
        QuoteRateReference = r.QuoteRateReference,
        Id = r.Id,
    };
}
