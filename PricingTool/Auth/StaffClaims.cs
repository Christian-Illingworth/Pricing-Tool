using System.Security.Claims;

namespace PricingTool.Auth;

// Reads who someone is from their sign-in claims. Entra tokens may arrive with short claim names
// ("oid") or the mapped long forms, so both are accepted. Roles always come from the live token,
// never the database (FR-094).
public static class StaffClaims
{
    public const string ObjectIdShort = "oid";
    public const string ObjectIdLong = "http://schemas.microsoft.com/identity/claims/objectidentifier";
    public const string NameClaim = "name";
    public const string PreferredUsernameClaim = "preferred_username";
    public const string EmailClaim = "email";
    public const string RolesClaim = "roles";
    public const string GroupsClaim = "groups";

    public static string? ObjectId(ClaimsPrincipal user) =>
        user.FindFirst(ObjectIdShort)?.Value ?? user.FindFirst(ObjectIdLong)?.Value;

    public static string? DisplayName(ClaimsPrincipal user) =>
        user.FindFirst(NameClaim)?.Value ?? user.FindFirst(ClaimTypes.Name)?.Value;

    public static string? EmailAddress(ClaimsPrincipal user) =>
        user.FindFirst(PreferredUsernameClaim)?.Value
        ?? user.FindFirst(EmailClaim)?.Value
        ?? user.FindFirst(ClaimTypes.Email)?.Value
        ?? user.FindFirst(ClaimTypes.Upn)?.Value;

    // The app roles this person holds, in RoleNames order.
    public static IReadOnlyList<string> Roles(ClaimsPrincipal user) =>
        RoleNames.All.Where(role =>
                user.IsInRole(role)
                || user.HasClaim(c => (c.Type == RolesClaim || c.Type == ClaimTypes.Role) && c.Value == role))
            .ToList();

    public static string RoleLabel(ClaimsPrincipal user) => Roles(user) switch
    {
        [] => "No role",
        [var one] => one,
        var many => string.Join(" and ", many),
    };

    public static string Initials(string? displayName)
    {
        var parts = (displayName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1].ToUpperInvariant(),
            _ => $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant(),
        };
    }
}
