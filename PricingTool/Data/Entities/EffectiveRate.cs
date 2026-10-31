using PricingTool.Domain;

namespace PricingTool.Data.Entities;

// An effective-dated rate or threshold (FR-031). A lookup takes the latest row with EffectiveFrom on or
// before the date. Rows are never edited; only a row whose date hasn't arrived can be deleted.
public class EffectiveRate
{
    public int Id { get; set; }

    public EffectiveRateKind Kind { get; set; }

    // Percent for VAT and the whole-supply rule, p/kWh for CCL, kWh a day for the low-use thresholds.
    public decimal Value { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public int? CreatedById { get; set; }

    public DateTime CreatedUtc { get; set; }
}
