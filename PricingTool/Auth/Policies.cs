using Microsoft.AspNetCore.Authorization;

namespace PricingTool.Auth;

// Authorisation policies, enforced on the server for every page and handler (FR-093, research R2).
// Every policy, including the fallback, also requires GroupAndRoleRequirement, because a page with a
// named policy doesn't get the fallback.
public static class Policies
{
    // Create, price and change your own quotes. The Admin role alone can't quote (FR-002).
    public const string CanQuote = "CanQuote";

    // See every agent's quotes, read-only (FR-003).
    public const string CanViewAllQuotes = "CanViewAllQuotes";

    // Settings and reports.
    public const string CanAdmin = "CanAdmin";

    public static void Configure(AuthorizationOptions options)
    {
        var inGroupWithRole = new GroupAndRoleRequirement();

        // Every page without its own policy: signed in, in the group, holding any app role.
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(inGroupWithRole)
            .Build();

        options.AddPolicy(CanQuote, p => p.RequireAuthenticatedUser().AddRequirements(inGroupWithRole).RequireRole(RoleNames.Agent));
        options.AddPolicy(CanViewAllQuotes, p => p.RequireAuthenticatedUser().AddRequirements(inGroupWithRole).RequireRole(RoleNames.Admin));
        options.AddPolicy(CanAdmin, p => p.RequireAuthenticatedUser().AddRequirements(inGroupWithRole).RequireRole(RoleNames.Admin));
    }
}
