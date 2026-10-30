# Contract: mapping from POWWR Meter Lookup Service 2.5.0

Source of truth: `docs/powwr/meter-lookup-api-2.5.0.json`. `MeterLookupMapper` implements this file,
and it is covered by `MeterLookupMappingTests`.

| Setting | Value |
|---|---|
| Base URL | Mock: in-process. SwaggerHub: `https://virtserver.swaggerhub.com/POWWR/MeterLookup.GasAndElec.V3.Service/2.5.0`. Sandbox: `https://meterlookupuk-sbx.dev-platform.powwr.net/api`. Live: given by POWWR (Q2) |
| Auth | OAuth2 client credentials. `POST {TokenUrl}`: `grant_type=client_credentials`, `client_id`, `client_secret` (user secret), `scope=udgs-lookupservices-meter-api brokerage:{OrganisationPowwrId}`. Sandbox token URL: `https://accounts.sbx.powwr.com/connect/token`. The token is cached until 60 s before `expires_in`. SwaggerHub mode sends no token |
| JSON | camelCase, read **case-insensitively** (POWWR's examples mix the casing) |

## `callerIdentification` (on every request)

| Field | Source |
|---|---|
| `organisationPowwrId` | `Powwr:MeterLookup:OrganisationPowwrId`, which must match `^[A-Z0-9]{1,8}-\d{3}-[SB]$` |
| `applicationId` | `"CU Pricing Tool"` |
| `userId` | The agent's Entra object ID (`StaffMember.EntraObjectId`) |

## `POST /Search/addressSearch`

- **Request**: `{ callerIdentification, postCode }`. `postCode` is always `Postcode.Normalise(input)`, the
  full postcode with one space. A postcode that fails validation never reaches POWWR.
- **Response**: `meterAddresses[]` → `LookupAddress`.

| POWWR field | Ours | Notes |
|---|---|---|
| `meterAddressId` | `MeterAddressId` | Used for `meterSearch` |
| `fullAddress` | `FullAddress` | |
| `mpanCount`, `mprnCount` | `ElectricMeterCount`, `GasMeterCount` | Addresses with 0 meters for the quote's fuel are hidden |
| `mpans[]`, `mprns[]` | `RedactedNumbers[]` | **Shown as hints only.** They may be redacted to the last 3 digits, so the full number always comes from `meterSearch` |

## `POST /Search/meterSearch`

- **Request**: `{ callerIdentification, meterAddressId | mpan | mprn }`. Exactly one is set. An MPAN or
  MPRN is validated first (research R15).
- **Response**: `electricMeters[]` and `gasMeters[]`. Only meters for the quote's fuel are used.

### ElectricMeter → `LookupElectricMeter`

| POWWR field | Ours | Conversion |
|---|---|---|
| `mpan` | `MpanCore` | Validated; an invalid number from POWWR is flagged and not prefilled |
| `profileClass` | `ProfileClass` | `"03"` → 3. Not numeric → null plus a warning |
| `meterTimeswitchClass` | `MeterTimeswitchClass` | `"801"` → 801 |
| `lineLossFactor` | `LineLossFactorClass` | Kept as a string |
| `standardSettlementConfiguration` | `StandardSettlementConfig` | String; empty → null |
| `measurementClass` | `MeasurementClass` | |
| `meterType` | `MeterType` | |
| `smetsVersion` | `SmetsVersion`; `SmartMeter` suggested true when not empty | |
| `gspId` | `GspId` | Stored for reporting |
| `isDomesticConsumer` | Warning `Domestic` (the agent must confirm) | FR-012 |
| `energisationStatus` | `"D"` → warning `DeEnergised` | |
| `greenDealStatus` | `"Active"` → warning `GreenDealActive` | |
| `tradingStatus` | Stored only | Meaning unknown (open item P-16) |
| `currentSupplier { mpid, shortCode, name, powwrId, effectiveDate }` | `ISupplierMatcher.Match(...)` → `CurrentSupplierId` and text | Not matched → warning `SupplierNotMapped` |
| `annualConsumption` | Prefills consumption (a total, split by the rate structure's registers once known) | |
| `meterPointLocation` | Site address lines and postcode | Case-insensitive |
| Top line | Not returned | Built only when PC, MTC and LLFC are all present: `PC:00 + MTC:000 + LLFC` = the legacy 8 digits, marked as derived. DUoS tariff ID and MHHS status aren't returned (FR-016) |

### GasMeter → `LookupGasMeter`

| POWWR field | Ours | Conversion |
|---|---|---|
| `mprn` | `Mprn` | Validated (6–10 digits) |
| `ldz`, `exitZone` | `Ldz`, `ExitZone` | Shown, not editable (FR-043) |
| `isIGT` | `IsIgt` → warning `Igt` | FR-046 |
| `meterCapacity`, `dailyMeteredQuantity` | Same names | Checked against the large-site limits (FR-045) |
| `annualConsumption` | AQ prefill | |
| `currentSupplier` | Matched as for electricity | |
| `incomingSupplier` | Present → warning `SwitchInProgress` (naming the supplier) | FR-012 |
| `additionalInformation.amrIndicator` | `"Y"` → smart meter suggested yes; otherwise no | FR-043 |
| `meterType`, `meterModel`, `meterMechanismCode` | Stored; `MeterType` shown | |

## Errors (RFC 7807)

| Response | `LookupResult.Status` | What the agent sees |
|---|---|---|
| 200 with an empty list | `NotFound` | "No addresses found for {postcode}. Check it, or enter the details yourself." |
| 400 `application/problem+json` | `Rejected` | "The meter lookup couldn't use that search." `title`, `detail` and `extensions.errors[].description` and `properties[].path` are logged, without `providedValue` if it could be personal data |
| 401 or 403 | `Unavailable` | "The meter lookup isn't available right now. Enter the details yourself." The token cache is cleared once and the call retried once |
| Timeout, 5xx or network | `Unavailable` | As above |

The lookup never blocks quoting (SC-005).

## Mock adapter behaviour (`MockMeterLookupClient`)

Every postcode uses the made-up `ZZ1` or `ZZ9` area, and every address and meter is made up.

| Postcode | Addresses and meters | Exercises |
|---|---|---|
| `ZZ1 1AA` | Unit 1, Test Park: electricity `1600000001013` (PC 03, MTC 801, LLFC 631, SSC 0393, single rate, 18,000 kWh, current supplier `BRITISHG-001-S` "British Gas Business") and gas `1234567` (AQ 40,000, LDZ NW, AMR Y) | US1-1, US2-1, supplier mapping |
| `ZZ1 1AB` | Unit 2: electricity `1600000001022` (PC 04, SSC 0151 Day/Night, 90,000 kWh) | US1-7 microbusiness |
| `ZZ1 1AC` | Unit 3: electricity `1600000001031` (10,950 kWh = 30 kWh a day) | US1-8 low use |
| `ZZ1 1AD` | Units 4–6: `1600000001040` (domestic), `1600000001050` (de-energised), `1600000001069` (Green Deal active) | US1-9 |
| `ZZ1 1AE` | Unit 7: electricity `1200000000011` (PC 00, measurement class C, half-hourly, 450,000 kWh) | FR-018 kVA |
| `ZZ1 1AF` | Unit 8: electricity `1200000000020` (MHHS, empty SSC) | FR-021 |
| `ZZ1 1AG` | Unit 9: gas `2345678` (IGT), gas `3456789` (incoming supplier E.ON Next), gas `7654321` (AQ 800,000, capacity 160, DMQ 2,500) | US2-3, US2-4, FR-012 |
| `ZZ9 9ZZ` | No addresses | `NotFound` |
| `ZZ9 9ZY` | Lookup unavailable | `Unavailable`, SC-005 |

A search by MPAN or MPRN finds any meter above. `2300000000019` also exists (an electricity meter with
no addresses) for the "every supplier declines" pricing scenario.
