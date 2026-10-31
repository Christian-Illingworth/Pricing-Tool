using PricingTool.Domain;
using PricingTool.Powwr.Pricing;

namespace PricingTool.Services.Pricing;

// ── Input ──

// One site to price: its start date, consumption by register (gas uses Day for the AQ), the agent's
// current rates, and POWWR's response. Response is null for a site that wasn't priced (FR-045).
public sealed record EngineSite(
    int SiteKey,
    int Ordinal,
    DateOnly StartDate,
    IReadOnlyDictionary<Register, int> RegisterKwh,
    int? KvaCapacity,
    int? KvarhConsumption,
    CurrentRates Current,
    SitePriceResponse? Response)
{
    public int AnnualKwh => RegisterKwh.Values.Sum();
}

// The uplift the agent asked for and the one sent, for one supplier and term (FR-062).
public sealed record EngineUplift(
    string Supplier,
    int TermMonths,
    decimal RequestedUnitRate,
    decimal RequestedStandingCharge,
    decimal UnitRate,
    decimal StandingCharge)
{
    public bool IsCapped => UnitRate < RequestedUnitRate || StandingCharge < RequestedStandingCharge;
}

public sealed record PricingEngineInput(
    Fuel Fuel,
    VatCclTreatment Treatment,
    decimal? QualifyingPercent,
    bool DeclaredFewerThan10Employees,
    bool DeclaredTurnoverOrBalanceUnder2m,
    IReadOnlyList<EngineSite> Sites,
    IReadOnlyList<EngineUplift> Uplifts,
    PricingSettingsSnapshot Settings);

// ── Output: everything PricingRunSite, PriceOption and PriceOptionSite store, at full precision ──

public sealed record SiteResult(
    int SiteKey,
    DateOnly StartDateUsed,
    SiteTreatment Treatment,
    decimal QualifyingFraction,
    decimal VatStandardRate,
    decimal VatReducedRate,
    decimal CclRatePencePerKwh,
    int AnnualKwh,
    decimal? CurrentAnnualCostExVatCcl,
    decimal? CurrentAnnualCostIncVatCcl);

public sealed record OptionSiteResult(
    int SiteKey,
    ReturnedRate Rate,
    decimal AnnualCostExVatCcl,
    decimal Ccl,
    decimal Vat,
    decimal AnnualCostIncVatCcl,
    decimal? SavingExVatCcl,
    decimal? SavingIncVatCcl,
    decimal CommissionContractTotal,
    decimal CommissionUpfront,
    decimal CommissionMonthly,
    decimal? BaseDayRate,
    decimal? BaseNightRate,
    decimal? BaseWendRate,
    decimal? BaseStandingCharge,
    decimal CrossCheckAnnualCost,
    decimal CrossCheckDifference);

public sealed record OptionResult(
    string Supplier,
    int? SupplierId,
    string? PlanType,
    int TermMonths,
    decimal RequestedUnitRateUplift,
    decimal RequestedStandingChargeUplift,
    decimal UnitRateUplift,
    decimal StandingChargeUplift,
    bool IsCappedAtSupplierMax,
    int SitesCovered,
    int SitesTotal,
    bool IsPartial,
    bool AnyOutsidePricingWindow,
    bool HasNoStandingCharge,
    bool? IsGreen,
    decimal TotalAnnualCostExVatCcl,
    decimal TotalCcl,
    decimal TotalVat,
    decimal TotalAnnualCostIncVatCcl,
    decimal? TotalSavingExVatCcl,
    decimal? TotalSavingIncVatCcl,
    decimal CommissionContractTotal,
    decimal CommissionUpfront,
    decimal CommissionMonthly,
    IReadOnlyList<OptionSiteResult> Sites);

public sealed record PricingResult(
    CommissionBasis CommissionBasis,
    MicrobusinessResult Microbusiness,
    IReadOnlyList<SiteResult> Sites,
    IReadOnlyList<OptionResult> Options,
    IReadOnlyList<string> Warnings);

