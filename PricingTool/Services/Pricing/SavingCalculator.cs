using PricingTool.Domain;

namespace PricingTool.Services.Pricing;

// The agent's optional current rates: p/kWh per register and p/day (FR-023).
public sealed record CurrentRates(decimal? Day, decimal? Night, decimal? Wend, decimal? StandingCharge)
{
    public static readonly CurrentRates None = new(null, null, null, null);

    public decimal? For(Register register) => register switch
    {
        Register.Day => Day,
        Register.Night => Night,
        _ => Wend,
    };
}

public enum SavingDirection { Saves, CostsMore, Same }

// Research R11. A saving is only worked out when the current standing charge and a current rate for
// every register the site uses are known; otherwise the screen and PDF say no current prices were given.
public static class SavingCalculator
{
    // Σ(current rate × register kWh) / 100 + current standing charge × 365 / 100, in pounds.
    public static decimal? CurrentAnnualCost(IReadOnlyDictionary<Register, int> registerKwh, CurrentRates current)
    {
        if (current.StandingCharge is not { } standing || registerKwh.Count == 0) return null;

        var units = 0m;
        foreach (var (register, kwh) in registerKwh)
        {
            if (current.For(register) is not { } rate) return null;
            units += rate * kwh;
        }
        return units / 100m + standing * 365m / 100m;
    }

    // Positive saves money; negative costs more.
    public static decimal Saving(decimal currentCost, decimal newCost) => currentCost - newCost;

    public static SavingDirection DirectionOf(decimal saving) =>
        saving > 0m ? SavingDirection.Saves : saving < 0m ? SavingDirection.CostsMore : SavingDirection.Same;
}
