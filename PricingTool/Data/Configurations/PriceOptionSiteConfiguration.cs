using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingTool.Data.Entities;

namespace PricingTool.Data.Configurations;

public class PriceOptionSiteConfiguration : IEntityTypeConfiguration<PriceOptionSite>
{
    public void Configure(EntityTypeBuilder<PriceOptionSite> builder)
    {
        // POWWR's rate strings, stored exactly as returned (nvarchar(32)).
        foreach (var rate in new[]
                 {
                     nameof(PriceOptionSite.StandingCharge), nameof(PriceOptionSite.DayUnitrate), nameof(PriceOptionSite.NightUnitrate),
                     nameof(PriceOptionSite.WendUnitrate), nameof(PriceOptionSite.Stod1), nameof(PriceOptionSite.Stod2),
                     nameof(PriceOptionSite.Stod3), nameof(PriceOptionSite.Stod4), nameof(PriceOptionSite.Stod5),
                     nameof(PriceOptionSite.Stod6), nameof(PriceOptionSite.CapacityCharge), nameof(PriceOptionSite.ExcessCapacityCharge),
                     nameof(PriceOptionSite.ReactiveRate), nameof(PriceOptionSite.FixedFee), nameof(PriceOptionSite.FixedFeeUplift),
                     nameof(PriceOptionSite.FixedFeeSCUplift), nameof(PriceOptionSite.DirectDebitDiscount), nameof(PriceOptionSite.ChequeSurcharge),
                     nameof(PriceOptionSite.TransportationCharge), nameof(PriceOptionSite.Fits), nameof(PriceOptionSite.Uplift),
                     nameof(PriceOptionSite.StandingChargeUplift), nameof(PriceOptionSite.AnnualPrice), nameof(PriceOptionSite.Commission),
                     nameof(PriceOptionSite.ContractEndDate),
                 })
        {
            builder.Property<string?>(rate).HasMaxLength(32);
        }
        builder.Property(s => s.TariffCode).HasMaxLength(100);
        builder.Property(s => s.Period).HasMaxLength(100);
        builder.Property(s => s.PricebookVersion).HasMaxLength(50);
        builder.Property(s => s.QuoteRateReference).HasMaxLength(100);

        builder.Property(s => s.BaseDayRate).HasPrecision(18, 6);
        builder.Property(s => s.BaseNightRate).HasPrecision(18, 6);
        builder.Property(s => s.BaseWendRate).HasPrecision(18, 6);
        builder.Property(s => s.BaseStandingCharge).HasPrecision(18, 6);

        builder.HasOne(s => s.PriceOption).WithMany(o => o.Sites).HasForeignKey(s => s.PriceOptionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.PricingRunSite).WithMany().HasForeignKey(s => s.PricingRunSiteId).OnDelete(DeleteBehavior.Restrict);
    }
}
