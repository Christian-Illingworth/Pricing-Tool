namespace PricingTool.Data.Entities;

// Tracks the export of one reporting record (research R21). The one reporting table the outbox worker
// updates, so it isn't append-only.
public class ReportingOutboxEntry
{
    public Guid ReportingRecordId { get; set; }
    public ReportingRecord ReportingRecord { get; set; } = null!;

    public int Attempts { get; set; }

    public DateTime NextAttemptUtc { get; set; }

    // Null while pending.
    public DateTime? ExportedUtc { get; set; }

    // quotes/yyyy/MM/dd/…json
    public string? ExportPath { get; set; }

    // Never contains secrets.
    public string? LastError { get; set; }
}
