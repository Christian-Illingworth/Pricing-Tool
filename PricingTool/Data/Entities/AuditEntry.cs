namespace PricingTool.Data.Entities;

// The append-only history of every action (Principle V). Written in the same SaveChanges as the action,
// so if it can't be saved the action fails too. Never holds secrets.
public class AuditEntry
{
    public long Id { get; set; }

    public DateTime OccurredUtc { get; set; }

    // Null for system jobs.
    public int? ActorId { get; set; }

    // A snapshot of the name; "System" for jobs.
    public string ActorName { get; set; } = "";

    // See AuditActions.
    public string Action { get; set; } = "";

    public string EntityType { get; set; } = "";

    public string EntityId { get; set; } = "";

    // Drives the quote history timeline (US7-6).
    public int? QuoteId { get; set; }

    public string? OldValueJson { get; set; }

    public string? NewValueJson { get; set; }

    public string? Detail { get; set; }
}
