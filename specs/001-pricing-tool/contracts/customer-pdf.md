# Contract: customer PDF and the customer-facing model

The PDF is built **only** from `CustomerQuoteDocumentModel` (Principle II). The list below is the
complete allow-list of what can reach a customer. Adding a field means changing this contract and
`CustomerModelShapeTests`.

## Pre-conditions (checked on the server, FR-052 and FR-053)

1. The latest run's `Status = Succeeded`, so no site failed technically.
2. The run's prices were fetched today (UK time).
3. `/pricechange` shows no change for any selected supplier and term. If any has changed, the agent
   sees "{Supplier, …} have published new prices since you priced. Re-price first."
4. At least one option is selected, and exactly one selected option is recommended.
5. Every selected option's `UpliftPolicy` status is `Within` or `Approved`.

## `CustomerQuoteDocumentModel`

```text
CustomerQuoteDocumentModel
├─ Company                CompanyOptions: Name, AddressLines[], Phone, Email, Website,
│                         RegistrationNumber          (configuration, R19)
├─ Logo                   "cu/logo/cu-logo-250.png"    (colour logo, on white)
├─ QuoteReference, QuoteDate (UK), QuoteTypeLabel ("Renewal" | "New supplier" | "Change of tenancy")
├─ FuelLabel              "Electricity" | "Gas"
├─ Customer               BusinessName, ContactName, ContactEmail, ContactPhone
├─ Sites[]                Ordinal, AddressLines[], Postcode, MeterNumberLabel ("MPAN" | "MPRN"),
│                         MeterNumber (full: top line + core, or MPRN), StartDate
├─ Validity               Text (wording with {PricesFetchedDate} filled in), IsPlaceholder
├─ IndicativeNotice       Text, IsPlaceholder
├─ IncludesVatCcl         bool
├─ Options[]              (selected options only, recommended first)
│   ├─ SupplierName, TermMonths, PlanTypeLabel?
│   ├─ IsRecommended
│   ├─ Rates[]            Label ("Day", "Night", "Evening & weekend", "Standing charge", "Capacity",
│   │                     "Excess capacity", "Reactive power", "Fixed fee", "Direct Debit discount",
│   │                     "Cheque surcharge"), Value (POWWR string exactly as returned), Unit
│   ├─ AnnualCost         Display string (engine figure, ex or inc VAT/CCL as chosen, to the penny)
│   ├─ Saving?            Direction (Saves | CostsMore), Display string ("£1,234.56 a year")
│   ├─ IsPartial, UncoveredSites[] (site ordinal + address)   (FR-074)
│   └─ PerSite[]          (multi-site only) SiteOrdinal, AnnualCost, Saving?
├─ Totals?                (multi-site only) shown before PerSite (FR-074)
├─ Assumptions            Consumption lines per site ("Day 12,000 kWh", "AQ 40,000 kWh"),
│                         PaymentMethodLabel, VatCclTreatmentLabel, IncludesVatCclLabel,
│                         VatCclNote (wording)
├─ CreditCheck            Text, IsPlaceholder
├─ CommissionDisclosure   Text (placeholders filled, see below), IsPlaceholder
├─ Microbusiness?         Text, IsPlaceholder      (present only when the run's flag is true)
├─ Terms                  Text (wording), Url (admin setting), UrlDisplay (full URL printed)
└─ Agent                  Name, Email, Phone (Graph, else company phone)
```

**Not in the model at all**:
- base rates;
- uplift values;
- commission figures, except inside `CommissionDisclosure.Text`;
- the commission basis or payment model;
- limit or approval status;
- `ExtraInfo`, `FooterMessage` and `FooterNotes`;
- POWWR references, pricebook versions and tariff codes;
- the cross-check figures;
- warnings for the agent;
- internal IDs.

All money values are display strings formatted by `MoneyFormat` from the stored engine figures.
`£1,234.56` uses `Rounding.ToPennies` and the `en-GB` culture. The template does no arithmetic
(Principle VI).

## Commission disclosure placeholders (FR-049)

These are filled in by `CustomerQuoteModelBuilder`, **only inside `CommissionDisclosure.Text`**, using
the recommended option:

| Placeholder | Value |
|---|---|
| `{CommissionTotal}` | Contract-total commission, `£x` |
| `{CommissionPerYear}` | Contract total ÷ (term ÷ 12), `£x` |
| `{CommissionUpfront}` | `£x` |
| `{CommissionMonthly}` | `£x` |
| `{SupplierName}` | Supplier name |
| `{TermMonths}` | Term in months |

An unknown `{Name}` is left as it is and logged, and the admin sees it on the wording preview.

## Layout (A4, `wwwroot/css/pdf.css`, tokens only)

1. **Header**: the colour logo (top left); company contact details; "Your {electricity|gas} quote";
   reference and date.
2. **Customer and sites**: the business, the contact, and each site's address with its full MPAN or
   MPRN.
3. **Validity and indicative notice** in a `.notice`-style box with a teal mark (not coloured text).
4. **Comparison table** of the selected options:
   - The recommended row is highlighted with a teal left border and a teal tint, plus a "Recommended"
     tag, so the highlight never depends on colour alone.
   - Savings show a green or coral dot plus words, with navy text.
5. **Multi-site**: totals table, then a per-site breakdown, then "Sites not covered by {supplier}" lists.
6. **Assumptions**.
7. **Credit check**, **commission disclosure**, and **microbusiness wording** when it applies.
8. **Agent contact.**
9. **Footer** (fixed, so it repeats on every page): our company registration number and the terms
   link. The `<a href>` is clickable, and the full URL is printed as the link text.

Any wording item with `IsPlaceholder = true` renders inside a dashed coral-bordered frame headed
"PLACEHOLDER – NOT APPROVED WORDING" (FR-050).

**Size**: a single site with up to 6 options fits on 1–2 A4 pages (SC-010). Margins are 14 mm, with
18 mm at the bottom for the footer. The body is 9.5–10 pt Poppins and table figures 9 pt with tabular
numbers.

## File name (FR-048)

`{CustomerName}_{Electricity|Gas}Quote_{yyyy-MM-dd}.pdf`. The date is the generation date in UK time.
`CustomerName` has invalid file-name characters and `/\:*?"<>|` replaced by `-`, runs of whitespace
replaced by `_`, and is trimmed to 100 characters. An empty result becomes `Customer`.

## Tests (required by Principle II)

- `CustomerModelShapeTests`: reflection over the model's type tree, as in research R18.
- `CustomerPdfContentTests`: renders the HTML from distinctive agent-only values and checks:
  - no base rate, uplift or commission value appears in any common format;
  - with the disclosure placeholders, only the disclosure paragraph contains the commission;
  - the microbusiness section is present only for microbusiness quotes;
  - placeholder frames appear for placeholder wording;
  - the terms URL is present as both the `href` and the printed text;
  - every money string matches the stored engine figure (SC-004).
- `PdfFileNameTests`.
