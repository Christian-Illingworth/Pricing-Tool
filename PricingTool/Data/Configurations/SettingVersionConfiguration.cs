using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingTool.Data.Entities;
using PricingTool.Data.Seed;

namespace PricingTool.Data.Configurations;

public class SettingVersionConfiguration : IEntityTypeConfiguration<SettingVersion>
{
    public void Configure(EntityTypeBuilder<SettingVersion> builder)
    {
        builder.Property(s => s.Key).HasMaxLength(80);
        builder.Property(s => s.ComplianceApprover).HasMaxLength(200);

        builder.HasIndex(s => new { s.Key, s.Version }).IsUnique();

        builder.HasData(SeedData.SettingVersions);
    }
}
