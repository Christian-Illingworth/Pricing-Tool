using Microsoft.EntityFrameworkCore;
using PricingTool.Data;
using PricingTool.Data.Entities;

namespace PricingTool.Tests.Data;

public class AppendOnlyGuardTests
{
    private static readonly DateOnly Today = new(2026, 10, 31);

    private static bool Forbidden(object entity, EntityState state, params string[] modified) =>
        AppendOnlyGuard.IsForbidden(entity.GetType(), state, entity, modified, Today);

    public static TheoryData<object> AppendOnlyEntities() =>
    [
        new AuditEntry(),
        new SettingVersion(),
        new PricingRun(),
        new PricingRunSite(),
        new NoQuoteResult(),
        new PriceOptionSite(),
        new ReportingRecord(),
        new ReportingRecordSupplier(),
    ];

    [Theory]
    [MemberData(nameof(AppendOnlyEntities))]
    public void AppendOnlyRowsCannotBeChangedOrDeleted(object entity)
    {
        Assert.True(Forbidden(entity, EntityState.Modified, "Anything"));
        Assert.True(Forbidden(entity, EntityState.Deleted));
    }

    [Theory]
    [MemberData(nameof(AppendOnlyEntities))]
    public void AppendOnlyRowsCanBeAdded(object entity)
    {
        Assert.False(Forbidden(entity, EntityState.Added));
        Assert.False(Forbidden(entity, EntityState.Unchanged));
    }

    [Fact]
    public void LaterStoriesEntitiesAreAlreadyCovered()
    {
        Assert.Contains("CustomerPdf", AppendOnlyGuard.AppendOnlyEntities);
        Assert.Contains("OutlookDraft", AppendOnlyGuard.AppendOnlyEntities);
    }

    [Fact]
    public void PriceOptionSelectionCanChange()
    {
        Assert.False(Forbidden(new PriceOption(), EntityState.Modified, "IsSelected"));
        Assert.False(Forbidden(new PriceOption(), EntityState.Modified, "IsSelected", "IsRecommended"));
    }

    [Fact]
    public void PriceOptionFiguresCannotChange()
    {
        Assert.True(Forbidden(new PriceOption(), EntityState.Modified, "TotalVat"));
        Assert.True(Forbidden(new PriceOption(), EntityState.Modified, "IsSelected", "UnitRateUplift"));
        Assert.True(Forbidden(new PriceOption(), EntityState.Deleted));
    }

    [Fact]
    public void OnlyFutureEffectiveRatesCanBeDeleted()
    {
        Assert.False(Forbidden(new EffectiveRate { EffectiveFrom = Today.AddDays(1) }, EntityState.Deleted));
        Assert.True(Forbidden(new EffectiveRate { EffectiveFrom = Today }, EntityState.Deleted));
        Assert.True(Forbidden(new EffectiveRate { EffectiveFrom = Today.AddYears(-1) }, EntityState.Deleted));
    }

    [Fact]
    public void EffectiveRatesCannotBeEdited()
    {
        Assert.True(Forbidden(new EffectiveRate { EffectiveFrom = Today.AddDays(30) }, EntityState.Modified, "Value"));
    }

    [Fact]
    public void WorkingDataCanChange()
    {
        Assert.False(Forbidden(new Quote(), EntityState.Modified, "Status"));
        Assert.False(Forbidden(new Site(), EntityState.Deleted));
        Assert.False(Forbidden(new Supplier(), EntityState.Modified, "IsActive"));
        Assert.False(Forbidden(new StaffMember(), EntityState.Modified, "CustomerViewOn"));
        Assert.False(Forbidden(new ReportingOutboxEntry(), EntityState.Modified, "ExportedUtc"));
    }
}
