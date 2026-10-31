using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingTool.Data.Entities;
using PricingTool.Data.Seed;

namespace PricingTool.Data.Configurations;

public class SupplierAliasConfiguration : IEntityTypeConfiguration<SupplierAlias>
{
    public void Configure(EntityTypeBuilder<SupplierAlias> builder)
    {
        builder.Property(a => a.PowwrId).HasMaxLength(20);
        builder.Property(a => a.Mpid).HasMaxLength(10);
        builder.Property(a => a.LookupName).HasMaxLength(200);

        builder.HasOne(a => a.Supplier).WithMany(s => s.Aliases).HasForeignKey(a => a.SupplierId).OnDelete(DeleteBehavior.Cascade);

        // Unique where present: EF filters nullable unique indexes to non-null values.
        builder.HasIndex(a => a.PowwrId).IsUnique();
        builder.HasIndex(a => a.Mpid).IsUnique();
        builder.HasIndex(a => a.LookupName).IsUnique();

        builder.ToTable(t => t.HasCheckConstraint("CK_SupplierAliases_AtLeastOne",
            "[PowwrId] IS NOT NULL OR [Mpid] IS NOT NULL OR [LookupName] IS NOT NULL"));

        builder.HasData(SeedData.SupplierAliases);
    }
}
