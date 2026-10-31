namespace PricingTool.Services.Reporting;

// Where reporting records are exported for Power BI (FR-087). A local folder now; Azure Blob Storage
// later, as a new implementation and configuration only (constitution Principle VII).
public interface IReportingExportStore
{
    // Idempotent: writing the same path again overwrites it.
    Task WriteAsync(string path, ReadOnlyMemory<byte> json, CancellationToken ct);

    // For retention.
    Task DeleteAsync(string path, CancellationToken ct);
}
