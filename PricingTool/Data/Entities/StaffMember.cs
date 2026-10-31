namespace PricingTool.Data.Entities;

// A signed-in person, created or updated on each sign-in (research R2). Roles are NOT stored for
// authorisation: they always come from the live token. LastRolesSeen is informational only.
public class StaffMember
{
    public int Id { get; set; }

    // The `oid` claim; for the Development sign-in, a fixed made-up GUID.
    public string EntraObjectId { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string Email { get; set; } = "";

    // From Graph /me, cached for 24 hours (research, open items).
    public string? Phone { get; set; }

    public DateTime? PhoneCheckedUtc { get; set; }

    // The "Customer view" switch (FR-099). Stays on until the person turns it off.
    public bool CustomerViewOn { get; set; }

    // For example "Agent,Admin".
    public string LastRolesSeen { get; set; } = "";

    public DateTime FirstSeenUtc { get; set; }

    public DateTime LastSeenUtc { get; set; }
}
