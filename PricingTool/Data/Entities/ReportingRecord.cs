using PricingTool.Domain;

namespace PricingTool.Data.Entities;

// One quote event (FR-084). Payload is the full self-contained JSON document
// (contracts/reporting-record.md); the other columns are copies for the Reports page filters and
// totals. Append-only (AppendOnlyGuard).
public class ReportingRecord
{
    // Used in the export file name.
    public Guid Id { get; set; }

    public int SchemaVersion { get; set; }

    public ReportingEventType EventType { get; set; }

    public DateTime OccurredUtc { get; set; }

    public int QuoteId { get; set; }

    public string QuoteReference { get; set; } = "";

    public int AgentId { get; set; }

    public string AgentName { get; set; } = "";

    public string AgentEmail { get; set; } = "";

    public Fuel Fuel { get; set; }

    public QuoteType QuoteType { get; set; }

    // Distinct regions or LDZs, comma-separated; filtered with LIKE.
    public string Regions { get; set; } = "";

    // For the Reports page totals (research R21).
    public decimal? RecommendedCommissionTotal { get; set; }

    public bool PdfGenerated { get; set; }

    public bool OpenedInOutlook { get; set; }

    public string Payload { get; set; } = "";

    public List<ReportingRecordSupplier> Suppliers { get; set; } = [];

    public ReportingOutboxEntry? OutboxEntry { get; set; }
}
