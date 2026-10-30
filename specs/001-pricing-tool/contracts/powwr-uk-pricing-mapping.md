# Contract: mapping to and from POWWR UK Pricing API 1.2.0

Source of truth: `docs/powwr/uk-pricing-api-1.2.0.json`. This file defines how `PowwrPricingMapper`
turns our domain into POWWR's request and back. Every row is covered by a unit test
(`PowwrPricingRequestMappingTests` and `PowwrPricingResponseMappingTests`).

| Setting | Value |
|---|---|
| Base URL | Mock: none (in-process). SwaggerHub: `https://virtserver.swaggerhub.com/POWWR/UkPricingAPI/1.2.0`. Live: `https://udcoreapi.co.uk/Service.svc/web` |
| JSON | PascalCase property names as in the definition (no naming policy). Nulls are left out. `Raw*` values are read as `decimal` |
| Auth | `SecurityDetails` in the body. There's no header auth |

## `POST /electricprices` and `POST /gasprices`

The request body is `{ "quoteDetails": QuoteDetails }`.

### QuoteDetails

| POWWR field | Our source | Notes |
|---|---|---|
| `QuoteReference` | `Quote.Reference` | For example `CUQ-26-000042` |
| `SecurityDetails.LicenceCode` | `Powwr:Pricing:LicenceCode` (user secret) | **Masked as `***`** in the stored copy and in logs |
| `SecurityDetails.mascaradeuser` | Signed-in agent's `DisplayName` | Q13 open |
| `PostCode` | `Site.Postcode` (normalised) | |
| `Renewal` | `QuoteType == Renewal` | |
| `COT` | `QuoteType == ChangeOfTenancy` | |
| `CotDate` | `Quote.CotDate` as `yyyy-MM-dd` | Only when COT |
| `OutOfContract` | `Quote.OutOfContract` | |
| `CurrentSupplier` | Current supplier's `PricingName`, else the text as returned or typed | |
| `PaymentMethod` | `PaymentMethodText.For(method, frequency, amount)` | For example `"Monthly Direct Debit"`, `"Quarterly Fixed Direct Debit"`, `"BACS"`, `"Cash/Cheque"`. Q4: the exact accepted strings |
| `BusinessType` | `BusinessTypeText.For(type)` | A table in configuration (`Powwr:Pricing:BusinessTypes`), starting as our labels, until POWWR gives its allowed values (Q4) |
| `Uplift` | `Quote.Contract.DefaultUnitRateUplift` | p/kWh (Q5) |
| `StandingChargeUplift` | `Quote.Contract.DefaultStandingChargeUplift` | p/day (Q5) |
| `QuoteDefinitions[]` | One per supplier that is **active**, supplies the fuel and passes the include/exclude filter | `Supplier` = `PricingName` |
| `QuoteDefinitions[].Plans[]` | One per requested term | `Duration` = months; `Uplift` and `StandingChargeUplift` = the override for (supplier, term), else the quote default; `PlanType` left out (Q4); `Id` left out |
| `Settings` | `[{ "key": "ShowNoQuoteReason", "value": "true" }]` | Always sent |
| `SortByCommission` | `false` | The app sorts for itself |
| `ElectricSupply` or `GasSupply` | See below | Exactly one |

### ElectricSupply

| POWWR field | Our source | Notes |
|---|---|---|
| `MPANTop` | `Meter.MpanTopLine` | 8 or 9 characters, as entered |
| `MPANBottom` | `Meter.MpanCore` | 13 digits |
| `ProfileClass` | `Meter.ProfileClass` | Integer, converted from the lookup's `"03"` |
| `MeterTimeswitchClass` | `Meter.MeterTimeswitchClass` | Integer |
| `LineLossFactorClass` | `Meter.LineLossFactorClass` | String, 3 characters |
| `StandardSettlementConfig` | `Meter.StandardSettlementConfig` | |
| `DuosTariffId` | `Meter.DuosTariffId` | Sent when known |
| `MhhsMigrated` | `Meter.MhhsMigrated` | Nullable |
| `ForcedRateStructure` | `Meter.ForcedRateStructure` | Only for an MHHS meter with an empty SSC (FR-021) |
| `MeasurementClass` | `Meter.MeasurementClass` | |
| `MeterType` | `Meter.MeterType` | As returned by the lookup (Q4) |
| `SmartMeter` | `Meter.SmartMeter` | |
| `DayConsumption.Amount` | Day kWh (or the single register) | Integer |
| `NightConsumption.Amount` | Night kWh | When the structure has a night register |
| `WendConsumption.Amount` | Evening and weekend kWh | When the structure has one |
| `KvaConsumption.Amount` | `Meter.KvaCapacity` | Half-hourly only |
| `KvarhConsumption.Amount` | `Meter.KvarhConsumption` | Half-hourly, optional |
| `ContractRenewalDate` | Site start date, else the quote start date | **Assumed to be the new contract's start date (Q9)** |
| `ContractEndDate` | `Site.CurrentContractEndDate` | Optional |
| `NewContractEndDate` | Start + term − 1 day, only when one term is requested; otherwise left out | Q9 |
| `CurrentSupplier` | As for QuoteDetails | |

### GasSupply

