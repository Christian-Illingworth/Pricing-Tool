namespace PricingTool.Services.Quotes;

// Who is asking: their StaffMember ID and the roles in their live token.
public readonly record struct QuoteAccessUser(int StaffMemberId, bool IsAgent, bool IsAdmin);

public enum QuoteAccessResult { Allowed, NotFound, Forbidden }

// The quote-level access rule (FR-003, FR-093), kept pure so it's tested without a request. An owner
// reads and, holding the Agent role, changes their quote. An Admin reads any quote but changes only
// their own. Anyone else gets "not found", so the app never reveals that someone else's quote exists.
public static class QuoteAccess
{
    public static bool CanRead(QuoteAccessUser user, int ownerId) =>
        user.StaffMemberId == ownerId || user.IsAdmin;

    public static bool CanChange(QuoteAccessUser user, int ownerId) =>
        user.StaffMemberId == ownerId && user.IsAgent;

    public static QuoteAccessResult ForRead(QuoteAccessUser user, int ownerId) =>
        CanRead(user, ownerId) ? QuoteAccessResult.Allowed : QuoteAccessResult.NotFound;

    public static QuoteAccessResult ForChange(QuoteAccessUser user, int ownerId) =>
        CanChange(user, ownerId) ? QuoteAccessResult.Allowed
        : CanRead(user, ownerId) ? QuoteAccessResult.Forbidden
        : QuoteAccessResult.NotFound;
}
