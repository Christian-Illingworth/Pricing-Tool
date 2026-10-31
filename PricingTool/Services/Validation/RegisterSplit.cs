using PricingTool.Domain;

namespace PricingTool.Services.Validation;

// Consumption entered as a total plus a percentage split (FR-022, research R15).
public static class RegisterSplit
{
    public static bool TotalsOneHundred(IReadOnlyCollection<decimal> percentages) =>
        percentages.Count > 0 && percentages.Sum() == 100m;

    // Splits a whole-kWh total across the registers by the largest-remainder method, so the parts are
    // whole kWh and always add up to the total. Ties go to the earlier register (Day, Night, Wend).
    public static IReadOnlyDictionary<Register, int> Split(int totalKwh, IReadOnlyDictionary<Register, decimal> percentages)
    {
        if (!TotalsOneHundred(percentages.Values.ToList()))
            throw new ArgumentException("The percentages must add up to exactly 100.", nameof(percentages));

        var shares = percentages
            .OrderBy(p => p.Key)
            .Select(p =>
            {
                var exact = totalKwh * p.Value / 100m;
                var whole = (int)decimal.Floor(exact);
                return (Register: p.Key, Whole: whole, Remainder: exact - whole);
            })
            .ToList();

        var spare = totalKwh - shares.Sum(s => s.Whole);
        var result = shares.ToDictionary(s => s.Register, s => s.Whole);
        foreach (var share in shares.OrderByDescending(s => s.Remainder).ThenBy(s => s.Register).Take(spare))
            result[share.Register]++;
        return result;
    }
}
