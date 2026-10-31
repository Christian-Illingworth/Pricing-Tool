namespace PricingTool.Data.Entities;

// The admin supplier list (FR-081).
public class Supplier
{
    public int Id { get; set; }

    // Exactly as POWWR's pricing API expects, for example "British Gas".
    public string PricingName { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public bool SuppliesElectricity { get; set; }
    public bool SuppliesGas { get; set; }

    // Inactive suppliers are left out of QuoteDefinitions.
    public bool IsActive { get; set; }

    // 0–100: the commission payment model (research R9). 0 is all monthly, 100 all upfront.
    public decimal UpfrontPercent { get; set; }

    // Hard limits per fuel (FR-059, FR-060): p/kWh and p/day. BLANK MEANS NO UPLIFT IS ALLOWED.
    public decimal? MaxUnitRateUpliftElectricity { get; set; }
    public decimal? MaxUnitRateUpliftGas { get; set; }
    public decimal? MaxStandingChargeUpliftElectricity { get; set; }
    public decimal? MaxStandingChargeUpliftGas { get; set; }

    // Set by SQL Server; null until the row is first saved.
    public byte[] RowVersion { get; set; } = null!;

    public List<SupplierAlias> Aliases { get; set; } = [];
}
