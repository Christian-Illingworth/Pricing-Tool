namespace PricingTool.Data.Entities;

// The per-site breakdown of one option. Append-only (AppendOnlyGuard). The rate strings are kept
// exactly as POWWR returned them and are shown that way (FR-035).
public class PriceOptionSite
{
    public int Id { get; set; }

    public int PriceOptionId { get; set; }
    public PriceOption PriceOption { get; set; } = null!;

    public int PricingRunSiteId { get; set; }
    public PricingRunSite PricingRunSite { get; set; } = null!;

    // Rate.Id.
    public int? PowwrRateId { get; set; }

    // ── As returned ──
    public string? StandingCharge { get; set; }
    public string? DayUnitrate { get; set; }
    public string? NightUnitrate { get; set; }
    public string? WendUnitrate { get; set; }
    public string? Stod1 { get; set; }
    public string? Stod2 { get; set; }
    public string? Stod3 { get; set; }
    public string? Stod4 { get; set; }
    public string? Stod5 { get; set; }
    public string? Stod6 { get; set; }
    public string? CapacityCharge { get; set; }
    public string? ExcessCapacityCharge { get; set; }
    public string? ReactiveRate { get; set; }
    public string? FixedFee { get; set; }
    public string? FixedFeeUplift { get; set; }
    public string? FixedFeeSCUplift { get; set; }
    public string? DirectDebitDiscount { get; set; }
    public string? ChequeSurcharge { get; set; }
    public string? TransportationCharge { get; set; }
    public string? Fits { get; set; }
    public string? Uplift { get; set; }
    public string? StandingChargeUplift { get; set; }
    public string? AnnualPrice { get; set; }
    public string? Commission { get; set; }
    public string? TariffCode { get; set; }
    public string? Period { get; set; }
    public string? ContractEndDate { get; set; }

    // Raw* values exactly as returned.
    public decimal? RawAnnualPrice { get; set; }
    public decimal? RawBaseAnnualPrice { get; set; }
    public decimal? RawAnnualPriceInclusive { get; set; }
    public decimal? RawCommission { get; set; }
    public decimal? FlatDirectDebitDiscount { get; set; }

    public bool OutsidePricingWindow { get; set; }

    // FR-041.
    public string? PricebookVersion { get; set; }

    public string? QuoteRateReference { get; set; }

    // Agent-only supplier notes (FR-033, FR-049). Never in the customer model.
    public string? ExtraInfo { get; set; }
    public string? FooterMessage { get; set; }

    // Agent-only: the returned rate minus the uplift (research R7).
    public decimal? BaseDayRate { get; set; }
    public decimal? BaseNightRate { get; set; }
    public decimal? BaseWendRate { get; set; }
    public decimal? BaseStandingCharge { get; set; }

    // = RawAnnualPrice (FR-034).
    public decimal AnnualCostExVatCcl { get; set; }

    // Research R8.
    public decimal Ccl { get; set; }
    public decimal Vat { get; set; }
    public decimal AnnualCostIncVatCcl { get; set; }

    // Research R11.
    public decimal? SavingExVatCcl { get; set; }
    public decimal? SavingIncVatCcl { get; set; }

    // Research R9.
    public decimal CommissionContractTotal { get; set; }
    public decimal CommissionUpfront { get; set; }
    public decimal CommissionMonthly { get; set; }

    // Research R7. Logged; never shown to customers.
    public decimal? CrossCheckAnnualCost { get; set; }
    public decimal? CrossCheckDifference { get; set; }
}
