using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PricingTool.Domain;
using PricingTool.Options;
using PricingTool.Powwr.Pricing.Dtos;
using PricingTool.Services.Time;

namespace PricingTool.Powwr.Pricing;

// The default pricing adapter: realistic, deterministic, made-up prices, so everything can be built
// and tested without POWWR (constitution Principle VI). It behaves exactly as the contract's "Mock
// adapter behaviour" section describes, and builds POWWR-shaped responses that go through the same
// PowwrPricingMapper as the real client.
public sealed class MockPowwrPricingClient(
    TimeProvider time,
    IOptions<PowwrPricingOptions> pricing,
    IOptionsMonitor<PowwrMockOptions> mock) : IPowwrPricingClient
{
    public static readonly IReadOnlyList<string> Suppliers =
    [
        "British Gas", "EDF", "E.ON Next", "Octopus Energy", "Opus Energy",
        "Scottish Power", "Smartest Energy", "Valda Energy", "Crown Gas & Power", "Yu Energy",
    ];

    // Scenario triggers (quickstart.md "Mock data for the scenarios").
    public const string EveryoneDeclinesMpan = "2300000000019";
    public const string PowwrErrorMprn = "9999999";
    public const string MhhsNoSscMpan = "1200000000020";

    private const string MockLicence = "MOCK-LICENCE";
    private const string FooterNotes = "Mock prices: made-up figures for development and testing.";

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public Task<SitePriceResponse> GetElectricPricesAsync(ElectricPriceRequest request, CancellationToken ct)
    {
        var sent = time.GetUtcNow().UtcDateTime;
        var registers = new List<(Register Register, int Kwh)>();
        if (request.DayKwh is > 0) registers.Add((Register.Day, request.DayKwh.Value));
        if (request.NightKwh is > 0) registers.Add((Register.Night, request.NightKwh.Value));
        if (request.WendKwh is > 0) registers.Add((Register.Wend, request.WendKwh.Value));

        var key = $"{request.MpanCore}|{registers.Sum(r => r.Kwh)}";
        var result = request.MpanCore switch
        {
            EveryoneDeclinesMpan => Declined(request, _ => "Unable to quote for this meter"),
            MhhsNoSscMpan => Priced(request, key, Fuel.Electricity, registers, request.KvaCapacity,
                declines: s => s is "British Gas" or "Octopus Energy" ? null : "Can't price a forced rate structure for this meter"),
            _ => Priced(request, key, Fuel.Electricity, registers, request.KvaCapacity, declines: _ => null),
        };

        var dto = PowwrPricingMapper.ToElectricRequest(request, MockLicence, pricing.Value.BusinessTypes);
        return Task.FromResult(Respond(dto, new GetElectricRatesResultWrapper { GetElectricRatesResult = result }, result, sent));
    }

    public Task<SitePriceResponse> GetGasPricesAsync(GasPriceRequest request, CancellationToken ct)
    {
        var sent = time.GetUtcNow().UtcDateTime;
        var result = request.Mprn == PowwrErrorMprn
            ? new QuoteResult { Error = new Error { Message = "Mock POWWR error: this meter couldn't be priced.", ErrorDetail = "MOCK-ERROR-001" } }
            : Priced(request, $"{request.Mprn}|{request.AnnualQuantityKwh}", Fuel.Gas, [(Register.Day, request.AnnualQuantityKwh)], null, declines: _ => null);

        var dto = PowwrPricingMapper.ToGasRequest(request, MockLicence, pricing.Value.BusinessTypes);
        return Task.FromResult(Respond(dto, new GetGasRatesResultWrapper { GetGasRatesResult = result }, result, sent));
    }

    // Single rate unless the meter's settlement configuration or profile class says day and night.
    public Task<RateStructureResponse> GetRateStructureAsync(RateStructureRequest request, CancellationToken ct)
    {
        if (request.MhhsMigrated == true && string.IsNullOrWhiteSpace(request.StandardSettlementConfig))
            return Task.FromResult(PowwrPricingMapper.FromPrompts(new Prompts
            {
                Error = new Error { Message = "No standard settlement configuration for this meter: choose the rate structure." },
            }));

        string[] prompts = request.StandardSettlementConfig switch
        {
            "0393" => ["Unit"],
            "0151" or "0244" => ["Day", "Night"],
            _ when request.ProfileClass is 0 or 2 or 4 => ["Day", "Night"],
            _ => ["Unit"],
        };
        return Task.FromResult(PowwrPricingMapper.FromPrompts(new Prompts { NumberOfPrompts = prompts.Length, ThePrompts = [.. prompts] }));
    }

    // Every supplier's pricebook is today's, unless Powwr:Mock:ChangedSuppliers names it (FR-052 testing).
    public Task<IReadOnlyList<SupplierPriceChange>> GetLatestPriceChangesAsync(string? supplierName, CancellationToken ct)
    {
        var today = UkCalendar.Today(time);
        var changed = new HashSet<string>(mock.CurrentValue.ChangedSuppliers, StringComparer.OrdinalIgnoreCase);
        var changes = new List<LatestPriceChange>();
        foreach (var supplier in Suppliers.Where(s => supplierName is null || string.Equals(s, supplierName, StringComparison.OrdinalIgnoreCase)))
            foreach (var utility in new[] { "electric", "gas" })
                foreach (var halfHourly in new[] { false, true })
                    foreach (var term in new[] { 12, 24, 36, 48, 60 })
                        changes.Add(new LatestPriceChange
                        {
                            Supplier = supplier,
                            Utility = utility,
                            HalfHourly = halfHourly,
                            Duration = term,
                            Product = "Fixed",
                            PlanType = "Fixed",
                            Version = changed.Contains(supplier) ? "MOCK-CHANGED" : PricebookVersion(today),
                            LastPriceChange = today.ToString("yyyy-MM-dd", Invariant),
                        });
        return Task.FromResult(PowwrPricingMapper.FromPriceChanges(new PriceChangeResult { PriceChanges = changes }));
    }

    public Task<bool> PingAsync(CancellationToken ct) => Task.FromResult(true);

    public static string PricebookVersion(DateOnly ukDate) => $"MOCK-{ukDate:yyyyMMdd}";

    private QuoteResult Declined(PriceRequest request, Func<string, string> reason) => new()
    {
        Reference = Reference(request.QuoteReference),
        FooterNotes = FooterNotes,
        Rates = request.SupplierPlans.Select(p => new Rate { Supplier = p.SupplierPricingName, NoQuoteReason = reason(p.SupplierPricingName) }).ToList(),
    };

    // Builds each supplier's rates: base rates from a generator seeded by the meter and its consumption,
    // 2% more per extra year of term, plus the plan's uplift. RawAnnualPrice is exactly
    // Σ(rate × kWh) / 100 + SC × 365 / 100 (+ capacity) from the rate strings, so the cross-check passes.
    private QuoteResult Priced(
        PriceRequest request, string meterKey, Fuel fuel, IReadOnlyList<(Register Register, int Kwh)> registers, int? kva,
        Func<string, string?> declines)
    {
        var today = UkCalendar.Today(time);
        var rates = new List<Rate>();

        foreach (var supplierPlan in request.SupplierPlans)
        {
            var supplier = supplierPlan.SupplierPricingName;
            if (!Suppliers.Contains(supplier))
            {
                rates.Add(new Rate { Supplier = supplier, NoQuoteReason = "Supplier not available in the mock" });
                continue;
            }
            if (supplier == "Valda Energy")
            {
                rates.Add(new Rate { Supplier = supplier, NoQuoteReason = "Meter type not supported" });
                continue;
            }
            if (declines(supplier) is { } reason)
            {
                rates.Add(new Rate { Supplier = supplier, NoQuoteReason = reason });
                continue;
            }

            var random = new Random(StableSeed($"{meterKey}|{supplier}"));
            var baseUnit = fuel == Fuel.Electricity
                ? new Dictionary<Register, decimal>
                {
                    [Register.Day] = Between(random, 22m, 28m),
                    [Register.Night] = Between(random, 14m, 18m),
                    [Register.Wend] = Between(random, 16m, 20m),
                }
                : new Dictionary<Register, decimal> { [Register.Day] = Between(random, 5m, 8m) };
            var baseStanding = decimal.Round(fuel == Fuel.Electricity ? Between(random, 40m, 90m) : Between(random, 25m, 60m), 2);
            const decimal capacityRate = 2.50m;   // p/kVA/day, half-hourly only

            foreach (var plan in supplierPlan.Plans)
            {
                var factor = 1m + 0.02m * (plan.TermMonths / 12 - 1);
                rates.Add(BuildRate(request, supplier, plan, "Fixed", registers, kva, capacityRate, today,
                    baseUnit.ToDictionary(r => r.Key, r => decimal.Round(r.Value * factor, 3)),
                    decimal.Round(baseStanding * factor, 2)));

                // Opus also offers a product with no standing charge, for the FR-026 filter.
                if (supplier == "Opus Energy")
                    rates.Add(BuildRate(request, supplier, plan, "Fixed No Standing Charge", registers, kva, capacityRate, today,
                        baseUnit.ToDictionary(r => r.Key, r => decimal.Round(r.Value * factor + 3m, 3)),
                        0m));
            }
        }

        return new QuoteResult { Reference = Reference(request.QuoteReference), FooterNotes = FooterNotes, Rates = rates };
    }

    private static Rate BuildRate(
        PriceRequest request, string supplier, PlanUplift plan, string planType,
        IReadOnlyList<(Register Register, int Kwh)> registers, int? kva, decimal capacityRate, DateOnly today,
        IReadOnlyDictionary<Register, decimal> baseUnit, decimal baseStanding)
    {
        var unit = baseUnit.ToDictionary(r => r.Key, r => r.Value + plan.UnitRateUplift);
        var standing = planType.EndsWith("No Standing Charge") ? 0m : baseStanding + plan.StandingChargeUplift;
        var standingBase = planType.EndsWith("No Standing Charge") ? 0m : baseStanding;
        var capacity = kva is > 0 ? kva.Value * capacityRate * 365m / 100m : 0m;

        var annual = registers.Sum(r => unit[r.Register] * r.Kwh) / 100m + standing * 365m / 100m + capacity;
        var baseAnnual = registers.Sum(r => baseUnit[r.Register] * r.Kwh) / 100m + standingBase * 365m / 100m + capacity;
        var commission = annual - baseAnnual;

        string? Unit(Register register) =>
            registers.Any(r => r.Register == register) ? unit[register].ToString("0.000#", Invariant) : null;

        return new Rate
        {
            Supplier = supplier,
            Term = plan.TermMonths.ToString(Invariant),
            PlanType = planType,
            DayUnitrate = Unit(Register.Day),
            NightUnitrate = Unit(Register.Night),
            WendUnitrate = Unit(Register.Wend),
            StandingCharge = standing.ToString("0.00##", Invariant),
            CapacityCharge = kva is > 0 ? capacityRate.ToString("0.00", Invariant) : null,
            Uplift = plan.UnitRateUplift.ToString(Invariant),
            StandingChargeUplift = planType.EndsWith("No Standing Charge") ? "0" : plan.StandingChargeUplift.ToString(Invariant),
            AnnualPrice = annual.ToString("0.00", Invariant),
            RawAnnualPrice = annual,
            BaseAnnualPrice = baseAnnual.ToString("0.00", Invariant),
            RawBaseAnnualPrice = baseAnnual,
            RawCommission = commission,
            Commission = commission.ToString("0.00", Invariant),
            OutsidePricingWindow = supplier == "Yu Energy" && plan.TermMonths == 60,
            FooterMessage = supplier == "Octopus Energy" ? "Mock note: Octopus Energy asks for a smart meter within six months." : null,
            ContractEndDate = request.StartDate.AddMonths(plan.TermMonths).AddDays(-1).ToString("yyyy-MM-dd", Invariant),
            Period = $"{plan.TermMonths} months",
            TariffCode = $"MOCK-{Abbreviation(supplier)}-{plan.TermMonths}",
            PricebookVersion = PricebookVersion(today),
            QuoteRateReference = $"MOCK-{Abbreviation(supplier)}-{plan.TermMonths}-{(planType.EndsWith("No Standing Charge") ? "NSC" : "STD")}",
            Renewal = request.QuoteType == QuoteType.Renewal,
        };
    }

    private static SitePriceResponse Respond<TRequest, TWrapper>(TRequest requestDto, TWrapper wrapper, QuoteResult result, DateTime sent)
    {
        var mapped = PowwrPricingMapper.FromQuoteResult(result);
        return new SitePriceResponse
        {
            Succeeded = mapped.Succeeded,
            ErrorCategory = mapped.ErrorCategory,
            ErrorMessage = mapped.ErrorMessage,
            PowwrReference = mapped.Reference,
            FooterNotes = mapped.FooterNotes,
            Rates = mapped.Rates,
            NoQuotes = mapped.NoQuotes,
            RequestJsonMasked = PowwrPricingMapper.SerializeMasked(requestDto),
            ResponseJson = JsonSerializer.Serialize(wrapper, PowwrJson.Options),
            SentUtc = sent,
            ReceivedUtc = sent,
        };
    }

    private static decimal Between(Random random, decimal min, decimal max) =>
        decimal.Round(min + (decimal)random.NextDouble() * (max - min), 3);

    private static string Reference(string quoteReference) => $"MOCK-REF-{StableSeed(quoteReference) % 1_000_000:000000}";

    private static string Abbreviation(string supplier) =>
        new(supplier.Where(char.IsLetter).Take(4).Select(char.ToUpperInvariant).ToArray());

    // FNV-1a over UTF-8: the same input gives the same seed on every machine and run, unlike GetHashCode.
    private static int StableSeed(string value)
    {
        var hash = 2166136261u;
        foreach (var b in Encoding.UTF8.GetBytes(value))
        {
            hash ^= b;
            hash *= 16777619u;
        }
        return (int)(hash & 0x7FFFFFFF);
    }
}
