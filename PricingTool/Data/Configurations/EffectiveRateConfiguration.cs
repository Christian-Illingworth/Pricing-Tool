using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingTool.Data.Entities;
using PricingTool.Data.Seed;

namespace PricingTool.Data.Configurations;

public class EffectiveRateConfiguration : IEntityTypeConfiguration<EffectiveRate>
{
    public void Configure(EntityTypeBuilder<EffectiveRate> builder)
    {
        builder.Property(r => r.Value).HasPrecision(18, 6);

        builder.HasIndex(r => new { r.Kind, r.EffectiveFrom }).IsUnique();

        builder.HasData(SeedData.EffectiveRates);
    }
}
