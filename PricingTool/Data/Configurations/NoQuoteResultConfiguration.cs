using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingTool.Data.Entities;

namespace PricingTool.Data.Configurations;

public class NoQuoteResultConfiguration : IEntityTypeConfiguration<NoQuoteResult>
{
    public void Configure(EntityTypeBuilder<NoQuoteResult> builder)
    {
        builder.Property(n => n.SupplierName).HasMaxLength(100);
        builder.Property(n => n.Reason).HasMaxLength(1000);

        builder.HasOne(n => n.PricingRunSite).WithMany(s => s.NoQuotes).HasForeignKey(n => n.PricingRunSiteId).OnDelete(DeleteBehavior.Restrict);
    }
}
