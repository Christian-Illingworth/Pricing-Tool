using PricingTool.Domain;
using PricingTool.Services.Pricing;

namespace PricingTool.Tests.Pricing;

// FR-037, research R9.
public class CommissionCalculatorTests
{
    [Fact]
    public void PerYearBasisScalesByTheTerm()
    {
        var split = CommissionCalculator.Split(1_368.25m, CommissionBasis.PerYear, 24, upfrontPercent: 0m);
        Assert.Equal(2_736.50m, split.ContractTotal);
    }

    [Fact]
    public void WholeContractBasisIsTheFigureAsReturned()
    {
        var split = CommissionCalculator.Split(1_368.25m, CommissionBasis.WholeContract, 24, upfrontPercent: 0m);
        Assert.Equal(1_368.25m, split.ContractTotal);
    }

    [Fact]
    public void AllMonthly()
    {
        var split = CommissionCalculator.Split(1_368.25m, CommissionBasis.PerYear, 24, upfrontPercent: 0m);
        Assert.Equal(0m, split.Upfront);
        Assert.Equal(2_736.50m / 24m, split.Monthly);   // 114.0208333…, never rounded here
    }

    [Fact]
    public void AllUpfront()
    {
        var split = CommissionCalculator.Split(1_368.25m, CommissionBasis.PerYear, 24, upfrontPercent: 100m);
        Assert.Equal(2_736.50m, split.Upfront);
        Assert.Equal(0m, split.Monthly);
    }

    [Fact]
    public void PartUpfrontPartMonthly()
    {
        var split = CommissionCalculator.Split(1_000m, CommissionBasis.PerYear, 36, upfrontPercent: 40m);
        Assert.Equal(3_000m, split.ContractTotal);
        Assert.Equal(1_200m, split.Upfront);
        Assert.Equal(1_800m / 36m, split.Monthly);
    }

    [Fact]
    public void MultiSiteCommissionIsSummed()
    {
        var total = CommissionCalculator.Sum(
        [
            CommissionCalculator.Split(1_000m, CommissionBasis.PerYear, 12, 40m),
            CommissionCalculator.Split(500m, CommissionBasis.PerYear, 12, 40m),
        ]);
        Assert.Equal(1_500m, total.ContractTotal);
        Assert.Equal(600m, total.Upfront);
        Assert.Equal(900m / 12m, total.Monthly);
    }

    [Fact]
    public void NoTermIsAProgrammingError()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CommissionCalculator.Split(100m, CommissionBasis.PerYear, 0, 0m));
    }
}
