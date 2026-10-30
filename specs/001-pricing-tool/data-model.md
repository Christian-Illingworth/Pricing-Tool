# Data model: CU Pricing Tool

**Feature**: `001-pricing-tool` | **Date**: 2026-10-30 | **Plan**: [plan.md](plan.md) | **Research**: [research.md](research.md)

This is the logical model for EF Core 10 with SQL Server: SQL Server Express locally, Azure SQL later,
with the same migrations for both. `PricingDbContext` lives in `PricingTool/Data/`.

## Storage conventions

| Kind of value | How it's stored |
|---|---|
| Keys | `int` identity, except `ReportingRecord.Id`, which is a `Guid` used in export file names |
| Instants | `DateTime` in UTC, `datetime2(3)`, named `…Utc` |
| Calendar dates | `DateOnly`, `date` |
| Money and engine figures | `decimal(28,10)`, full precision (R7) |
| Thresholds and limits | `decimal(18,6)` |
| Percentages | `decimal(9,4)` |
| POWWR rate strings | `nvarchar(32)`, stored exactly as returned |
| Consumption | `int`, whole kWh (POWWR's `Consumption.Amount` is an integer) |
| JSON snapshots | `nvarchar(max)`, written with `System.Text.Json` |
| Enums | Stored as strings (`nvarchar(32)`), so reports and SQL queries stay readable |
| Concurrency | `rowversion` on `Quote`, `ApprovalRequest` and `Supplier` |
| Append-only data | Never updated or deleted through the app. `AppendOnlyGuard` (R19) enforces this; only the retention job deletes. Marked 🔒 below. |

## Entity map

```text
StaffMember 1─* Quote (owner) 1─* Site (each with one Meter, owned)
                 │
                 ├─* QuoteUpliftOverride
                 ├─* PricingRun 🔒 1─* PricingRunSite 🔒 1─* NoQuoteResult 🔒
                 │        │
                 │        └─* PriceOption 1─* PriceOptionSite 🔒 (*─1 PricingRunSite)
                 ├─* ApprovalRequest 1─* ApprovalItem
                 ├─* CustomerPdf 🔒 1─* OutlookDraft 🔒
                 ├─* PendingUpload 1─* PendingUploadRow
                 ├─* AuditEntry 🔒
                 └─* ReportingRecord 🔒 1─* ReportingRecordSupplier 🔒
                                        1─1 ReportingOutboxEntry

Supplier 1─* SupplierAlias          UpliftLimit (*─0..1 StaffMember)
SettingVersion 🔒                   EffectiveRate 🔒
```

`PriceOption` is append-only except for its selection fields (`IsSelected`, `IsRecommended`), which
the agent changes, and each change is audited.

---

## StaffMember

A signed-in person. Created or updated on each sign-in (R2).

| Field | Type | Rules |
|---|---|---|
| Id | int | PK |
| EntraObjectId | string(64) | Unique. The `oid` claim; for the dev sign-in, a fixed made-up GUID |
| DisplayName | string(200) | From the `name` claim |
| Email | string(320) | From `preferred_username` or `email` |
| Phone | string(32)? | From Graph `/me`, cached (see research, Open items) |
| PhoneCheckedUtc | DateTime? | When Phone was last read |
| LastRolesSeen | string(64) | For example `Agent,Manager`. Informational, for reports; access always uses the live token |
| FirstSeenUtc, LastSeenUtc | DateTime | |

Roles are **not** stored for authorisation. They always come from the token.

## Quote

One customer, one fuel, one or more sites.

| Field | Type | Rules |
|---|---|---|
| Id | int | PK |
| Reference | string(16) | Unique, `CUQ-yy-nnnnnn`, from the sequence (R28, FR-078) |
| OwnerId | int | FK StaffMember. The only person who can change the quote |
| Fuel | enum `Electricity`/`Gas` | Fixed once created |
| QuoteType | enum `Renewal`/`NewSupplier`/`ChangeOfTenancy` | FR-013 |
| CotDate | DateOnly? | Required when ChangeOfTenancy |
| OutOfContract | bool | FR-014 |
| **Customer** (owned) | | FR-005 to FR-007 |
| ‣ BusinessName | string(200) | Required |
| ‣ BusinessType | enum `SoleTrader`/`Partnership`/`LimitedCompany`/`Llp`/`Plc`/`Charity`/`PublicSector` | Required |
| ‣ CompanyNumber | string(8)? | Only for LimitedCompany, Llp and Plc; 8 digits, or 2 letters + 6 digits |
| ‣ ContactName | string(200) | Required |
| ‣ ContactEmail | string(320) | Required, valid email |
| ‣ ContactPhone | string(32) | Required, valid UK number, stored normalised |
| ‣ DeclaredFewerThan10Employees | bool | Microbusiness declaration (FR-027) |
| ‣ DeclaredTurnoverOrBalanceUnder2m | bool | Microbusiness declaration |
| **Contract** (owned) | | FR-024 to FR-026 |
| ‣ StartDate | DateOnly | Not in the past (UK date) when pricing |
| ‣ TermsMonths | string(20) | A set from {12, 24, 36, 48, 60}, stored as `12,24`; at least one |
| ‣ PaymentMethod | enum `DirectDebit`/`Bacs`/`CashCheque` | Default from the admin setting (monthly Direct Debit) |
| ‣ PaymentFrequency | enum `Monthly`/`Quarterly`? | |
| ‣ PaymentAmount | enum `Fixed`/`Variable`? | |
| ‣ SupplierFilterMode | enum `All`/`Include`/`Exclude` | |
| ‣ SupplierFilterIds | string(400) | Comma-separated Supplier IDs |
| ‣ NoStandingChargeOnly | bool | FR-026 |
| ‣ DefaultUnitRateUplift | decimal(18,6) | p/kWh, ≥ 0; prefilled from the fuel default (FR-025) |
| ‣ DefaultStandingChargeUplift | decimal(18,6) | p/day, ≥ 0 |
| VatCclTreatment | enum `Standard`/`Qualifying` | Low use is decided per site by the engine (FR-030) |
| QualifyingPercent | decimal(9,4)? | 0–100; required when Qualifying |
| IncludeVatCcl | bool | The display switch, saved and used on the PDF (FR-034) |
| Status | enum | See the state diagram below |
| CreatedUtc, LastActivityUtc | DateTime | LastActivityUtc is updated by every action and drives retention (FR-080) |
| RowVersion | rowversion | |

**Indexes**: Reference (unique); (OwnerId, CreatedUtc); Customer.BusinessName; LastActivityUtc.

**Microbusiness**: the flag isn't stored on the quote. It is stored on each `PricingRun` (FR-028), and
the latest run's flag is the quote's flag.

### Quote status

```text
Draft ──price──▶ Priced ──generate PDF──▶ Quoted
  ▲                │  ▲                     │
  └──edit inputs───┘  └─────re-price────────┘
```

- **Draft**: saved but never successfully priced, or inputs changed since the last pricing.
- **Priced**: the latest run has results.
- **Quoted**: at least one PDF has been generated from the latest run.

Re-pricing never deletes earlier runs or PDFs (FR-039, FR-051). Approval state isn't a quote status: it
is worked out for each option (R13).

## Site

An address with exactly one meter (FR-071).

| Field | Type | Rules |
|---|---|---|
| Id | int | PK |
| QuoteId | int | FK Quote, cascade |
| Ordinal | int | Order shown, starting at 1 |
| AddressLine1–4 | string(200)? | At least line 1 required |
| Town | string(100)? | |
| Postcode | string(8) | Validated and normalised with a space (FR-008) |
| StartDate | DateOnly? | Optional per-site override of the contract start date (FR-071) |
| LookupMeterAddressId | string(100)? | When picked from the lookup |
| CurrentSupplierId | int? | FK Supplier, when known |
| CurrentSupplierText | string(200)? | As returned or typed, when it isn't mapped |
| CurrentDayRate, CurrentNightRate, CurrentWendRate | decimal(18,6)? | p/kWh, optional (FR-023) |
| CurrentStandingCharge | decimal(18,6)? | p/day, optional |
| CurrentContractEndDate | DateOnly? | Optional |
| ConsumptionDayKwh, ConsumptionNightKwh, ConsumptionWendKwh | int? | Registers as the rate structure needs them; > 0 |
| ConsumptionEntryMode | enum `PerRegister`/`TotalWithSplit` | |
| ConsumptionTotalKwh | int? | Used with TotalWithSplit, split by largest remainder (R15) |
| SplitDayPct, SplitNightPct, SplitWendPct | decimal(9,4)? | Must add up to exactly 100 |
| GasAnnualQuantityKwh | int? | Gas AQ, > 0 |
| **Meter** (owned, see below) | | |
| Warnings | JSON | List of `{ code, message, confirmedByAgent }`. Codes: `Domestic`, `DeEnergised`, `GreenDealActive`, `SwitchInProgress`, `Igt`, `LargeGasSite`, `MhhsNoSsc`, `SupplierNotMapped` (FR-012, FR-045, FR-046) |
| FieldSources | JSON | Map of field → `Lookup`, `Agent` or `AgentChanged` (FR-011) |
| LookupSnapshot | JSON? | The mapped lookup response, without secrets, for audit |

**Indexes**: Postcode; Meter.MpanCore; Meter.Mprn.

**Validation**: a meter number can appear only once per quote (FR-070). All of a quote's sites must
match the quote's fuel.

### Meter (owned by Site, same table)

| Field | Fuel | Type | Rules |
|---|---|---|---|
| MpanTopLine | Electricity | string(9)? | 8 digits (legacy) or 9 characters (MHHS) (FR-015) |
| MpanCore | Electricity | string(13) | 13 digits with a valid check digit (FR-017) |
| DistributorId | Electricity | string(2) | First two digits of the core; region worked out from it (R15) |
| RegionName | Electricity | string(60) | Display copy of the region |
| GspId | Electricity | string(4)? | From the lookup |
| ProfileClass | Electricity | int? | 0–8 |
| MeterTimeswitchClass | Electricity | int? | 3 digits |
| LineLossFactorClass | Electricity | string(3)? | |
| StandardSettlementConfig | Electricity | string(4)? | |
| DuosTariffId | Electricity | string(3)? | Taken from a 9-character top line; otherwise optional (FR-016) |
| MhhsMigrated | Electricity | bool? | True when the top line has 9 characters; otherwise optional |
| MeasurementClass | Electricity | string(1)? | |
| MeterType | Both | string(20)? | Shown as returned |
| SmetsVersion | Electricity | string(10)? | |
| SmartMeter | Both | bool | FR-019 and FR-043 |
| IsHalfHourly | Electricity | bool | Worked out: profile class 00, or a half-hourly measurement class |
| KvaCapacity | Electricity | int? | Required when half-hourly (FR-018) |
| KvarhConsumption | Electricity | int? | Optional when half-hourly |
| RateRegisters | Electricity | string(40) | For example `Day,Night`, from `/prompts` or the forced structure (FR-020) |
| ForcedRateStructure | Electricity | enum? | `Standard`/`Economy7`/`EveningAndWeekend`/`EveningWeekendAndNight`/`OffPeak`, only for an MHHS meter with no SSC (FR-021) |
| Mprn | Gas | string(10)? | 6–10 digits (FR-043) |
| Ldz | Gas | string(4)? | From the lookup, shown only; blank when entered by hand |
| ExitZone | Gas | string(4)? | |
| IsIgt | Gas | bool | FR-046 |
| MeterCapacity | Gas | int? | Checked against the large-site limit |
| DailyMeteredQuantity | Gas | int? | Checked against the large-site limit |
| AmrIndicator | Gas | string(2)? | Suggests the smart meter flag |
| IsLargeGasSite | Gas | bool | Worked out from the three optional limits (FR-045); such a site is not priced |

## QuoteUpliftOverride

The agent's uplift for one supplier and term (FR-025, FR-039). Supplier and term pairs without an
override use the quote's default uplifts.

| Field | Type | Rules |
|---|---|---|
| Id | int | PK |
| QuoteId | int | FK, cascade |
| SupplierId | int | FK Supplier |
| TermMonths | int | One of the quote's terms |
| UnitRateUplift | decimal(18,6) | ≥ 0 |
| StandingChargeUplift | decimal(18,6) | ≥ 0 |

Unique (QuoteId, SupplierId, TermMonths). Each change writes an AuditEntry with the old and new values.

## PricingRun 🔒

One request for prices at a point in time (FR-039, FR-041).

| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| QuoteId | int | FK |
| RunNumber | int | 1, 2, 3… per quote |
| RequestedById | int | FK StaffMember |
| RequestedUtc | DateTime | |
| FetchedUtc | DateTime? | Earliest successful site response; drives expiry (R14) |
| Status | enum `Succeeded`/`Incomplete`/`Failed` | Incomplete means some sites failed technically (R12) |
| InputsSnapshot | JSON | Quote and site inputs used, including uplifts. Never contains secrets |
| SettingsSnapshot | JSON | VAT and CCL rates per site start date, thresholds, supplier payment models, supplier maximums, limits used |
| CommissionBasis | enum `PerYear`/`WholeContract` | FR-037 |
| IsMicrobusiness | bool | FR-028 |
| MicrobusinessSource | enum `Consumption`/`Declaration`/`Both`/`None` | |
| CombinedAnnualKwh | int | Used for the microbusiness test |

Unique (QuoteId, RunNumber).

## PricingRunSite 🔒

One POWWR request and response for one site in a run, plus the engine's site-level figures.

| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| PricingRunId | int | FK |
| SiteId | int | FK |
| Attempt | int | Starts at 1; a retry of failed sites adds attempt 2 and so on (spec edge case) |
| Status | enum `Succeeded`/`Failed`/`NotPriced` | NotPriced is used for a large gas site (FR-045) |
| RequestJson | nvarchar(max) | The request as sent, with `LicenceCode` masked (R4) |
| ResponseJson | nvarchar(max)? | Raw response, kept for audit (Principle V) |
| PowwrReference | string(64)? | `QuoteResult.Reference` |
| FooterNotes | nvarchar(max)? | Agent-only |
| ErrorCategory | enum? | `Timeout`, `RateLimited`, `PowwrError`, `InvalidResponse`, `Network` |
| ErrorMessage | string(1000)? | Safe for display; never contains secrets |
| SentUtc, ReceivedUtc | DateTime | |
| StartDateUsed | DateOnly | |
| Treatment | enum `Standard`/`LowUse`/`Qualifying` | Decided by the engine for this site (R8) |
| AnnualKwh | int | |
| VatStandardRate, VatReducedRate, CclRatePencePerKwh | decimal(18,6) | Rates used for this site |
| QualifyingFraction | decimal(9,6) | After the whole-supply rule |
| CurrentAnnualCostExVatCcl, CurrentAnnualCostIncVatCcl | decimal(28,10)? | Only when current rates are complete (R11) |

## NoQuoteResult 🔒

| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| PricingRunSiteId | int | FK |
| SupplierName | string(100) | As returned |
| TermMonths | int? | When the rate gave a term |
| Reason | string(1000) | `NoQuoteReason` exactly as returned (FR-036) |

## PriceOption

One supplier product and term from a run, with totals across the run's sites (FR-033, FR-073).
Everything except the selection fields is append-only.

| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| PricingRunId | int | FK |
| SupplierName | string(100) | The pricing name as returned |
| SupplierId | int? | Matched admin supplier |
| PlanType | string(50)? | As returned |
| TermMonths | int | |
| UnitRateUplift, StandingChargeUplift | decimal(18,6) | The uplifts used for this supplier and term (limits and approvals are checked against these) |
| SitesCovered, SitesTotal | int | |
| IsPartial | bool | SitesCovered < SitesTotal |
| AnyOutsidePricingWindow | bool | |
| HasNoStandingCharge | bool | Every covered site's standing charge parses as 0 (FR-026) |
| IsGreen | bool? | Null until POWWR identifies green products (Q7); the filter is hidden while null |
| TotalAnnualCostExVatCcl, TotalCcl, TotalVat, TotalAnnualCostIncVatCcl | decimal(28,10) | Summed over covered sites |
| TotalSavingExVatCcl, TotalSavingIncVatCcl | decimal(28,10)? | Only when every covered site has a saving |
| CommissionContractTotal, CommissionUpfront, CommissionMonthly | decimal(28,10) | R9 |
| IsSelected | bool | Changed by the agent, audited (FR-040) |
| IsRecommended | bool | At most one per run, and only among selected options |

Index: (PricingRunId, SupplierName, TermMonths, PlanType), unique.

**Limit and approval status isn't stored**: it is worked out each time it is read, by `UpliftPolicy`
(R13).

## PriceOptionSite 🔒

The per-site breakdown of one option.

| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| PriceOptionId | int | FK |
| PricingRunSiteId | int | FK |
| PowwrRateId | int? | `Rate.Id` |
| **As returned** (strings, shown exactly) | | `StandingCharge`, `DayUnitrate`, `NightUnitrate`, `WendUnitrate`, `Stod1`–`Stod6`, `CapacityCharge`, `ExcessCapacityCharge`, `ReactiveRate`, `FixedFee`, `FixedFeeUplift`, `FixedFeeSCUplift`, `DirectDebitDiscount`, `ChequeSurcharge`, `TransportationCharge`, `Fits`, `Uplift`, `StandingChargeUplift`, `AnnualPrice`, `Commission`, `TariffCode`, `Period`, `ContractEndDate` |
| RawAnnualPrice, RawBaseAnnualPrice, RawAnnualPriceInclusive, RawCommission, FlatDirectDebitDiscount | decimal(28,10)? | `Raw*` values exactly as returned |
| OutsidePricingWindow | bool | |
| PricebookVersion | string(50)? | FR-041 |
| QuoteRateReference | string(100)? | |
| ExtraInfo, FooterMessage | nvarchar(max)? | **Agent-only** supplier notes (FR-033, FR-049) |
| BaseDayRate, BaseNightRate, BaseWendRate, BaseStandingCharge | decimal(18,6)? | **Agent-only**: returned rate minus uplift (R7) |
| AnnualCostExVatCcl | decimal(28,10) | = RawAnnualPrice |
| Ccl, Vat, AnnualCostIncVatCcl | decimal(28,10) | R8 |
| SavingExVatCcl, SavingIncVatCcl | decimal(28,10)? | R11 |
| CommissionContractTotal, CommissionUpfront, CommissionMonthly | decimal(28,10) | R9 |
| CrossCheckAnnualCost, CrossCheckDifference | decimal(28,10)? | R7. Logged; never shown to customers |

## ApprovalRequest

| Field | Type | Rules |
|---|---|---|
| Id | int | PK |
| QuoteId | int | FK |
| PricingRunId | int | The run the agent was looking at |
| RequestedById | int | FK StaffMember |
| RequestedUtc | DateTime | |
| Reason | string(1000) | Required (FR-061) |
| Status | enum `Pending`/`Approved`/`Declined` | |
| DecidedById | int? | Must not equal RequestedById (FR-063) |
| DecidedUtc | DateTime? | |
| DecisionComment | string(1000)? | Required when Declined (FR-062) |
| RowVersion | rowversion | Stops two managers deciding at once |

```text
Pending ──approve (manager ≠ requester)──▶ Approved
Pending ──decline + comment (manager ≠ requester)──▶ Declined
```

Approved and Declined are final. Raising an uplift above the approved value needs a **new** request
(FR-065). Every transition writes an AuditEntry and a reporting record (FR-067).

## ApprovalItem

| Field | Type | Rules |
|---|---|---|
| Id | int | PK |
| ApprovalRequestId | int | FK, cascade |
| SupplierId | int | |
| SupplierName | string(100) | Snapshot |
| TermMonths | int | |
| UnitRateUplift, StandingChargeUplift | decimal(18,6) | The values approved, or to be approved |
| LimitUnitRate, LimitStandingCharge | decimal(18,6)? | The requester's limits when they asked, shown to the manager |

## CustomerPdf 🔒

| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| QuoteId | int | FK |
| PricingRunId | int | FK |
| GeneratedById | int | FK StaffMember |
| GeneratedUtc | DateTime | |
| PricesFetchedOn | DateOnly | UK date of the run's FetchedUtc. Drives the Outlook check (FR-076) |
| FileName | string(160) | FR-048 |
| StorageKey | string(300) | IFileStore key (R20) |
| SizeBytes | int | |
| Sha256 | string(64) | |
| OptionIds | string(400) | The PriceOption IDs shown; the recommended one is marked |
| WordingVersions | JSON | Map of wording key → SettingVersion.Id used (FR-051, Principle IX) |
| IncludedVatCcl | bool | |
| IsMicrobusiness | bool | |

## OutlookDraft 🔒

| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| CustomerPdfId | int | FK |
| QuoteId | int | FK |
| CreatedById | int | FK StaffMember |
| CreatedUtc | DateTime | FR-057, US4-4 |
| Method | enum `Graph`/`Eml` | R3 |
| GraphMessageId | string(200)? | |
| WebLink | string(1000)? | Used only to open the draft for the agent who made it |

## PendingUpload and PendingUploadRow

An upload in progress (R23). Valid rows wait here while the agent fixes the bad ones.

| PendingUpload | Type | Notes |
|---|---|---|
| Id | int | PK |
| QuoteId | int | FK, cascade |
| UploadedById | int | |
| UploadedUtc | DateTime | |
| FileName | string(200) | |
| Format | enum `Csv`/`Xlsx` | |

| PendingUploadRow | Type | Notes |
|---|---|---|
| Id | int | PK |
| PendingUploadId | int | FK, cascade |
| RowNumber | int | As in the file |
| Values | JSON | Column → raw text |
| Errors | JSON | List of `{ column, message }`; empty when the row is valid |

When every row is valid, the agent adds the rows as `Site`s, and the pending upload is deleted. This is
working data, not audit data. The upload itself is audited (who, when, file name, row and error
counts).

## Supplier

The admin supplier list (FR-081).

| Field | Type | Rules |
|---|---|---|
| Id | int | PK |
| PricingName | string(100) | Unique. Exactly as POWWR's pricing API expects (for example "British Gas") |
| DisplayName | string(100) | |
| SuppliesElectricity, SuppliesGas | bool | |
| IsActive | bool | Inactive suppliers are left out of QuoteDefinitions |
| UpfrontPercent | decimal(9,4) | 0–100, the commission payment model (R9). Seeded with 0 |
| MaxUnitRateUplift | decimal(18,6)? | p/kWh; a hard limit (FR-059, FR-066). Blank means none |
| MaxStandingChargeUplift | decimal(18,6)? | p/day; a hard limit |
| RowVersion | rowversion | |

## SupplierAlias

Maps the meter lookup's identifiers to a supplier (R5, US8-4).

| Field | Type | Rules |
|---|---|---|
| Id | int | PK |
| SupplierId | int | FK, cascade |
| PowwrId | string(20)? | For example `BRITISHG-001-S` |
| Mpid | string(10)? | For example `ENRD` |
| LookupName | string(200)? | For example "British Gas Business" |

At least one of the three is required. Unique where present.

## UpliftLimit

Maximum uplifts per role or per person, per fuel (FR-058).

| Field | Type | Rules |
|---|---|---|
| Id | int | PK |
| Scope | enum `Role`/`StaffMember` | |
| RoleName | enum `Agent`/`Manager`? | When Scope is Role |
| StaffMemberId | int? | When Scope is StaffMember; replaces the person's role limit |
| Fuel | enum | |
| MaxUnitRateUplift | decimal(18,6)? | Blank means no limit |
| MaxStandingChargeUplift | decimal(18,6)? | |

Unique (Scope, RoleName, StaffMemberId, Fuel).

## SettingVersion 🔒

Versioned scalar and wording settings (R19). The current value is the highest Version for a Key.

| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| Key | string(80) | See the keys below |
| Version | int | 1, 2, 3… per key |
| ValueJson | nvarchar(max) | |
| IsPlaceholder | bool | For wording; true until compliance approves it (FR-050) |
| CreatedById | int? | Null for seed data |
| CreatedUtc | DateTime | |

Unique (Key, Version).

**Keys**:

| Group | Keys |
|---|---|
| Default uplifts | `Uplift.Default.Electricity.UnitRate`, `Uplift.Default.Electricity.StandingCharge`, `Uplift.Default.Gas.UnitRate`, `Uplift.Default.Gas.StandingCharge` |
| Commission and payment | `Commission.Basis`, `Payment.Default` |
| Microbusiness | `Microbusiness.Threshold.Electricity.Kwh` (100000), `Microbusiness.Threshold.Gas.Kwh` (293000) |
| Large gas sites (blank to start) | `LargeGas.Limit.AnnualQuantityKwh`, `LargeGas.Limit.MeterCapacity`, `LargeGas.Limit.DailyMeteredQuantity` |
| Retention | `Retention.Years` (6) |
| Customer wording | `Wording.ValidityStatement` (supports `{PricesFetchedDate}`), `Wording.IndicativeNotice`, `Wording.CreditCheck` (starts as the spec's sentence, still marked placeholder until compliance approves it), `Wording.CommissionDisclosure` (supports the placeholders in R18), `Wording.Microbusiness`, `Wording.VatCclNote`, `Wording.Terms` |
| Terms address | `Terms.Url` |

## EffectiveRate 🔒

Effective-dated rates and thresholds (FR-031).

| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| Kind | enum | `VatStandard`, `VatReduced`, `CclElectricity`, `CclGas` (p/kWh), `LowUseElectricityKwhPerDay`, `LowUseGasKwhPerDay`, `QualifyingWholeSupplyPercent` |
| Value | decimal(18,6) | |
| EffectiveFrom | DateOnly | |
| CreatedById | int? | |
| CreatedUtc | DateTime | |

Index: (Kind, EffectiveFrom), unique. A lookup takes the latest row with `EffectiveFrom ≤ date`. Only
rows whose date hasn't arrived can be deleted, and that is audited.

## AuditEntry 🔒

The append-only history of every action (FR-067, FR-079, FR-082, FR-086; Principle V).

| Field | Type | Notes |
|---|---|---|
| Id | long | PK |
| OccurredUtc | DateTime | |
| ActorId | int? | Null for system jobs |
| ActorName | string(200) | Snapshot ("System" for jobs) |
| Action | string(60) | See the list below |
| EntityType | string(60) | |
| EntityId | string(40) | |
| QuoteId | int? | Indexed; drives the quote history timeline (US7-6) |
| OldValueJson, NewValueJson | nvarchar(max)? | Never contain secrets |
| Detail | string(1000)? | |

**Actions**:

| Area | Actions |
|---|---|
| Quote | `QuoteCreated`, `QuoteInputsChanged`, `SiteAdded`, `SiteRemoved`, `SitesUploaded` |
| Pricing | `PricingRequested`, `PricingReturned`, `PricingFailed`, `FailedSitesRetried` |
| Uplift and selection | `UpliftChanged`, `OptionsSelected`, `RecommendedChanged` |
| Approvals | `ApprovalRequested`, `ApprovalApproved`, `ApprovalDeclined` |
| PDF and Outlook | `PdfGenerated`, `PdfDownloaded`, `OutlookDraftCreated`, `OutlookDraftFailed` |
| Warnings | `DomesticConfirmed` |
| Admin | `SettingChanged`, `RateAdded`, `RateDeleted`, `SupplierChanged`, `LimitChanged`, `ReportExported` |
| System | `RetentionRun` |

## ReportingRecord 🔒

One quote event (FR-084). The payload format is in
[contracts/reporting-record.md](contracts/reporting-record.md).

| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK; used in the export file name |
| SchemaVersion | int | 1 |
| EventType | enum | `QuoteRequested`, `PricesReturned`, `Repriced`, `ApprovalRequested`, `ApprovalDecided`, `PdfGenerated`, `OpenedInOutlook`, `Failed` |
| OccurredUtc | DateTime | Indexed |
| QuoteId | int | Indexed |
| QuoteReference | string(16) | |
| AgentId | int | Indexed |
| AgentName, AgentEmail | string | |
| Fuel, QuoteType | enum | Indexed |
| Regions | string(200) | Distinct regions or LDZs, comma-separated; filtered with LIKE |
| RecommendedCommissionTotal | decimal(28,10)? | For the Reports page totals (R21) |
| PdfGenerated, OpenedInOutlook | bool | |
| Payload | nvarchar(max) | The full JSON document |

## ReportingRecordSupplier 🔒

(ReportingRecordId, SupplierName). Composite PK, with an index on SupplierName for the supplier filter.

## ReportingOutboxEntry

| Field | Type | Notes |
|---|---|---|
| ReportingRecordId | Guid | PK and FK |
| Attempts | int | |
| NextAttemptUtc | DateTime | Indexed with ExportedUtc |
| ExportedUtc | DateTime? | Null while pending |
| ExportPath | string(300)? | `quotes/yyyy/MM/dd/…json` |
| LastError | string(1000)? | Never contains secrets |

This table is the one exception to append-only among the reporting tables: the worker updates it.

---

## Validation rules by requirement

| Requirement | Rule | Where |
|---|---|---|
| FR-005 to FR-007 | Required business name and type; company number format; email, UK phone and UK postcode formats | `QuoteInputValidator` |
| FR-008 | Postcode normalised to the full format with a space before the lookup | `Postcode.Normalise` |
| FR-015 to FR-017 | Top line 8 or 9 characters; fields must agree with it; 13-digit core with a check digit; region | `MpanTopLine`, `MpanCore` |
| FR-018 | Half-hourly requires kVA | `QuoteInputValidator` |
| FR-022 | Registers > 0, or total + split = 100% | `QuoteInputValidator`, `RegisterSplit` |
| FR-024 | Start date not in the past; at least one term; payment method | `QuoteInputValidator` |
| FR-025 | Uplifts ≥ 0 | `QuoteInputValidator` |
| FR-029 | Qualifying % between 0 and 100 when Qualifying | `QuoteInputValidator` |
| FR-043 | MPRN 6–10 digits | `Mprn` |
| FR-045 | Large gas site flagged and not priced | `LargeGasSiteTest` |
| FR-070 | Every upload row through the same rules; repeated meter or wrong fuel is an error | `SiteUploadValidator` |
| FR-012 | A domestic meter needs the agent's confirmation before pricing | `QuoteInputValidator` (checks the confirmation flag) |
