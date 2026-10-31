using PricingTool.Services.Audit;

namespace PricingTool.Tests.Security;

public class AuditJsonTests
{
    [Fact]
    public void SecretLookingPropertiesAreLeftOut()
    {
        var json = AuditJson.Serialize(new
        {
            Supplier = "EDF",
            UnitRateUplift = 1.5m,
            LicenceCode = "abc123",
            ClientSecret = "shh",
            AccessToken = "eyJ...",
        })!;

        Assert.Contains("\"supplier\":\"EDF\"", json);
        Assert.Contains("\"unitRateUplift\":1.5", json);
        Assert.DoesNotContain("abc123", json);
        Assert.DoesNotContain("shh", json);
        Assert.DoesNotContain("eyJ", json);
    }

    [Fact]
    public void NullStaysNull() => Assert.Null(AuditJson.Serialize(null));
}
