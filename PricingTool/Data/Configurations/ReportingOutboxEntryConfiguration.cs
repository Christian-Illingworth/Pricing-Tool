using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingTool.Data.Entities;

namespace PricingTool.Data.Configurations;

public class ReportingOutboxEntryConfiguration : IEntityTypeConfiguration<ReportingOutboxEntry>
{
    public void Configure(EntityTypeBuilder<ReportingOutboxEntry> builder)
    {
        builder.HasKey(e => e.ReportingRecordId);
        builder.Property(e => e.ExportPath).HasMaxLength(300);
        builder.Property(e => e.LastError).HasMaxLength(1000);

        builder.HasOne(e => e.ReportingRecord).WithOne(r => r.OutboxEntry)
            .HasForeignKey<ReportingOutboxEntry>(e => e.ReportingRecordId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.NextAttemptUtc, e.ExportedUtc });
    }
}
