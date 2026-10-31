using PricingTool.Domain;

namespace PricingTool.Data.Entities;

// One request for prices at a point in time (FR-039, FR-041). Append-only (AppendOnlyGuard).
public class PricingRun
{
    public int Id { get; set; }

    public int QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;

    // 1, 2, 3… per quote.
    public int RunNumber { get; set; }

    public int RequestedById { get; set; }
    public StaffMember RequestedBy { get; set; } = null!;

    public DateTime RequestedUtc { get; set; }

    // The earliest successful site response; drives expiry (research R14).
    public DateTime? FetchedUtc { get; set; }

    // Incomplete means some sites failed technically (research R12).
    public PricingRunStatus Status { get; set; }

    // JSON: the quote and site inputs used, including uplifts. Never contains secrets.
    public string InputsSnapshot { get; set; } = "";

    // JSON: VAT and CCL rates per site start date, thresholds, payment models, supplier maximums, limits.
    public string SettingsSnapshot { get; set; } = "";

    // The basis POWWR's commission figure was read on (FR-037).
    public CommissionBasis CommissionBasis { get; set; }

    // The flag in force when priced (FR-028); the latest run's flag is the quote's flag.
    public bool IsMicrobusiness { get; set; }
    public MicrobusinessSource MicrobusinessSource { get; set; }
    public int CombinedAnnualKwh { get; set; }

    public List<PricingRunSite> Sites { get; set; } = [];

    public List<PriceOption> Options { get; set; } = [];
}
