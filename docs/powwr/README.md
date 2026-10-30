# POWWR API notes

Read from SwaggerHub (owner POWWR) on 29 October 2026. Download the full definitions into this folder by running these in PowerShell from the Pricing Tool folder:

```powershell
Invoke-WebRequest https://api.swaggerhub.com/apis/POWWR/UkPricingAPI/1.2.0 -OutFile docs\powwr\uk-pricing-api-1.2.0.json
Invoke-WebRequest https://api.swaggerhub.com/apis/POWWR/MeterLookup.GasAndElec.V3.Service/2.5.0 -OutFile docs\powwr\meter-lookup-api-2.5.0.json
```

If this summary and the downloaded files disagree, the files win.

## APIs we use

| API | Version | Used for |
|---|---|---|
| UK Pricing API (`UkPricingAPI`) | 1.2.0 | Electricity and gas prices, rate structure lookup, price change history |
| New POWWR Utility Meter Lookup Service (`MeterLookup.GasAndElec.V3.Service`) | 2.5.0 | Postcode → address → meter details at the start of a quote |

## APIs we don't use (for now)

- **PDF Generation API, DocuSign Generation API:** we make our own branded PDF, and contracts are out of scope.
- **Connected API:** submits deals to suppliers once a customer accepts. A possible later feature.
- **V3 Portal API:** a supplier-side portal API (Yu Energy contract statuses and data exports). Not relevant.

## UK Pricing API 1.2.0

- **Servers:** production `https://udcoreapi.co.uk/Service.svc/web`; mock `https://virtserver.swaggerhub.com/POWWR/UkPricingAPI/1.2.0`. No sandbox is listed.
- **Auth:** no OAuth. Every request body carries `SecurityDetails { LicenceCode, mascaradeuser }`. The licence code is a secret.
- **No field is marked required** in the spec.

**Endpoints**

- `POST /electricprices`: `{ quoteDetails }` with `ElectricSupply` → `GetElectricRatesResult` (QuoteResult)
- `POST /gasprices`: `{ quoteDetails }` with `GasSupply` → `GetGasRatesResult` (QuoteResult)
- `POST /prompts` (rate structure lookup): `PromptsSearch { MPANTop, MPANBottom, StandardSettlementConfig, MhhsMigrated, DuosTariffId, ProfileClass, MeterTimeswitchClass, LineLossFactorClass, Suppliers[] }` → `{ NumberOfPrompts, ThePrompts }`, e.g. `["Day","Night"]`. With no suppliers, returns the maximum number of rates across all suppliers.
- `POST /pricechange`: `{ SupplierName, IncludeFuture }` → the latest price change per supplier, utility, duration and product (`LastPriceChange`, `Version`, `HalfHourly`)
- `GET /ping`

**QuoteDetails:** `QuoteReference`, `SecurityDetails`, `ElectricSupply` or `GasSupply`, `PostCode`, `Renewal` (bool), `CurrentSupplier`, `COT` (bool), `CotDate`, `OutOfContract` (bool), `Uplift`, `StandingChargeUplift`, `PaymentMethod` (e.g. "Monthly Direct Debit"; must match Direct Debit, BACS or Cash/Cheque, can include Monthly/Quarterly and Fixed/Variable, and restricts which prices return), `QuoteDefinitions[] { Supplier, Plans[] { Duration, PlanType (e.g. "SC"), Uplift, StandingChargeUplift, Id } }`, `Settings[] { key, value }` (e.g. `ShowNoQuoteReason` = `true`), `SortByCommission` (bool), `BusinessType`.

**ElectricSupply:** `MPANTop` (8–9 characters; legacy or post-MHHS format), `MPANBottom` (13), `ProfileClass`, `MeterTimeswitchClass`, `LineLossFactorClass`, `StandardSettlementConfig`, `DuosTariffId`, `MhhsMigrated`, `ForcedRateStructure` (Standard, Economy7, EveningAndWeekend, EveningWeekendAndNight, OffPeak; only for MHHS-migrated meters with an empty SSC, and only some suppliers support it), `MeasurementClass`, `MeterType`, `SmartMeter`, `DayConsumption`, `NightConsumption`, `WendConsumption`, `KvaConsumption`, `KvarhConsumption` (each `{ Amount }`), `ContractRenewalDate`, `ContractEndDate`, `NewContractEndDate`, `CurrentSupplier`.

