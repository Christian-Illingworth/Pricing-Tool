using PricingTool.Domain;

namespace PricingTool.Services.Pricing;

public sealed record MicrobusinessResult(bool IsMicrobusiness, MicrobusinessSource Source, int CombinedAnnualKwh);

// FR-027, FR-028, research R10. A microbusiness by consumption (this quote's sites added up, at or below
// the fuel's threshold) or by declaration (both of the customer's answers yes). A flag from
// consumption can't be removed by the agent; the source says which applied.
public static class MicrobusinessTest
{
    public static MicrobusinessResult Evaluate(
        IEnumerable<int> siteAnnualKwh, decimal thresholdKwh, bool fewerThan10Employees, bool turnoverOrBalanceUnder2m)
    {
        var combined = siteAnnualKwh.Sum();
        var byConsumption = combined <= thresholdKwh;
        var byDeclaration = fewerThan10Employees && turnoverOrBalanceUnder2m;

        var source = (byConsumption, byDeclaration) switch
        {
            (true, true) => MicrobusinessSource.Both,
            (true, false) => MicrobusinessSource.Consumption,
            (false, true) => MicrobusinessSource.Declaration,
            _ => MicrobusinessSource.None,
        };
        return new MicrobusinessResult(byConsumption || byDeclaration, source, combined);
    }
}
