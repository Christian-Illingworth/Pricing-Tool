using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingTool.Data.Entities;

namespace PricingTool.Data.Configurations;

public class PricingRunConfiguration : IEntityTypeConfiguration<PricingRun>
{
    public void Configure(EntityTypeBuilder<PricingRun> builder)
    {
        builder.HasOne(r => r.Quote).WithMany(q => q.PricingRuns).HasForeignKey(r => r.QuoteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.RequestedBy).WithMany().HasForeignKey(r => r.RequestedById).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.QuoteId, r.RunNumber }).IsUnique();
    }
}
