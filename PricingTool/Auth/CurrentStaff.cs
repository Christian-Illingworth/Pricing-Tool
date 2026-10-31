using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PricingTool.Data;
using PricingTool.Data.Entities;
using PricingTool.Services.Quotes;

namespace PricingTool.Auth;

// The signed-in person for this request. Name and roles come straight from the claims; the StaffMember
// row is loaded only when something needs it (an audit entry, a quote's owner).
public sealed class CurrentStaff(IHttpContextAccessor http, PricingDbContext db, StaffMemberSync sync)
{
    private StaffMember? _staff;

    public ClaimsPrincipal User => http.HttpContext?.User ?? new ClaimsPrincipal();

    public bool IsSignedIn => User.Identity?.IsAuthenticated == true;

    public string? ObjectId => StaffClaims.ObjectId(User);

    public string DisplayName => StaffClaims.DisplayName(User) ?? "";

    public IReadOnlyList<string> Roles => StaffClaims.Roles(User);

    public bool IsAgent => Roles.Contains(RoleNames.Agent);

    public bool IsAdmin => Roles.Contains(RoleNames.Admin);

    // The row is normally created at sign-in; it's created here too if the database was reset since.
    public async Task<StaffMember> GetAsync(CancellationToken ct)
    {
        if (_staff is not null) return _staff;
        var objectId = ObjectId ?? throw new InvalidOperationException("Nobody is signed in.");
        _staff = await db.StaffMembers.SingleOrDefaultAsync(s => s.EntraObjectId == objectId, ct)
            ?? await sync.SyncAsync(User, ct);
        return _staff;
    }

    public async Task<QuoteAccessUser> AccessUserAsync(CancellationToken ct) =>
        new((await GetAsync(ct)).Id, IsAgent, IsAdmin);
}
