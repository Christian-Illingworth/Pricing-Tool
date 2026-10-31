using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingTool.Data.Entities;
using PricingTool.Domain;

namespace PricingTool.Data.Configurations;

public class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> builder)
    {
        builder.Property(s => s.AddressLine1).HasMaxLength(200);
        builder.Property(s => s.AddressLine2).HasMaxLength(200);
        builder.Property(s => s.AddressLine3).HasMaxLength(200);
        builder.Property(s => s.AddressLine4).HasMaxLength(200);
        builder.Property(s => s.Town).HasMaxLength(100);
        builder.Property(s => s.Postcode).HasMaxLength(8);
        builder.Property(s => s.LookupMeterAddressId).HasMaxLength(100);
        builder.Property(s => s.CurrentSupplierText).HasMaxLength(200);

        builder.Property(s => s.CurrentDayRate).HasPrecision(18, 6);
        builder.Property(s => s.CurrentNightRate).HasPrecision(18, 6);
        builder.Property(s => s.CurrentWendRate).HasPrecision(18, 6);
        builder.Property(s => s.CurrentStandingCharge).HasPrecision(18, 6);
        builder.Property(s => s.SplitDayPct).HasPrecision(9, 4);
        builder.Property(s => s.SplitNightPct).HasPrecision(9, 4);
        builder.Property(s => s.SplitWendPct).HasPrecision(9, 4);

        builder.Property(s => s.Warnings)
            .HasConversion(JsonColumn.Converter<List<SiteWarning>>(), JsonColumn.Comparer<List<SiteWarning>>())
            .HasColumnType("nvarchar(max)");
        builder.Property(s => s.FieldSources)
            .HasConversion(JsonColumn.Converter<Dictionary<string, FieldSource>>(), JsonColumn.Comparer<Dictionary<string, FieldSource>>())
            .HasColumnType("nvarchar(max)");

        builder.HasOne(s => s.Quote).WithMany(q => q.Sites).HasForeignKey(s => s.QuoteId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.CurrentSupplier).WithMany().HasForeignKey(s => s.CurrentSupplierId).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(s => s.Meter, meter =>
        {
            meter.Property(m => m.MpanTopLine).HasMaxLength(9);
            meter.Property(m => m.MpanCore).HasMaxLength(13);
            meter.Property(m => m.DistributorId).HasMaxLength(2);
            meter.Property(m => m.RegionName).HasMaxLength(60);
            meter.Property(m => m.GspId).HasMaxLength(4);
            meter.Property(m => m.LineLossFactorClass).HasMaxLength(3);
            meter.Property(m => m.StandardSettlementConfig).HasMaxLength(4);
            meter.Property(m => m.DuosTariffId).HasMaxLength(3);
            meter.Property(m => m.MeasurementClass).HasMaxLength(1);
            meter.Property(m => m.MeterType).HasMaxLength(20);
            meter.Property(m => m.SmetsVersion).HasMaxLength(10);
            meter.Property(m => m.RateRegisters).HasMaxLength(40);
            meter.Property(m => m.Mprn).HasMaxLength(10);
            meter.Property(m => m.Ldz).HasMaxLength(4);
            meter.Property(m => m.ExitZone).HasMaxLength(4);
            meter.Property(m => m.AmrIndicator).HasMaxLength(2);
            meter.HasIndex(m => m.MpanCore);
            meter.HasIndex(m => m.Mprn);
        });
        builder.Navigation(s => s.Meter).IsRequired();

        builder.HasIndex(s => s.Postcode);
    }
}
