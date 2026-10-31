using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingTool.Data.Entities;

namespace PricingTool.Data.Configurations;

public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.Property(a => a.ActorName).HasMaxLength(200);
        builder.Property(a => a.Action).HasMaxLength(60);
        builder.Property(a => a.EntityType).HasMaxLength(60);
        builder.Property(a => a.EntityId).HasMaxLength(40);
        builder.Property(a => a.Detail).HasMaxLength(1000);

        builder.HasIndex(a => a.QuoteId);
    }
}
