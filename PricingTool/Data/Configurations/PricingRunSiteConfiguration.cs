using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingTool.Data.Entities;

namespace PricingTool.Data.Configurations;

public class PricingRunSiteConfiguration : IEntityTypeConfiguration<PricingRunSite>
{
    public void Configure(EntityTypeBuilder<PricingRunSite> builder)
    {
        builder.Property(s => s.PowwrReference).HasMaxLength(64);
        builder.Property(s => s.ErrorMessage).HasMaxLength(1000);
        builder.Property(s => s.VatStandardRate).HasPrecision(18, 6);
        builder.Property(s => s.VatReducedRate).HasPrecision(18, 6);
        builder.Property(s => s.CclRatePencePerKwh).HasPrecision(18, 6);
        builder.Property(s => s.QualifyingFraction).HasPrecision(9, 6);

        builder.HasOne(s => s.PricingRun).WithMany(r => r.Sites).HasForeignKey(s => s.PricingRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Site).WithMany().HasForeignKey(s => s.SiteId).OnDelete(DeleteBehavior.Restrict);
    }
}
