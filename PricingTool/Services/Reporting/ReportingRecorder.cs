using PricingTool.Data;
using PricingTool.Data.Entities;

namespace PricingTool.Services.Reporting;

// Records a quote event (FR-084, research R21): adds the ReportingRecord, its supplier rows and an
// outbox entry to the CURRENT DbContext, so they're saved in the same transaction as the action. One
// insert is the only reporting cost on the agent's request; the outbox worker exports it later, so
// reporting never slows down or blocks an agent (FR-089). Nothing is saved here.
public sealed class ReportingRecorder(PricingDbContext db, TimeProvider time)
{
    public ReportingRecord Record(ReportingEvent reportingEvent)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var record = ReportingRecordBuilder.Build(reportingEvent, Guid.NewGuid(), now);
        record.OutboxEntry = new ReportingOutboxEntry
        {
            ReportingRecordId = record.Id,
            Attempts = 0,
            NextAttemptUtc = now,
        };
        db.ReportingRecords.Add(record);
        return record;
    }
}
