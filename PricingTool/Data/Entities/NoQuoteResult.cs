namespace PricingTool.Data.Entities;

// A supplier that couldn't price a site, with POWWR's reason exactly as returned (FR-036).
// Append-only (AppendOnlyGuard).
public class NoQuoteResult
{
    public int Id { get; set; }

    public int PricingRunSiteId { get; set; }
    public PricingRunSite PricingRunSite { get; set; } = null!;

    // As returned.
    public string SupplierName { get; set; } = "";

    // When the rate gave a term.
    public int? TermMonths { get; set; }

    public string Reason { get; set; } = "";
}
