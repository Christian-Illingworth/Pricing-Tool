using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PricingTool.Data;

// Every DateTime in the database is UTC. SQL Server's datetime2 has no kind, so values are marked UTC
// on the way out, and anything that isn't already UTC is converted on the way in.
public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : DateTime.SpecifyKind(v, DateTimeKind.Utc),
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
