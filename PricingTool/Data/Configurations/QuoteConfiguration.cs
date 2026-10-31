using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingTool.Data.Entities;

namespace PricingTool.Data.Configurations;

public class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> builder)
    {
        builder.Property(q => q.Reference).HasMaxLength(16);
        builder.Property(q => q.QualifyingPercent).HasPrecision(9, 4);
        builder.Property(q => q.RowVersion).IsRowVersion();

        builder.HasOne(q => q.Owner).WithMany().HasForeignKey(q => q.OwnerId).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(q => q.Customer, customer =>
        {
            customer.Property(c => c.BusinessName).HasMaxLength(200);
            customer.Property(c => c.CompanyNumber).HasMaxLength(8);
            customer.Property(c => c.ContactName).HasMaxLength(200);
            customer.Property(c => c.ContactEmail).HasMaxLength(320);
            customer.Property(c => c.ContactPhone).HasMaxLength(32);
            customer.HasIndex(c => c.BusinessName);
        });
        builder.Navigation(q => q.Customer).IsRequired();

        builder.OwnsOne(q => q.Contract, contract =>
        {
            contract.Property(c => c.TermsMonths).HasMaxLength(20);
            contract.Property(c => c.SupplierFilterIds).HasMaxLength(400);
            contract.Property(c => c.DefaultUnitRateUplift).HasPrecision(18, 6);
            contract.Property(c => c.DefaultStandingChargeUplift).HasPrecision(18, 6);
        });
        builder.Navigation(q => q.Contract).IsRequired();

        builder.HasIndex(q => q.Reference).IsUnique();
        builder.HasIndex(q => new { q.OwnerId, q.CreatedUtc });
        builder.HasIndex(q => q.LastActivityUtc);

        builder.ToTable(t => t.HasCheckConstraint("CK_Quotes_QualifyingPercent", "[QualifyingPercent] IS NULL OR ([QualifyingPercent] >= 0 AND [QualifyingPercent] <= 100)"));
    }
}
