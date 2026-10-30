# Contract: reporting record (schema version 1)

Every quote event (FR-084) produces one JSON document:
- It is stored in `ReportingRecord.Payload`, in the **same** transaction as the action.
- It is exported by the outbox worker to the reporting store as
  `quotes/yyyy/MM/dd/{HHmmssfff}-{eventType}-{recordId}.json` (UTC date and time of the event).

The machine-readable schema is [reporting-record.v1.schema.json](reporting-record.v1.schema.json).
`ReportingRecordBuilder` writes it, and `ReportingRecordBuilderTests` checks it.

## Rules

- **`schemaVersion`** is always present. Adding optional fields keeps version 1. Renaming or removing a
  field, or changing a field's meaning, means version 2, and older files are never rewritten.
- **Self-contained documents**: every record carries the full quote snapshot at the time of the event
  (customer, sites, contract, the latest run's prices and options), so Power BI needs no joins. FR-084
  lists these fields for *each* record.
- **Money and energy figures** are pairs: `{ "value": <full precision>, "display": <rounded to 2 dp> }`.
  `display` uses the same `Rounding.ToPennies` as the screen and PDF, so all three match (SC-004).
  POWWR's rate strings are copied exactly in `returned`.
- **Times and dates**: times are UTC ISO 8601 (`2026-10-30T10:15:04.123Z`). Dates are `yyyy-MM-dd`.
- **Never in a record**: secrets, tokens, the licence code, POWWR's raw request or response, the
  customer's contact name, email address or phone number (FR-084), or the agent's Graph data beyond
  their name and email.
- **Commission basis and capping**: `pricingRun.commissionBasis` gives the basis used for every price
  in the record, and each price and option says whether it was `cappedAtSupplierMax` (FR-084).
- **Access**: records contain customer and commission data, so they are visible to Admins only in the
  app (FR-088). Access to the export location is restricted in the same way (local folder now, Blob
  later).

## Event types

| `eventType` | Raised when | Extra section |
|---|---|---|
| `QuoteRequested` | A quote is first priced (before POWWR is called) | none |
| `PricesReturned` | The first pricing run completes, fully or partly | `pricingRun` |
| `Repriced` | Any later run completes, including a retry of failed sites | `pricingRun` |
| `PdfGenerated` | A PDF is stored | `pdf` |
| `OpenedInOutlook` | A draft is created (Graph) or downloaded (Eml) | `outlook` |
| `Failed` | A pricing call, a PDF render or an Outlook draft fails | `failure` |

## Example (abridged, made-up data)

```json
{
  "schemaVersion": 1,
  "recordId": "8d4c6a3e-2f1b-4c9a-9e57-1b2c3d4e5f60",
  "eventType": "PricesReturned",
  "occurredUtc": "2026-10-30T10:15:04.123Z",
  "agent": { "id": 12, "name": "Alex Agent", "email": "alex.agent@example.com" },
  "quote": {
    "reference": "CUQ-26-000042",
    "powwrReferences": ["MOCK-REF-0001"],
    "quoteType": "Renewal",
    "outOfContract": false,
    "cotDate": null,
    "fuel": "Electricity",
    "isMicrobusiness": true,
    "microbusinessSource": "Consumption"
  },
  "customer": {
    "businessName": "Acme Trading Ltd",
    "businessType": "LimitedCompany",
    "companyNumber": "12345678",
    "postcode": "ZZ1 1AB"
  },
  "contract": {
    "startDate": "2026-12-01",
    "termsMonths": [12, 24, 36],
    "paymentMethod": "Monthly Direct Debit",
    "defaultUplift": { "unitRatePencePerKwh": 1.5, "standingChargePencePerDay": 5.0 },
    "upliftOverrides": [ { "supplier": "EDF", "termMonths": 36, "unitRatePencePerKwh": 2.0, "standingChargePencePerDay": 5.0 } ],
    "noStandingChargeOnly": false,
    "vatCclTreatment": "Standard",
    "qualifyingPercent": null,
    "includeVatCcl": false
  },
  "sites": [
    {
      "ordinal": 1,
      "postcode": "ZZ1 1AB",
      "electricity": {
        "mpanTopLine": "04845661", "mpanCore": "1600000001022",
        "profileClass": 4, "meterTimeswitchClass": 845, "lineLossFactorClass": "661",
        "standardSettlementConfig": "0151", "duosTariffId": null, "mhhsMigrated": null,
        "distributorId": "16", "region": "North Western",
        "isHalfHourly": false, "kvaCapacity": null, "smartMeter": true
      },
      "gas": null,
      "consumption": { "dayKwh": 63000, "nightKwh": 27000, "eveningWeekendKwh": null, "annualQuantityKwh": null, "totalKwh": 90000 },
      "currentSupplier": "British Gas",
      "currentRates": { "dayPencePerKwh": 30.1, "nightPencePerKwh": 19.2, "eveningWeekendPencePerKwh": null, "standingChargePencePerDay": 75.0 },
      "treatment": "Standard"
    }
  ],
  "pricingRun": {
    "runNumber": 1, "fetchedUtc": "2026-10-30T10:15:02.900Z", "status": "Succeeded",
    "commissionBasis": "PerYear"
  },
  "prices": [
    {
      "siteOrdinal": 1, "supplier": "EDF", "planType": null, "termMonths": 24,
      "returned": { "dayUnitrate": "26.912", "nightUnitrate": "17.450", "standingCharge": "62.00", "uplift": "1.5", "standingChargeUplift": "5" },
      "base": { "dayUnitrate": "25.412", "nightUnitrate": "15.950", "standingCharge": "57.00" },
      "annualCostExVatCcl": { "value": 21892.36, "display": 21892.36 },
      "ccl": { "value": 720.9, "display": 720.90 },
      "vat": { "value": 4522.652, "display": 4522.65 },
      "annualCostIncVatCcl": { "value": 27135.912, "display": 27135.91 },
      "savingExVatCcl": { "value": 2528.39, "display": 2528.39 },
      "commission": {
        "powwr": { "value": 1368.25, "display": 1368.25 },
        "contractTotal": { "value": 2736.5, "display": 2736.50 },
        "upfront": { "value": 0.0, "display": 0.00 },
        "monthly": { "value": 114.0208333333, "display": 114.02 }
      },
      "outsidePricingWindow": false,
      "cappedAtSupplierMax": false,
      "pricebookVersion": "MOCK-20261030"
    }
  ],
  "noQuotes": [ { "siteOrdinal": 1, "supplier": "Valda Energy", "termMonths": null, "reason": "Meter type not supported" } ],
  "options": [
    { "supplier": "EDF", "planType": null, "termMonths": 24, "isPartial": false, "selected": true, "recommended": true,
      "annualCostExVatCcl": { "value": 21892.36, "display": 21892.36 },
      "commissionContractTotal": { "value": 2736.5, "display": 2736.50 },
      "cappedAtSupplierMax": false }
  ],
  "pdf": { "generated": false, "pdfId": null, "fileName": null },
  "outlook": { "opened": false, "method": null },
  "failure": null
}
```

## Reports page columns and CSV export

The CSV has one row per record, with these columns:

| Group | Columns |
|---|---|
| Event | `OccurredUtc`, `EventType`, `QuoteReference` |
| Agent | `AgentName`, `AgentEmail` |
| Quote | `Fuel`, `QuoteType`, `OutOfContract`, `IsMicrobusiness` |
| Customer and sites | `CustomerName`, `BusinessType`, `CompanyNumber`, `Postcode`, `Regions`, `SiteCount`, `TotalKwh` |
| Recommended option | `RecommendedSupplier`, `RecommendedTermMonths`, `RecommendedAnnualCostExVatCcl`, `RecommendedCommissionTotal`, `RecommendedCommissionUpfront`, `RecommendedCommissionMonthly` |
| Outcome | `PdfGenerated`, `OpenedInOutlook`, `FailureCategory` |

Money columns use the `display` values. Every cell is escaped against formula injection (research
R21). The full detail of each record, including every price, is in the exported JSON for Power BI.
