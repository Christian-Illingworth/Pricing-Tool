namespace PricingTool.Data.Entities;

// One supplier product and term from a run, with totals across the run's sites (FR-033, FR-073).
// Everything except IsSelected and IsRecommended is append-only (AppendOnlyGuard). Figures are the
// pricing engine's full-precision output (research R7); nothing is rounded until it's displayed.
public class PriceOption
{
    public int Id { get; set; }

    public int PricingRunId { get; set; }
    public PricingRun PricingRun { get; set; } = null!;

    // The pricing name as returned.
    public string SupplierName { get; set; } = "";

    // The matched admin supplier.
    public int? SupplierId { get; set; }

    // As returned.
    public string? PlanType { get; set; }

    public int TermMonths { get; set; }

    // What the agent asked for, for this supplier and term.
    public decimal RequestedUnitRateUplift { get; set; }
    public decimal RequestedStandingChargeUplift { get; set; }

    // What was actually sent to POWWR, after any supplier cap. Re-checked before each PDF and Outlook
    // draft (FR-063).
    public decimal UnitRateUplift { get; set; }
    public decimal StandingChargeUplift { get; set; }

    // True when either applied uplift is lower than requested (FR-062). Agent-only.
    public bool IsCappedAtSupplierMax { get; set; }

    public int SitesCovered { get; set; }
    public int SitesTotal { get; set; }
    public bool IsPartial { get; set; }
    public bool AnyOutsidePricingWindow { get; set; }

    // Every covered site's standing charge parses as 0 (FR-026).
    public bool HasNoStandingCharge { get; set; }

    // Null until POWWR identifies green products (Q7); the filter is hidden while null.
    public bool? IsGreen { get; set; }

    public decimal TotalAnnualCostExVatCcl { get; set; }
    public decimal TotalCcl { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalAnnualCostIncVatCcl { get; set; }

    // Only when every covered site has a saving.
    public decimal? TotalSavingExVatCcl { get; set; }
    public decimal? TotalSavingIncVatCcl { get; set; }

    public decimal CommissionContractTotal { get; set; }
    public decimal CommissionUpfront { get; set; }
    public decimal CommissionMonthly { get; set; }

    // Changed by the agent and audited (FR-040).
    public bool IsSelected { get; set; }

    // At most one per run, and only among selected options.
    public bool IsRecommended { get; set; }

    public List<PriceOptionSite> Sites { get; set; } = [];
}
