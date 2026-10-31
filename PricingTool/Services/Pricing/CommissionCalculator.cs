using PricingTool.Domain;

namespace PricingTool.Services.Pricing;

// Commission for the whole contract, split into the upfront and monthly amounts (pounds, full precision).
public sealed record CommissionSplit(decimal ContractTotal, decimal Upfront, decimal Monthly)
{
    public static readonly CommissionSplit Zero = new(0m, 0m, 0m);
}

// FR-037, research R9. POWWR's RawCommission is read on the admin's basis (per year to start with,
// Q3), then split by the supplier's payment model: UpfrontPercent of it upfront, the rest monthly.
public static class CommissionCalculator
{
    public static CommissionSplit Split(decimal rawCommission, CommissionBasis basis, int termMonths, decimal upfrontPercent)
    {
        if (termMonths <= 0) throw new ArgumentOutOfRangeException(nameof(termMonths), termMonths, "A term must be at least one month.");

        var total = basis == CommissionBasis.PerYear ? rawCommission * termMonths / 12m : rawCommission;
        var upfront = total * upfrontPercent / 100m;
        return new CommissionSplit(total, upfront, (total - upfront) / termMonths);
    }

    // A multi-site option's commission is the sum over the sites it prices.
    public static CommissionSplit Sum(IEnumerable<CommissionSplit> splits) =>
        splits.Aggregate(CommissionSplit.Zero, (sum, s) => new CommissionSplit(sum.ContractTotal + s.ContractTotal, sum.Upfront + s.Upfront, sum.Monthly + s.Monthly));
}
