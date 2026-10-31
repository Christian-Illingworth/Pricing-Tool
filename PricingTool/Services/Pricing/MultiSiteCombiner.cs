using System.Globalization;
using PricingTool.Powwr.Pricing;

namespace PricingTool.Services.Pricing;

// One site's figures for one returned rate, as the engine worked them out.
public sealed record SiteOptionFigures(
    int SiteKey,
    ReturnedRate Rate,
    decimal AnnualCostExVatCcl,
    decimal Ccl,
    decimal Vat,
    decimal AnnualCostIncVatCcl,
    decimal? SavingExVatCcl,
    decimal? SavingIncVatCcl,
    CommissionSplit Commission);

// One supplier product and term across the quote's sites, with totals at full precision.
public sealed record CombinedOption(
    string Supplier,
    string? PlanType,
    int TermMonths,
    IReadOnlyList<SiteOptionFigures> Sites,
    int SitesCovered,
    int SitesTotal,
    bool IsPartial,
    bool AnyOutsidePricingWindow,
    bool HasNoStandingCharge,
    decimal TotalAnnualCostExVatCcl,
    decimal TotalCcl,
    decimal TotalVat,
    decimal TotalAnnualCostIncVatCcl,
    decimal? TotalSavingExVatCcl,
    decimal? TotalSavingIncVatCcl,
    CommissionSplit Commission);

// Research R12: options are matched across sites by supplier pricing name, term and plan type as
// returned. An option missing any site is partial (FR-073). A total saving appears only when every
// covered site has one. Nothing is rounded.
public static class MultiSiteCombiner
{
    public static IReadOnlyList<CombinedOption> Combine(IReadOnlyList<SiteOptionFigures> figures, int sitesTotal) =>
        figures
            .GroupBy(f => (Supplier: f.Rate.Supplier.ToUpperInvariant(), f.Rate.TermMonths, PlanType: f.Rate.PlanType ?? ""))
            .Select(group =>
            {
                // One rate per site and key; a repeat for the same site isn't counted twice.
                var sites = group.GroupBy(f => f.SiteKey).Select(s => s.First()).OrderBy(s => s.SiteKey).ToList();
                var first = sites[0].Rate;
                var everySiteHasSaving = sites.All(s => s.SavingExVatCcl.HasValue);
                return new CombinedOption(
                    first.Supplier,
                    first.PlanType,
                    first.TermMonths,
                    sites,
                    sites.Count,
                    sitesTotal,
                    sites.Count < sitesTotal,
                    sites.Any(s => s.Rate.OutsidePricingWindow),
                    sites.All(s => IsZero(s.Rate.StandingCharge)),
                    sites.Sum(s => s.AnnualCostExVatCcl),
                    sites.Sum(s => s.Ccl),
                    sites.Sum(s => s.Vat),
                    sites.Sum(s => s.AnnualCostIncVatCcl),
                    everySiteHasSaving ? sites.Sum(s => s.SavingExVatCcl!.Value) : null,
                    everySiteHasSaving && sites.All(s => s.SavingIncVatCcl.HasValue) ? sites.Sum(s => s.SavingIncVatCcl!.Value) : null,
                    CommissionCalculator.Sum(sites.Select(s => s.Commission)));
            })
            .OrderBy(o => o.Supplier, StringComparer.OrdinalIgnoreCase)
            .ThenBy(o => o.TermMonths)
            .ThenBy(o => o.PlanType, StringComparer.OrdinalIgnoreCase)
            .ToList();

    // FR-026: "no standing charge" means the returned standing charge parses as zero.
    private static bool IsZero(string? standingCharge) =>
        decimal.TryParse(standingCharge?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value) && value == 0m;
}
