using PricingTool.Services.Quotes;

namespace PricingTool.Tests.Security;

public class QuoteAccessTests
{
    private const int Alex = 1, Sam = 2, Ada = 3, Bea = 4;

    private static readonly QuoteAccessUser AlexAgent = new(Alex, IsAgent: true, IsAdmin: false);
    private static readonly QuoteAccessUser SamAgent = new(Sam, IsAgent: true, IsAdmin: false);
    private static readonly QuoteAccessUser AdaAdmin = new(Ada, IsAgent: false, IsAdmin: true);
    private static readonly QuoteAccessUser BeaBoth = new(Bea, IsAgent: true, IsAdmin: true);

    [Fact]
    public void OwnerCanReadAndChange()
    {
        Assert.True(QuoteAccess.CanRead(AlexAgent, ownerId: Alex));
        Assert.True(QuoteAccess.CanChange(AlexAgent, ownerId: Alex));
        Assert.Equal(QuoteAccessResult.Allowed, QuoteAccess.ForChange(AlexAgent, ownerId: Alex));
    }

    [Fact]
    public void AnotherAgentGetsNotFoundNotForbidden()
    {
        // FR-003, FR-093, US7-8: the quote's existence isn't revealed.
        Assert.False(QuoteAccess.CanRead(SamAgent, ownerId: Alex));
        Assert.False(QuoteAccess.CanChange(SamAgent, ownerId: Alex));
        Assert.Equal(QuoteAccessResult.NotFound, QuoteAccess.ForRead(SamAgent, ownerId: Alex));
        Assert.Equal(QuoteAccessResult.NotFound, QuoteAccess.ForChange(SamAgent, ownerId: Alex));
    }

    [Fact]
    public void AdminCanReadAnyQuoteButNotChangeIt()
    {
        Assert.True(QuoteAccess.CanRead(AdaAdmin, ownerId: Alex));
        Assert.False(QuoteAccess.CanChange(AdaAdmin, ownerId: Alex));
        Assert.Equal(QuoteAccessResult.Allowed, QuoteAccess.ForRead(AdaAdmin, ownerId: Alex));
        Assert.Equal(QuoteAccessResult.Forbidden, QuoteAccess.ForChange(AdaAdmin, ownerId: Alex));
    }

    [Fact]
    public void PersonWithBothRolesChangesOnlyTheirOwn()
    {
        Assert.True(QuoteAccess.CanChange(BeaBoth, ownerId: Bea));
        Assert.True(QuoteAccess.CanRead(BeaBoth, ownerId: Alex));
        Assert.False(QuoteAccess.CanChange(BeaBoth, ownerId: Alex));
    }

    [Fact]
    public void OwnerWhoLostTheAgentRoleCanReadButNotChange()
    {
        var formerAgent = new QuoteAccessUser(Alex, IsAgent: false, IsAdmin: false);
        Assert.True(QuoteAccess.CanRead(formerAgent, ownerId: Alex));
        Assert.False(QuoteAccess.CanChange(formerAgent, ownerId: Alex));
        Assert.Equal(QuoteAccessResult.Forbidden, QuoteAccess.ForChange(formerAgent, ownerId: Alex));
    }
}