**GasSupply:** `MPR`, `Consumption { Amount }`, `ContractRenewalDate`, `ContractEndDate`, `NewContractEndDate`, `CurrentSupplier`, `SmartMeter`.

**Rate (each price returned):** `Supplier`, `Term`, `PlanType`, `StandingCharge`, `DayUnitrate`, `NightUnitrate`, `WendUnitrate`, `Stod1`–`Stod6`, `Fits`, `CapacityCharge`, `ExcessCapacityCharge`, `ReactiveRate`, `TransportationCharge`, `FixedFee`, `FixedFeeUplift`, `FixedFeeSCUplift`, `Uplift`, `StandingChargeUplift`, `DirectDebitDiscount`, `FlatDirectDebitDiscount`, `ChequeSurcharge`, `AnnualPrice`/`RawAnnualPrice`, `BaseAnnualPrice`/`RawBaseAnnualPrice`, `AnnualPriceInclusive`/`RawAnnualPriceInclusive`, `Commission`/`RawCommission`, `Renewal`, `OutsidePricingWindow`, `NoQuoteReason`, `ContractEndDate`, `PricebookVersion`, `QuoteRateReference`, `TariffCode`, `ExtraInfo`, `FooterMessage`, `Ref`, `Period`, `SC`, `Id`. `QuoteResult` also has `Reference`, `FooterNotes` and `Error { Message, ErrorDetail }`.

Rates come back as strings (e.g. `"13.84"`, `"32.00"`): show them as returned. The `Raw*` fields are decimals: use those for calculations.

## Meter Lookup Service 2.5.0

- **Servers:** sandbox `https://meterlookupuk-sbx.dev-platform.powwr.net/api`; SwaggerHub mock. No production URL is listed.
- **Auth:** OAuth2 client credentials. Sandbox token URL `https://accounts.sbx.powwr.com/connect/token`. Scopes: `udgs-lookupservices-meter-api` plus `brokerage:<ID>`. The listed brokerage IDs don't include Consultiv.
- **`callerIdentification`:** `organisationPowwrId` (required, format like `ABCDEFGH-001-B`), plus optional `applicationId` and `userId` for usage reports.

**Flow**

1. Get a token.
2. `POST /Search/addressSearch { callerIdentification, postCode }` (full postcode with a space) → `meterAddresses[] { meterAddressId, fullAddress, mpanCount, mpans, mprnCount, mprns }`. MPANs may be redacted to the last 3 digits.
3. `POST /Search/meterSearch { callerIdentification, meterAddressId | mpan | mprn }` → `electricMeters[]`, `gasMeters[]`.

**ElectricMeter:** `mpan`, `gspId` (region), `profileClass`, `meterTimeswitchClass`, `lineLossFactor`, `standardSettlementConfiguration`, `measurementClass`, `meterType`, `smetsVersion`, `isDomesticConsumer`, `energisationStatus` (and date), `tradingStatus`, `greenDealStatus`, `currentSupplier { mpid, shortCode, name, effectiveDate, powwrId }`, `annualConsumption`, `serialNumber`, `meterPointLocation` (address lines, `postCode`, `fullAddress`), `additionalInformation` (e.g. `meterOperatorMpid`).

**GasMeter:** `mprn`, `ldz`, `exitZone`, `isIGT`, `meterType`, `meterCapacity`, `meterMechanismCode`, `meterModel`, `dailyMeteredQuantity`, `currentSupplier`, `incomingSupplier`, `annualConsumption`, `serialNumber`, `meterPointLocation`, `additionalInformation` (e.g. `amrIndicator`).

**Errors:** RFC 7807 problem details, with `errors` and `warnings` lists.