// The single pricing engine (constitution Principle VI, research R7). Stateless: it takes the mapped
// POWWR rates, the site inputs and the settings snapshot, and returns every figure the screen, the PDF
// and reporting will show, at full decimal precision. It does no rounding; the figures are stored, and
// displays round them with Rounding.ToPennies.
public sealed class PricingEngine(ILogger<PricingEngine> log)
{
    public PricingResult Calculate(PricingEngineInput input)
    {
        var settings = input.Settings;
        var warnings = new List<string>();
        var uplifts = input.Uplifts.ToDictionary(u => (u.Supplier.ToUpperInvariant(), u.TermMonths));

        var microbusiness = MicrobusinessTest.Evaluate(
            input.Sites.Select(s => s.AnnualKwh),
            settings.MicrobusinessThresholdKwh,
            input.DeclaredFewerThan10Employees,
            input.DeclaredTurnoverOrBalanceUnder2m);

        var siteResults = new List<SiteResult>();
        var figures = new List<SiteOptionFigures>();
        var details = new Dictionary<SiteOptionFigures, OptionSiteResult>(ReferenceEqualityComparer.Instance);

        foreach (var site in input.Sites)
        {
            var rates = settings.RatesFor(site.StartDate);
            var (treatment, q) = VatCclCalculator.DecideTreatment(input.Treatment, input.QualifyingPercent, site.AnnualKwh, rates);
            var currentEx = SavingCalculator.CurrentAnnualCost(site.RegisterKwh, site.Current);
            var currentInc = currentEx is { } ce ? VatCclCalculator.Apply(ce, site.AnnualKwh, treatment, q, rates).Total : (decimal?)null;

            siteResults.Add(new SiteResult(site.SiteKey, site.StartDate, treatment, q, rates.VatStandardPercent,
                rates.VatReducedPercent, rates.CclPencePerKwh, site.AnnualKwh, currentEx, currentInc));

            if (site.Response is not { Succeeded: true } response) continue;

            foreach (var rate in response.Rates)
            {
                var cost = rate.RawAnnualPrice;   // FR-034: POWWR's annual price, which includes every charge
                var taxed = VatCclCalculator.Apply(cost, site.AnnualKwh, treatment, q, rates);
                var upfrontPercent = settings.Supplier(rate.Supplier)?.UpfrontPercent ?? 0m;
                var commission = CommissionCalculator.Split(rate.RawCommission ?? 0m, settings.CommissionBasis, rate.TermMonths, upfrontPercent);
                var check = AnnualCostCrossCheck.Check(rate, site.RegisterKwh, site.KvaCapacity, site.KvarhConsumption);

                if (check.IsSignificant)
                {
                    var warning = $"Cross-check for site {site.Ordinal}, {rate.Supplier} {rate.TermMonths} months: POWWR's annual price "
                        + $"{cost} differs from the recomputed {check.Recomputed} by {check.Difference}.";
                    warnings.Add(warning);
                    log.LogWarning("{CrossCheck}", warning);
                }
                LogCommissionRatio(rate);

                var figure = new SiteOptionFigures(
                    site.SiteKey, rate, cost, taxed.Ccl, taxed.Vat, taxed.Total,
                    currentEx is { } ex ? SavingCalculator.Saving(ex, cost) : null,
                    currentInc is { } inc ? SavingCalculator.Saving(inc, taxed.Total) : null,
                    commission);
                figures.Add(figure);
                details[figure] = new OptionSiteResult(
                    site.SiteKey, rate, figure.AnnualCostExVatCcl, figure.Ccl, figure.Vat, figure.AnnualCostIncVatCcl,
                    figure.SavingExVatCcl, figure.SavingIncVatCcl,
                    commission.ContractTotal, commission.Upfront, commission.Monthly,
                    BaseRates.Compute(rate.DayUnitrate, rate.Uplift),
                    BaseRates.Compute(rate.NightUnitrate, rate.Uplift),
                    BaseRates.Compute(rate.WendUnitrate, rate.Uplift),
                    BaseRates.Compute(rate.StandingCharge, rate.StandingChargeUplift),
                    check.Recomputed,
                    check.Difference);
            }
        }

        var options = MultiSiteCombiner.Combine(figures, input.Sites.Count).Select(combined =>
        {
            var uplift = uplifts.GetValueOrDefault((combined.Supplier.ToUpperInvariant(), combined.TermMonths))
                ?? FromReturnedRate(combined.Sites[0].Rate);
            return new OptionResult(
                combined.Supplier,
                settings.Supplier(combined.Supplier)?.Id,
                combined.PlanType,
                combined.TermMonths,
                uplift.RequestedUnitRate,
                uplift.RequestedStandingCharge,
                uplift.UnitRate,
                uplift.StandingCharge,
                uplift.IsCapped,
                combined.SitesCovered,
                combined.SitesTotal,
                combined.IsPartial,
                combined.AnyOutsidePricingWindow,
                combined.HasNoStandingCharge,
                IsGreen: null,   // until POWWR identifies green products (Q7)
                combined.TotalAnnualCostExVatCcl,
                combined.TotalCcl,
                combined.TotalVat,
                combined.TotalAnnualCostIncVatCcl,
                combined.TotalSavingExVatCcl,
                combined.TotalSavingIncVatCcl,
                combined.Commission.ContractTotal,
                combined.Commission.Upfront,
                combined.Commission.Monthly,
                combined.Sites.Select(s => details[s]).ToList());
        }).ToList();

        return new PricingResult(settings.CommissionBasis, microbusiness, siteResults, options, warnings);
    }

    // A plan the request builder didn't record: take the uplift POWWR reports, uncapped.
    private static EngineUplift FromReturnedRate(ReturnedRate rate)
    {
        var unit = BaseRates.Compute(rate.Uplift, null) ?? 0m;
        var standing = BaseRates.Compute(rate.StandingChargeUplift, null) ?? 0m;
        return new EngineUplift(rate.Supplier, rate.TermMonths, unit, standing, unit, standing);
    }

    // Research R9: RawCommission ÷ (RawAnnualPrice − RawBaseAnnualPrice) near 1 suggests a per-year
    // figure; near term ÷ 12 a whole-contract one. Logged to help answer Q3; no figure changes.
    private void LogCommissionRatio(ReturnedRate rate)
    {
        if (rate.RawCommission is not { } commission || rate.RawBaseAnnualPrice is not { } baseAnnual) return;
        var upliftValue = rate.RawAnnualPrice - baseAnnual;
        if (upliftValue == 0m) return;
        log.LogDebug("Commission basis check: {Supplier} {Term} months, RawCommission ÷ uplift value = {Ratio}",
            rate.Supplier, rate.TermMonths, commission / upliftValue);
    }
}
