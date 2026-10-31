using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingTool.Data.Entities;

namespace PricingTool.Data.Configurations;

public class PriceOptionConfiguration : IEntityTypeConfiguration<PriceOption>
{
    public void Configure(EntityTypeBuilder<PriceOption> builder)
    {
        builder.Property(o => o.SupplierName).HasMaxLength(100);
        builder.Property(o => o.PlanType).HasMaxLength(50);
        builder.Property(o => o.RequestedUnitRateUplift).HasPrecision(18, 6);
        builder.Property(o => o.RequestedStandingChargeUplift).HasPrecision(18, 6);
        builder.Property(o => o.UnitRateUplift).HasPrecision(18, 6);
        builder.Property(o => o.StandingChargeUplift).HasPrecision(18, 6);

        builder.HasOne(o => o.PricingRun).WithMany(r => r.Options).HasForeignKey(o => o.PricingRunId).OnDelete(DeleteBehavior.Restrict);

        // PlanType is usually null (it's left out of the request until Q4), so the index must treat
        // nulls as equal: no filter, unlike EF's default for a nullable column.
        builder.HasIndex(o => new { o.PricingRunId, o.SupplierName, o.TermMonths, o.PlanType }).IsUnique().HasFilter(null);
    }
}
