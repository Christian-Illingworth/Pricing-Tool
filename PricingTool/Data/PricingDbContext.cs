using Microsoft.EntityFrameworkCore;
using PricingTool.Data.Entities;

namespace PricingTool.Data;

// The app's database: SQL Server Express locally, Azure SQL later, with the same migrations for both
// (data-model.md). Each entity's mapping is in Data/Configurations.
public class PricingDbContext(DbContextOptions<PricingDbContext> options) : DbContext(options)
{
    public const string QuoteNumberSequence = "QuoteNumbers";

    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<QuoteUpliftOverride> QuoteUpliftOverrides => Set<QuoteUpliftOverride>();
    public DbSet<PricingRun> PricingRuns => Set<PricingRun>();
    public DbSet<PricingRunSite> PricingRunSites => Set<PricingRunSite>();
    public DbSet<NoQuoteResult> NoQuoteResults => Set<NoQuoteResult>();
    public DbSet<PriceOption> PriceOptions => Set<PriceOption>();
    public DbSet<PriceOptionSite> PriceOptionSites => Set<PriceOptionSite>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierAlias> SupplierAliases => Set<SupplierAlias>();
    public DbSet<SettingVersion> SettingVersions => Set<SettingVersion>();
    public DbSet<EffectiveRate> EffectiveRates => Set<EffectiveRate>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<ReportingRecord> ReportingRecords => Set<ReportingRecord>();
    public DbSet<ReportingRecordSupplier> ReportingRecordSuppliers => Set<ReportingRecordSupplier>();
    public DbSet<ReportingOutboxEntry> ReportingOutboxEntries => Set<ReportingOutboxEntry>();

    // data-model.md "Storage conventions": money and engine figures at full precision, enums by name,
    // instants in UTC. Thresholds (18,6) and percentages (9,4) are set per property.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(28, 10);
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>().HaveColumnType("datetime2(3)");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Gives CUQ-yy-nnnnnn references. Global and never reset, so references stay unique (R28).
        modelBuilder.HasSequence<int>(QuoteNumberSequence).StartsAt(1).IncrementsBy(1);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PricingDbContext).Assembly);
    }
}
