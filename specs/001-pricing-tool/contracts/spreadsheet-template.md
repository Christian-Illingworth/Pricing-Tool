# Contract: multi-site spreadsheet template

Downloaded from `GET /Templates/MultiSite?fuel=…&format=csv|xlsx` (FR-069). One row is one site and its
meter. Every row is validated with the same rules as a site entered on the form (FR-070, research
R23).

## Format

| | CSV | Excel (.xlsx) |
|---|---|---|
| Encoding and sheet | UTF-8 (a BOM is accepted), comma-separated, RFC 4180 quoting | First sheet "Sites". A hidden sheet "Lists" feeds the data-validation drop-downs |
| Header | Row 1, exact column names below (case-insensitive, surrounding spaces ignored, unknown columns are an error) | The same |
| Dates | `yyyy-MM-dd` or `dd/MM/yyyy` | A date cell, or text in either format |
| Numbers | Plain digits; a decimal point for rates; no thousands separators (`12,000` is an error in CSV) | A number cell or digit text |
| Meter numbers | **Must be text.** The xlsx template formats those columns as Text, so Excel doesn't drop leading zeros or show `1.6E+12` | The same |
| Limits | At most 2 MB and 200 data rows | The same |

The template contains the header row and one example row of made-up data. Agents delete the example
row before uploading. A row that exactly matches the example is rejected with "Delete the example row".

## Electricity columns

| Column | Required | Rule |
|---|---|---|
| `AddressLine1` | Yes | At most 200 |
| `AddressLine2` | No | |
| `Town` | No | |
| `Postcode` | Yes | UK postcode, normalised |
| `MpanTopLine` | No | 8 digits or 9 characters (FR-015) |
| `MpanCore` | Yes | 13 digits, valid check digit (FR-017) |
| `ProfileClass` | Yes if there's no top line | 0–8; must agree with the top line |
| `MeterTimeswitchClass` | Yes if there's no top line | 3 digits; must agree |
| `LineLossFactorClass` | Yes if there's no top line | 3 characters; must agree |
| `StandardSettlementConfig` | No | 4 digits; must agree with a 9-character top line |
| `DuosTariffId` | No | 3 characters; must agree with a 9-character top line |
| `MhhsMigrated` | No | `Yes` or `No` |
| `MeasurementClass` | No | One letter A–G |
| `SmartMeter` | No | `Yes` or `No` (default No) |
| `KvaCapacity` | Yes when half-hourly | Whole number > 0 (FR-018) |
| `KvarhConsumption` | No | Whole number ≥ 0, half-hourly only |
| `DayKwh` | Yes | Whole number > 0. The single-rate consumption for a single-rate meter |
| `NightKwh` | When the meter has a night register | Whole number > 0 |
| `EveningWeekendKwh` | When the meter has an evening/weekend register | Whole number > 0 |
| `CurrentSupplier` | No | Matched to the supplier list's pricing name or an alias. An unknown name is a **warning**, not an error |
| `CurrentDayRate`, `CurrentNightRate`, `CurrentEveningWeekendRate` | No | p/kWh, ≥ 0 |
| `CurrentStandingCharge` | No | p/day, ≥ 0 |
| `CurrentContractEndDate` | No | Date |
| `StartDate` | No | Date, not in the past. Overrides the quote's start date for this site (FR-071) |

The register columns are checked against the meter's rate structure only after `/prompts` (research
R16), which runs when the valid rows are added. Until then, a structure mismatch shows on the site as
an amber notice; it never prices silently.

## Gas columns

| Column | Required | Rule |
|---|---|---|
| `AddressLine1`, `AddressLine2`, `Town`, `Postcode` | As for electricity | |
| `Mprn` | Yes | Digits only, 6–10 (FR-043) |
| `AnnualQuantityKwh` | Yes | Whole number > 0. Checked against the large-site AQ limit (FR-045) |
| `SmartMeter` | No | `Yes` or `No` |
| `CurrentSupplier`, `CurrentUnitRate`, `CurrentStandingCharge`, `CurrentContractEndDate`, `StartDate` | No | As for electricity |

## Whole-file rules

- **Wrong fuel**: an electricity quote with an `Mprn` column or value, or a gas quote with an
  `MpanCore`, gives the error "This row is for the other fuel".
- **Repeated meter**: a meter number repeated within the file, or one already on the quote, gives
  "MPAN 16000… is already on row 4" or "…already on this quote".
- **Nothing priced until fixed**: no site is priced while any row has an error (FR-070, US6-2).

## Error display

The per-row error table shows: Row (as numbered in the file, header = row 1) · Column · Value as given
· Message. For example: `Row 7 · MpanCore · 1600000001014 · The check digit is wrong. Check the MPAN on
the bill.`
