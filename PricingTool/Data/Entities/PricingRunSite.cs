using PricingTool.Domain;

namespace PricingTool.Data.Entities;

// One POWWR request and response for one site in a run, plus the engine's site-level figures.
// Append-only (AppendOnlyGuard).
public class PricingRunSite
{
    public int Id { get; set; }

    public int PricingRunId { get; set; }
    public PricingRun PricingRun { get; set; } = null!;

    public int SiteId { get; set; }
    public Site Site { get; set; } = null!;

    // Starts at 1; a retry of failed sites adds attempt 2 and so on.
    public int Attempt { get; set; }

    // NotPriced is a large gas site (FR-045).
    public PricingRunSiteStatus Status { get; set; }

    // The request as sent, with LicenceCode masked as *** (research R4).
    public string RequestJson { get; set; } = "";

    // The raw response, kept for audit (Principle V).
    public string? ResponseJson { get; set; }

    // QuoteResult.Reference.
    public string? PowwrReference { get; set; }

    // Agent-only.
    public string? FooterNotes { get; set; }

    public ErrorCategory? ErrorCategory { get; set; }

    // Safe for display; never contains secrets.
    public string? ErrorMessage { get; set; }

    public DateTime SentUtc { get; set; }
    public DateTime ReceivedUtc { get; set; }

    public DateOnly StartDateUsed { get; set; }

    // Decided by the engine for this site (research R8).
    public SiteTreatment Treatment { get; set; }

    public int AnnualKwh { get; set; }

    // The rates used for this site, from its start date (FR-031).
    public decimal VatStandardRate { get; set; }
    public decimal VatReducedRate { get; set; }
    public decimal CclRatePencePerKwh { get; set; }

    // After the whole-supply rule.
    public decimal QualifyingFraction { get; set; }

    // Only when the current rates are complete (research R11).
    public decimal? CurrentAnnualCostExVatCcl { get; set; }
    public decimal? CurrentAnnualCostIncVatCcl { get; set; }

    public List<NoQuoteResult> NoQuotes { get; set; } = [];
}
