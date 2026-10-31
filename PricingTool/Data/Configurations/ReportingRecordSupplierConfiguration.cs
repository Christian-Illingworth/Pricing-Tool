using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingTool.Data.Entities;

namespace PricingTool.Data.Configurations;

public class ReportingRecordSupplierConfiguration : IEntityTypeConfiguration<ReportingRecordSupplier>
{
    public void Configure(EntityTypeBuilder<ReportingRecordSupplier> builder)
    {
        builder.HasKey(s => new { s.ReportingRecordId, s.SupplierName });
        builder.Property(s => s.SupplierName).HasMaxLength(100);

        builder.HasOne(s => s.ReportingRecord).WithMany(r => r.Suppliers).HasForeignKey(s => s.ReportingRecordId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.SupplierName);
    }
}
