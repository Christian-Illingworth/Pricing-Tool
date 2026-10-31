using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PricingTool.Data.Entities;
using PricingTool.Services.Time;

namespace PricingTool.Data;

// Audit and history rows can be added but never changed or deleted through the app (constitution
// Principle V, research R19). The retention job deletes with ExecuteDeleteAsync, which bypasses the
// change tracker by design, and is the only deletion path.
public static class AppendOnlyGuard
{
    // By name, so entities that arrive with later stories (CustomerPdf, OutlookDraft) are covered too.
    public static readonly IReadOnlySet<string> AppendOnlyEntities = new HashSet<string>
    {
        nameof(AuditEntry),
        nameof(SettingVersion),
        nameof(EffectiveRate),
        nameof(PricingRun),
        nameof(PricingRunSite),
        nameof(NoQuoteResult),
        nameof(PriceOptionSite),
        "CustomerPdf",
        "OutlookDraft",
        nameof(ReportingRecord),
        nameof(ReportingRecordSupplier),
    };

    // The agent's selection is the only part of a PriceOption that can change (FR-040).
    public static readonly IReadOnlySet<string> PriceOptionSelectionFields = new HashSet<string>
    {
        nameof(PriceOption.IsSelected),
        nameof(PriceOption.IsRecommended),
    };

    public static bool IsForbidden(Type entityType, EntityState state, object entity, IReadOnlyCollection<string> modifiedProperties, DateOnly today)
    {
        if (state is not (EntityState.Modified or EntityState.Deleted)) return false;

        if (entityType == typeof(PriceOption))
            return state == EntityState.Deleted || modifiedProperties.Any(p => !PriceOptionSelectionFields.Contains(p));

        // A rate that hasn't taken effect yet can be withdrawn (the admin's delete is audited).
        if (entityType == typeof(EffectiveRate) && state == EntityState.Deleted && entity is EffectiveRate rate)
            return rate.EffectiveFrom <= today;

        return AppendOnlyEntities.Contains(entityType.Name);
    }
}

public sealed class AppendOnlyGuardInterceptor(TimeProvider time) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Check(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Check(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Check(DbContext? context)
    {
        if (context is null) return;
        var today = UkCalendar.Today(time);

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Modified or EntityState.Deleted)) continue;

            var modified = entry.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name).ToList();
            if (AppendOnlyGuard.IsForbidden(entry.Metadata.ClrType, entry.State, entry.Entity, modified, today))
            {
                var verb = entry.State == EntityState.Deleted ? "deleted" : "changed";
                throw new InvalidOperationException($"{entry.Metadata.ClrType.Name} is append-only and can't be {verb} through the app.");
            }
        }
    }
}
