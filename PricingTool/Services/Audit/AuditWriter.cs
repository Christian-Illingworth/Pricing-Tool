using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using PricingTool.Auth;
using PricingTool.Data;
using PricingTool.Data.Entities;

namespace PricingTool.Services.Audit;

// Adds an AuditEntry to the CURRENT DbContext, so it's saved in the same SaveChanges as the action it
// records: if the audit row can't be saved, the action fails too (constitution Principle V, R19).
// Nothing is saved here.
public sealed class AuditWriter(PricingDbContext db, CurrentStaff staff, TimeProvider time)
{
    public async Task WriteAsync(
        string action,
        string entityType,
        string entityId,
        int? quoteId = null,
        object? oldValue = null,
        object? newValue = null,
        string? detail = null,
        CancellationToken ct = default)
    {
        // A background job has no signed-in person, and is recorded as "System".
        StaffMember? actor = staff.IsSignedIn ? await staff.GetAsync(ct) : null;

        db.AuditEntries.Add(new AuditEntry
        {
            OccurredUtc = time.GetUtcNow().UtcDateTime,
            ActorId = actor?.Id,
            ActorName = Fit(actor?.DisplayName, 200) ?? "System",
            Action = action,
            EntityType = entityType,
            EntityId = Fit(entityId, 40) ?? "",
            QuoteId = quoteId,
            OldValueJson = AuditJson.Serialize(oldValue),
            NewValueJson = AuditJson.Serialize(newValue),
            Detail = Fit(detail, 1000),
        });
    }

    private static string? Fit(string? value, int maxLength) =>
        value is null ? null : value.Length <= maxLength ? value : value[..maxLength];
}

// JSON for audit values. Secrets aren't settings and shouldn't ever be passed in, but as a second line
// of defence any property whose name looks like a secret is left out (FR-096).
public static class AuditJson
{
    private static readonly string[] SecretWords = ["secret", "licencecode", "licensecode", "password", "token"];

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { LeaveOutSecrets } },
    };

    public static string? Serialize(object? value) => value is null ? null : JsonSerializer.Serialize(value, Options);

    private static void LeaveOutSecrets(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object) return;
        for (var i = typeInfo.Properties.Count - 1; i >= 0; i--)
        {
            var name = typeInfo.Properties[i].Name.ToLowerInvariant();
            if (SecretWords.Any(name.Contains)) typeInfo.Properties.RemoveAt(i);
        }
    }
}
