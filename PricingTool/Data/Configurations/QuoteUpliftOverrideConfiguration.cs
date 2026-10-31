using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingTool.Data.Entities;

namespace PricingTool.Data.Configurations;

public class QuoteUpliftOverrideConfiguration : IEntityTypeConfiguration<QuoteUpliftOverride>
{
    public void Configure(EntityTypeBuilder<QuoteUpliftOverride> builder)
    {
        builder.Property(o => o.UnitRateUplift).HasPrecision(18, 6);
        builder.Property(o => o.StandingChargeUplift).HasPrecision(18, 6);

        builder.HasOne(o => o.Quote).WithMany(q => q.UpliftOverrides).HasForeignKey(o => o.QuoteId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(o => o.Supplier).WithMany().HasForeignKey(o => o.SupplierId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => new { o.QuoteId, o.SupplierId, o.TermMonths }).IsUnique();
    }
}
