namespace PricingTool.Domain;

// Every enum is stored by EF as its name (nvarchar(32)), so reports and SQL queries stay readable
// (data-model.md "Storage conventions"). Renaming a member is a data migration, not a refactor.

// ── Quote and customer ──

public enum Fuel { Electricity, Gas }

public enum QuoteType { Renewal, NewSupplier, ChangeOfTenancy }

public enum BusinessType { SoleTrader, Partnership, LimitedCompany, Llp, Plc, Charity, PublicSector }

public enum PaymentMethod { DirectDebit, Bacs, CashCheque }

public enum PaymentFrequency { Monthly, Quarterly }

public enum PaymentAmount { Fixed, Variable }

public enum SupplierFilterMode { All, Include, Exclude }

public enum QuoteStatus { Draft, Priced, Quoted }

// ── Pricing ──

// The customer's declared treatment. Low use isn't here: the engine decides it per site (FR-030).
public enum VatCclTreatment { Standard, Qualifying }

// The treatment the engine applied to one site (research R8).
public enum SiteTreatment { Standard, LowUse, Qualifying }

public enum ConsumptionEntryMode { PerRegister, TotalWithSplit }

// For an MHHS-migrated meter with no SSC, chosen by the agent (FR-021, research R16).
public enum ForcedRateStructure { Standard, Economy7, EveningAndWeekend, EveningWeekendAndNight, OffPeak }

// Consumption registers, in display order. Wend is evening and weekend.
public enum Register { Day, Night, Wend }

public enum PricingRunStatus { Succeeded, Incomplete, Failed }

public enum PricingRunSiteStatus { Succeeded, Failed, NotPriced }

public enum ErrorCategory { Timeout, RateLimited, PowwrError, InvalidResponse, Network, CredentialsRejected }

public enum CommissionBasis { PerYear, WholeContract }

public enum MicrobusinessSource { Consumption, Declaration, Both, None }

// ── Outputs and settings ──

public enum DraftMethod { Graph, Eml }

public enum ReportingEventType { QuoteRequested, PricesReturned, Repriced, PdfGenerated, OpenedInOutlook, Failed }

public enum EffectiveRateKind
{
    VatStandard,
    VatReduced,
    CclElectricity,
    CclGas,
    LowUseElectricityKwhPerDay,
    LowUseGasKwhPerDay,
    QualifyingWholeSupplyPercent,
}

public enum SiteWarningCode
{
    Domestic,
    DeEnergised,
    GreenDealActive,
    SwitchInProgress,
    Igt,
    LargeGasSite,
    MhhsNoSsc,
    SupplierNotMapped,
}

// Where a site field's value came from (FR-011).
public enum FieldSource { Lookup, Agent, AgentChanged }
