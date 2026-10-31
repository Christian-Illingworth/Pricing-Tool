using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingTool.Data.Entities;
using PricingTool.Data.Seed;

namespace PricingTool.Data.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.Property(s => s.PricingName).HasMaxLength(100);
        builder.Property(s => s.DisplayName).HasMaxLength(100);
        builder.Property(s => s.UpfrontPercent).HasPrecision(9, 4);
        builder.Property(s => s.MaxUnitRateUpliftElectricity).HasPrecision(18, 6);
        builder.Property(s => s.MaxUnitRateUpliftGas).HasPrecision(18, 6);
        builder.Property(s => s.MaxStandingChargeUpliftElectricity).HasPrecision(18, 6);
        builder.Property(s => s.MaxStandingChargeUpliftGas).HasPrecision(18, 6);
        builder.Property(s => s.RowVersion).IsRowVersion();

        builder.HasIndex(s => s.PricingName).IsUnique();

        builder.ToTable(t => t.HasCheckConstraint("CK_Suppliers_UpfrontPercent", "[UpfrontPercent] >= 0 AND [UpfrontPercent] <= 100"));

        builder.HasData(SeedData.Suppliers);
    }
}
