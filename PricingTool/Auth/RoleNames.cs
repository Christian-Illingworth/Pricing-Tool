namespace PricingTool.Auth;

// The app roles defined in the Entra app registration (FR-002). There is no Manager role.
public static class RoleNames
{
    public const string Agent = "Agent";
    public const string Admin = "Admin";

    public static readonly IReadOnlyList<string> All = [Agent, Admin];
}
