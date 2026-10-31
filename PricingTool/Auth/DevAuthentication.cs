using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using PricingTool.Options;
using AuthenticationOptions = PricingTool.Options.AuthenticationOptions;

namespace PricingTool.Auth;

// The test sign-in with made-up staff, until IT provides the app registration (FR-095, research R2).
// Its cookie scheme is registered only in the Development environment with Authentication:Mode set
// to Development; anywhere else startup refuses that mode and /Dev/SignIn returns 404.
public static class DevAuthentication
{
    public const string Scheme = "DevCookie";

    public sealed record DevPerson(string Key, string DisplayName, string Email, string Phone, string ObjectId, IReadOnlyList<string> Roles);

    // Made-up people with fixed object IDs, so their StaffMember rows and quotes survive restarts.
    // The phone numbers are from Ofcom's range for drama.
    public static readonly IReadOnlyList<DevPerson> People =
    [
        new("alex", "Alex Agent", "alex.agent@example.com", "01632 960001", "0d1a0000-0000-4000-8000-000000000001", [RoleNames.Agent]),
        new("sam", "Sam Agent", "sam.agent@example.com", "01632 960002", "0d1a0000-0000-4000-8000-000000000002", [RoleNames.Agent]),
        new("ada", "Ada Admin", "ada.admin@example.com", "01632 960003", "0d1a0000-0000-4000-8000-000000000003", [RoleNames.Admin]),
        new("bea", "Bea Both", "bea.both@example.com", "01632 960004", "0d1a0000-0000-4000-8000-000000000004", [RoleNames.Agent, RoleNames.Admin]),
        new("pat", "Pat Nobody", "pat.nobody@example.com", "01632 960005", "0d1a0000-0000-4000-8000-000000000005", []),
    ];

    public static bool IsEnabled(IHostEnvironment env, AuthenticationOptions options) =>
        env.IsDevelopment() && options.Mode == AuthenticationMode.Development;

    public static void Register(IServiceCollection services)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = Scheme;
                options.DefaultChallengeScheme = Scheme;
                options.DefaultForbidScheme = Scheme;
            })
            .AddCookie(Scheme);
    }

    // The same claim names Entra uses, so the rest of the app can't tell the difference.
    public static ClaimsPrincipal PrincipalFor(DevPerson person)
    {
        var claims = new List<Claim>
        {
            new(StaffClaims.ObjectIdShort, person.ObjectId),
            new(StaffClaims.NameClaim, person.DisplayName),
            new(StaffClaims.PreferredUsernameClaim, person.Email),
        };
        claims.AddRange(person.Roles.Select(role => new Claim(StaffClaims.RolesClaim, role)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme, StaffClaims.NameClaim, StaffClaims.RolesClaim));
    }

    public static DevPerson? Find(string? objectId) => People.FirstOrDefault(p => p.ObjectId == objectId);
}
