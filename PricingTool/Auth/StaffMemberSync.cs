using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PricingTool.Data;
using PricingTool.Data.Entities;

namespace PricingTool.Auth;

// Creates or updates the signed-in person's StaffMember row on each sign-in (research R2). Roles are
// recorded for reports only; access always uses the live token.
public sealed class StaffMemberSync(PricingDbContext db, TimeProvider time)
{
    public async Task<StaffMember> SyncAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        var objectId = StaffClaims.ObjectId(user)
            ?? throw new InvalidOperationException("The sign-in has no object ID (oid) claim.");
        var now = time.GetUtcNow().UtcDateTime;

        var staff = await db.StaffMembers.SingleOrDefaultAsync(s => s.EntraObjectId == objectId, ct);
        if (staff is null)
        {
            staff = new StaffMember { EntraObjectId = objectId, FirstSeenUtc = now };
            db.StaffMembers.Add(staff);
        }

        staff.DisplayName = Fit(StaffClaims.DisplayName(user), 200) ?? staff.DisplayName;
        staff.Email = Fit(StaffClaims.EmailAddress(user), 320) ?? staff.Email;
        staff.LastRolesSeen = string.Join(",", StaffClaims.Roles(user));
        staff.LastSeenUtc = now;

        await db.SaveChangesAsync(ct);
        return staff;
    }

    private static string? Fit(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Length <= maxLength ? value : value[..maxLength];
}
