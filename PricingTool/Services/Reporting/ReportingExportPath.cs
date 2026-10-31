using System.Globalization;
using PricingTool.Data.Entities;

namespace PricingTool.Services.Reporting;

// Where a record is exported: quotes/yyyy/MM/dd/{HHmmssfff}-{eventType}-{recordId}.json, from the
// event's UTC time (contracts/reporting-record.md). Unique and sortable, so Power BI can read a day at
// a time and a re-export overwrites the same file.
public static class ReportingExportPath
{
    public static string For(ReportingRecord record) => For(record.OccurredUtc, record.EventType.ToString(), record.Id);

    public static string For(DateTime occurredUtc, string eventType, Guid recordId)
    {
        var utc = occurredUtc.Kind == DateTimeKind.Local ? occurredUtc.ToUniversalTime() : DateTime.SpecifyKind(occurredUtc, DateTimeKind.Utc);
        return string.Create(CultureInfo.InvariantCulture,
            $"quotes/{utc:yyyy}/{utc:MM}/{utc:dd}/{utc:HHmmssfff}-{eventType}-{recordId:D}.json");
    }
}
