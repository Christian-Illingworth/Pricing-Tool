using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingTool.Data.Entities;

namespace PricingTool.Data.Configurations;

public class ReportingRecordConfiguration : IEntityTypeConfiguration<ReportingRecord>
{
    public void Configure(EntityTypeBuilder<ReportingRecord> builder)
    {
        // The id is chosen by the app, because it's also the export file name.
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.QuoteReference).HasMaxLength(16);
        builder.Property(r => r.AgentName).HasMaxLength(200);
        builder.Property(r => r.AgentEmail).HasMaxLength(320);
        builder.Property(r => r.Regions).HasMaxLength(200);

        builder.HasIndex(r => r.OccurredUtc);
        builder.HasIndex(r => r.QuoteId);
        builder.HasIndex(r => r.AgentId);
        builder.HasIndex(r => r.Fuel);
        builder.HasIndex(r => r.QuoteType);
    }
}