**Not provided:** DUoS tariff ID, MHHS-migrated flag, contract end date, or an explicit half-hourly flag (use measurement class and profile class).

## Things to watch (checked against the downloaded definitions)

- **Supplier names differ between the two APIs.** The lookup returns e.g. `"British Gas Business"` with `powwrId` `BRITISHG-001-S`; the Pricing API's examples use `"British Gas"`. Map them through the admin supplier list.
- **Types differ.** The lookup returns `profileClass` and `meterTimeswitchClass` as strings (e.g. `"03"`, `"801"`); the Pricing API wants integers.
- **Postcodes** must be the full postcode with a space, or the lookup returns nothing.
- **`incomingSupplier`** on a gas meter means a switch is already in progress: warn the agent.
- **`greenDealStatus: Active`** on an electricity meter: flag it to the agent.
- **`dailyMeteredQuantity`** appears even on small gas meters (the example has 100 on a capacity-6 meter), so it can't on its own identify daily-metered sites. A threshold is needed (question 11).
- **Gas smart/AMR** isn't a top-level field; use `additionalInformation.amrIndicator`.
- **`/prompts` length rules look wrong:** `PromptsSearch.MPANTop` allows only 8 characters (legacy format only), and `LineLossFactorClass` is set to 8–9 characters, though LLFCs are 3 (question 15).
- The Pricing API only defines a `default` response, so check the `Error` object in every result rather than relying on HTTP status codes.

## Decisions made from these docs

- Meter details are pre-filled from the lookup, with manual entry as a fallback.
- Quote type maps to `Renewal`, `COT`/`CotDate` and `OutOfContract`.
- Payment method is on the form, because it changes which prices return.
- Uplifts can be set per supplier and term through `QuoteDefinitions` and `Plans`.
- Annual cost excluding VAT and CCL is `RawAnnualPrice`. Our pricing engine adds VAT and CCL using the customer's treatment.
- Commission is `RawCommission`, split into upfront and monthly by the supplier payment model in admin settings.
- The API gives no price expiry, so quotes record `PricebookVersion`, and `/pricechange` is used to warn when an old quote's prices have changed.
- The pricing-window warning uses each rate's `OutsidePricingWindow` instead of a fixed 6 months.
- Declines show `NoQuoteReason` (we send `ShowNoQuoteReason=true`).
- Multi-site quotes send one request per site, and we combine the results.
- Supplier maximum uplifts aren't in the API, so they're an admin setting.

## Open questions for POWWR

1. Is there a test environment or test licence code for the Pricing API? Are live calls charged or rate-limited?
2. Meter Lookup access for Consultiv: client ID and secret, `organisationPowwrId`, brokerage scope and the production URL. Is the service (marked unpublished) available to us?
3. What does `RawCommission` represent: per year, the whole contract, or the upfront payment?
4. What are the allowed values for `PlanType` (is there a no-standing-charge plan type?), `BusinessType`, `PaymentMethod` and `MeterType`?
5. What units are `Uplift` and `StandingChargeUplift` in (p/kWh and p/day?), and do POWWR or suppliers enforce maximum uplifts?
6. How long are returned prices valid?
7. Does any field identify green or renewable products?
8. What does `AnnualPriceInclusive` include (VAT rate, CCL)?
9. Which date is the new contract's start date (`ContractRenewalDate`?), and what is `NewContractEndDate` for?
10. How should we get `DuosTariffId` and `MhhsMigrated` when the lookup doesn't return them?
11. What thresholds should we use to treat a gas site as large or daily-metered?
12. Should `FooterMessage`, `FooterNotes` or `ExtraInfo` be shown to customers?
13. Should `mascaradeuser` be the agent's name, their email, or a fixed value?
14. How do the meter lookup's supplier names and IDs (`powwrId`, MPID) map to the supplier names the Pricing API expects? Is there a list?
15. For `/prompts`: should `MPANTop` accept the 9-character post-MHHS format, and is `LineLossFactorClass` really 8–9 characters?
