using PricingTool.Domain;

namespace PricingTool.Services.Pricing;

// The settings in force when a quote is priced (research R7). The pricing engine reads only this, and
// it's stored on PricingRun.SettingsSnapshot, so a later settings change never alters a past quote
// (FR-083).
public sealed record PricingSettingsSnapshot(
    Fuel Fuel,
    CommissionBasis CommissionBasis,
    IReadOnlyList<SiteRates> RatesByStartDate,
    decimal MicrobusinessThresholdKwh,
    decimal? FuelMaxUnitRateUplift,
    decimal? FuelMaxStandingChargeUplift,
    IReadOnlyList<SupplierSettings> Suppliers)
{
    public SiteRates RatesFor(DateOnly startDate) =>
        RatesByStartDate.FirstOrDefault(r => r.StartDate == startDate)
        ?? throw new InvalidOperationException($"The settings snapshot has no rates for a start date of {startDate:yyyy-MM-dd}.");

    public SupplierSettings? Supplier(string pricingName) =>
        Suppliers.FirstOrDefault(s => string.Equals(s.PricingName, pricingName, StringComparison.OrdinalIgnoreCase));
}

// VAT and CCL rates and the low-use threshold in effect on one site start date (FR-031). VAT rates are
// percentages; CCL is pence per kWh; the threshold is kWh a day.
public sealed record SiteRates(
    DateOnly StartDate,
    decimal VatStandardPercent,
    decimal VatReducedPercent,
    decimal CclPencePerKwh,
    decimal LowUseKwhPerDay,
    decimal? QualifyingWholeSupplyPercent);

// A supplier's payment model and its maximum uplifts for this quote's fuel. Null maximum = no uplift.
public sealed record SupplierSettings(
    int Id,
    string PricingName,
    decimal UpfrontPercent,
    decimal? MaxUnitRateUplift,
    decimal? MaxStandingChargeUplift);
