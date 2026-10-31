namespace PricingTool.Data.Entities;

// The agent's uplift for one supplier and term (FR-025, FR-039). Pairs without an override use the
// quote's default uplifts. Each change writes an AuditEntry with the old and new values.
public class QuoteUpliftOverride
{
    public int Id { get; set; }

    public int QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    // One of the quote's terms.
    public int TermMonths { get; set; }

    // p/kWh and p/day, ≥ 0 and ≤ the per-fuel maximum (FR-061).
    public decimal UnitRateUplift { get; set; }
    public decimal StandingChargeUplift { get; set; }
}
