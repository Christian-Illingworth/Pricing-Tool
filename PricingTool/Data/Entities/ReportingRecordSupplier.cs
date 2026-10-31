namespace PricingTool.Data.Entities;

// A supplier named in a reporting record, for the Reports page supplier filter. Append-only.
public class ReportingRecordSupplier
{
    public Guid ReportingRecordId { get; set; }
    public ReportingRecord ReportingRecord { get; set; } = null!;

    public string SupplierName { get; set; } = "";
}
