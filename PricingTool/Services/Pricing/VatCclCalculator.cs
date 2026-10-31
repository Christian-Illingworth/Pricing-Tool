using PricingTool.Domain;

namespace PricingTool.Services.Pricing;

// CCL and VAT on one site's annual cost, all in pounds at full precision.
public sealed record VatCclResult(decimal Ccl, decimal Vat, decimal Total);

// FR-029 to FR-031, research R8. Each site uses the rates in force on its own start date. CCL rates are
// pence per kWh; VAT rates are percentages. VAT is charged on CCL, as HMRC requires.
public static class VatCclCalculator
{
    // Low use first (decided by consumption, FR-030), then the customer's declared treatment. For
    // qualifying use, a declared percentage at or above the whole-supply setting counts as 100%.
    public static (SiteTreatment Treatment, decimal QualifyingFraction) DecideTreatment(
        VatCclTreatment declared, decimal? qualifyingPercent, int annualKwh, SiteRates rates)
    {
        if (LowUseTest.IsLowUse(annualKwh, rates.LowUseKwhPerDay)) return (SiteTreatment.LowUse, 0m);
        if (declared != VatCclTreatment.Qualifying) return (SiteTreatment.Standard, 0m);

        var percent = qualifyingPercent ?? 0m;
        if (rates.QualifyingWholeSupplyPercent is { } whole && percent >= whole) return (SiteTreatment.Qualifying, 1m);
        return (SiteTreatment.Qualifying, percent / 100m);
    }

    public static VatCclResult Apply(decimal costExVatCcl, int annualKwh, SiteTreatment treatment, decimal qualifyingFraction, SiteRates rates)
    {
        var standard = rates.VatStandardPercent / 100m;
        var reduced = rates.VatReducedPercent / 100m;

        var (ccl, vat) = treatment switch
        {
            SiteTreatment.LowUse => (0m, costExVatCcl * reduced),
            SiteTreatment.Qualifying => QualifyingUse(costExVatCcl, annualKwh, qualifyingFraction, rates.CclPencePerKwh, standard, reduced),
            _ => StandardUse(costExVatCcl, annualKwh, rates.CclPencePerKwh, standard),
        };
        return new VatCclResult(ccl, vat, costExVatCcl + ccl + vat);
    }

    private static (decimal Ccl, decimal Vat) StandardUse(decimal cost, int kwh, decimal cclPence, decimal standard)
    {
        var ccl = kwh * cclPence / 100m;
        return (ccl, (cost + ccl) * standard);
    }

    // CCL relief and reduced VAT on the qualifying share q; standard treatment on the rest.
    private static (decimal Ccl, decimal Vat) QualifyingUse(decimal cost, int kwh, decimal q, decimal cclPence, decimal standard, decimal reduced)
    {
        var ccl = kwh * (1m - q) * cclPence / 100m;
        return (ccl, cost * q * reduced + (cost * (1m - q) + ccl) * standard);
    }
}
