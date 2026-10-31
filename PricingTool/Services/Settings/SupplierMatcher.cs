using Microsoft.EntityFrameworkCore;
using PricingTool.Data;

namespace PricingTool.Services.Settings;

public enum SupplierMatchedOn { None, PowwrId, Mpid, ShortCode, Name }

// The admin supplier a meter lookup supplier maps to, or nulls when it isn't mapped (research R5).
public sealed record SupplierMatch(int? SupplierId, string? PricingName, SupplierMatchedOn MatchedOn)
{
    public static readonly SupplierMatch None = new(null, null, SupplierMatchedOn.None);
}

// One alias with its supplier's pricing name.
public sealed record SupplierAliasEntry(int SupplierId, string PricingName, string? PowwrId, string? Mpid, string? LookupName);

public interface ISupplierMatcher
{
    SupplierMatch Match(string? powwrId, string? mpid, string? shortCode, string? name);

    // Called when an admin changes a supplier or alias, so the next match reloads the list.
    void Invalidate();
}

// Matches the meter lookup's supplier against the admin aliases: POWWR ID first, then the MPID or
// short code, then the name. POWWR's own example shows why name alone is fragile: "British Gas
// Business" in the lookup is "British Gas" in pricing. The alias list is cached until invalidated.
public sealed class SupplierMatcher(IServiceScopeFactory scopes) : ISupplierMatcher
{
    private readonly Lock _gate = new();
    private IReadOnlyList<SupplierAliasEntry>? _aliases;

    public SupplierMatch Match(string? powwrId, string? mpid, string? shortCode, string? name) =>
        Match(Aliases(), powwrId, mpid, shortCode, name);

    public void Invalidate()
    {
        lock (_gate) _aliases = null;
    }

    public static SupplierMatch Match(IReadOnlyList<SupplierAliasEntry> aliases, string? powwrId, string? mpid, string? shortCode, string? name)
    {
        return Find(a => a.PowwrId, powwrId, SupplierMatchedOn.PowwrId)
            ?? Find(a => a.Mpid, mpid, SupplierMatchedOn.Mpid)
            ?? Find(a => a.Mpid, shortCode, SupplierMatchedOn.ShortCode)
            ?? Find(a => a.LookupName, name, SupplierMatchedOn.Name)
            ?? SupplierMatch.None;

        SupplierMatch? Find(Func<SupplierAliasEntry, string?> field, string? value, SupplierMatchedOn on)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var alias = aliases.FirstOrDefault(a => string.Equals(field(a), value.Trim(), StringComparison.OrdinalIgnoreCase));
            return alias is null ? null : new SupplierMatch(alias.SupplierId, alias.PricingName, on);
        }
    }

    private IReadOnlyList<SupplierAliasEntry> Aliases()
    {
        lock (_gate)
        {
            if (_aliases is not null) return _aliases;
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PricingDbContext>();
            _aliases = db.SupplierAliases.AsNoTracking()
                .Select(a => new SupplierAliasEntry(a.SupplierId, a.Supplier.PricingName, a.PowwrId, a.Mpid, a.LookupName))
                .ToList();
            return _aliases;
        }
    }
}