| POWWR field | Our source |
|---|---|
| `MPR` | `Meter.Mprn` |
| `Consumption.Amount` | `Site.GasAnnualQuantityKwh` |
| `SmartMeter` | `Meter.SmartMeter` |
| `ContractRenewalDate`, `ContractEndDate`, `NewContractEndDate`, `CurrentSupplier` | As for electricity |

### Response: `{ "GetElectricRatesResult" | "GetGasRatesResult": QuoteResult }`

| POWWR field | Mapping |
|---|---|
| `Error.Message` / `ErrorDetail` | When present, the response is `Succeeded = false`, `ErrorCategory = PowwrError`. The agent sees the message; `ErrorDetail` is logged only |
| `Reference` | `PricingRunSite.PowwrReference` |
| `FooterNotes` | `PricingRunSite.FooterNotes`. **Agent-only** |
| `Rates[]` with `NoQuoteReason` set | `NoQuoteResult(SupplierName = Supplier, TermMonths = int.TryParse(Term), Reason = NoQuoteReason)` |
| `Rates[]` without `NoQuoteReason` | `ReturnedRate` → `PriceOptionSite`. Every string field is kept exactly; the `Raw*` decimals are kept exactly |
| `Rate.Term` | `int` months. A rate whose term isn't a number is rejected and logged (`InvalidResponse`) |
| `Rate.RawAnnualPrice` | Required for a priced rate; a missing or negative value makes the rate invalid (logged; the supplier is shown as "couldn't be priced: invalid response") |
| `Rate.Uplift`, `Rate.StandingChargeUplift` | Parsed as decimals (invariant culture) to work out base rates (research R7) |
| `Rate.OutsidePricingWindow` | Flag shown to the agent (spec edge case) |
| `Rate.PricebookVersion` | Stored (FR-041) and compared with `/pricechange` (research R14) |
| `Rate.ExtraInfo`, `Rate.FooterMessage` | Agent-only notes; **never** in the customer model |

The HTTP status isn't trusted. The definition only has a `default` response, so the `Error` object is
always checked. HTTP 429 and 503 are retried by `PowwrRetryHandler` (research R6). A timeout, or any
other non-2xx response without a parseable body, becomes a failure in the matching category.

## `POST /prompts`: rate structure lookup

The request is `{ "promptsSearch": PromptsSearch }`.

| POWWR field | Source |
|---|---|
| `SecurityDetails` | As above |
| `MPANTop` | Legacy 8-digit top line only. Left out for a 9-character top line (Q15) |
| `MPANBottom` | Core |
| `ProfileClass`, `MeterTimeswitchClass`, `LineLossFactorClass`, `StandardSettlementConfig`, `DuosTariffId`, `MhhsMigrated` | As for ElectricSupply |
| `Suppliers` | Left out, which gives the maximum structure across all suppliers |

The response is `GetNumberOfPromptsResult` → `{ NumberOfPrompts, ThePrompts[], Error }`. `ThePrompts` is
mapped to registers as in research R16.

## `POST /pricechange`

The request is `{ "priceChangeRequest": { SecurityDetails, SupplierName?, IncludeFuture: false } }`.

The response is `GetLatestPriceChangeResult.PriceChanges[]` → `SupplierPriceChange`. `Utility` is
`"electric"` or `"gas"`, and `Duration` is in months.

## `GET /ping`

`PingResult` is any non-empty string, which means healthy. It is used by a developer health check on
the Settings page (Admin only), never on the agent's path.

## Mock adapter behaviour (`MockPowwrPricingClient`)

The mock is deterministic for a given MPAN or MPRN and consumption, so tests and demos repeat. It
uses made-up data only.

**Suppliers**: British Gas, EDF, E.ON Next, Octopus Energy, Opus Energy, Scottish Power, Smartest
Energy, Valda Energy, Crown Gas & Power and Yu Energy. Each one prices every requested term from 12 to
60 months.

**How the figures are built**:
- **Base rates**: day 22–28 p/kWh, night 14–18, evening/weekend 16–20 (from a seeded random), plus 2%
  per extra year of term; standing charge 40–90 p/day; gas 5–8 p/kWh and 25–60 p/day.
- **Uplift** applied: the plan's uplift, else the quote's.
- **`RawAnnualPrice`**: Σ(rate × kWh) / 100 + SC × 365 / 100, so the cross-check passes.
- **`RawBaseAnnualPrice`**: the same calculation without uplift.
- **`RawCommission`**: RawAnnualPrice − RawBaseAnnualPrice, a per-year figure.
- **`PricebookVersion`**: `MOCK-{yyyyMMdd}`.

**Variety in every result**:
- Valda Energy declines with `NoQuoteReason = "Meter type not supported"`.
- Yu Energy's 60-month plan is `OutsidePricingWindow`.
- Octopus Energy has a `FooterMessage`.
- Opus Energy returns one product with standing charge `"0.00"` (for the no-standing-charge filter).

**Scenario triggers** are in [../quickstart.md](../quickstart.md):

| MPAN or MPRN | Response |
|---|---|
| MPAN `2300000000019` | Every supplier declines |
| MPRN `9999999` | A POWWR `Error` |
| MPAN `1200000000020` (an MHHS meter with no SSC) | Only British Gas and Octopus Energy price it |

`/pricechange` returns `MOCK-{today}` for every supplier, except when the configuration value
`Powwr:Mock:ChangedSuppliers` lists suppliers, which then return `MOCK-CHANGED`. This exercises FR-052.
