using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using PricingTool.Options;
using AuthenticationOptions = PricingTool.Options.AuthenticationOptions;

namespace PricingTool.Auth;

// "In the Pricing Tool Users group and holding an app role" (FR-001, FR-002, research R2).
public sealed class GroupAndRoleRequirement : IAuthorizationRequirement;

// The app's own check of the group, on top of IT's "assignment required" in Entra, so access still
// holds if the Entra assignment is misconfigured. The Development sign-in has no groups claim, so
// there it needs only a role.
public sealed class GroupAndRoleHandler(IOptions<AuthenticationOptions> auth, IConfiguration config)
    : AuthorizationHandler<GroupAndRoleRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, GroupAndRoleRequirement requirement)
    {
        if (IsAllowed(context.User, auth.Value.Mode, config["AzureAd:AllowedGroupId"]))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }

    public static bool IsAllowed(ClaimsPrincipal user, AuthenticationMode mode, string? allowedGroupId)
    {
        if (user.Identity?.IsAuthenticated != true) return false;
        if (StaffClaims.Roles(user).Count == 0) return false;
        if (mode == AuthenticationMode.Development) return true;

        return !string.IsNullOrWhiteSpace(allowedGroupId)
            && user.FindAll(StaffClaims.GroupsClaim).Any(g => string.Equals(g.Value, allowedGroupId, StringComparison.OrdinalIgnoreCase));
    }
}
