namespace PricingTool.Data.Entities;

// Versioned scalar and wording settings (research R19). The current value is the highest Version for
// a Key. Each change inserts a new row; old rows are never changed. Append-only (AppendOnlyGuard).
public class SettingVersion
{
    public int Id { get; set; }

    // See SettingKeys.
    public string Key { get; set; } = "";

    // 1, 2, 3… per key.
    public int Version { get; set; }

    public string ValueJson { get; set; } = "";

    // Wording stays a placeholder until compliance approves it (FR-050). Outside Development, no PDF or
    // Outlook draft can use a placeholder.
    public bool IsPlaceholder { get; set; }

    // The approver's name, required when a wording version is saved as approved (FR-082).
    public string? ComplianceApprover { get; set; }

    // Null for seed data.
    public int? CreatedById { get; set; }

    public DateTime CreatedUtc { get; set; }
}
