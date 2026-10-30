# Quickstart and validation guide: CU Pricing Tool

**Feature**: `001-pricing-tool` | **Plan**: [plan.md](plan.md)

This guide shows how to build, run and check the feature on a developer machine with the **mock POWWR
adapters** and the **Development test sign-in**. Everything here uses made-up data. Commands are for
PowerShell, run from the repository root. Details live in the linked contracts rather than being
repeated here.

## 1. Prerequisites

- The .NET 10 SDK. Check with `dotnet --version`, which should show 10.0.x.
- SQL Server Express on this machine as `.\SQLEXPRESS`, with Windows sign-in.

Nothing else. PowerShell 7 (for Playwright's install script) and the EF Core tools come from the
repository's local tool manifest (research R26).

## 2. One-time setup

```powershell
dotnet tool restore                          # dotnet-ef and pwsh (PowerShell 7) as local .NET tools
dotnet dev-certs https --trust               # local HTTPS for https://localhost:8101
dotnet build
dotnet pwsh PricingTool/bin/Debug/net10.0/playwright.ps1 install chromium   # PDF rendering
dotnet ef database update --project PricingTool                              # creates CuPricingTool on .\SQLEXPRESS
```

The migration seeds these starting values (data-model.md, research R19):
- placeholder wording, visibly marked;
- the example supplier list with lookup aliases;
- default uplifts, limits and thresholds;
- VAT and CCL rates, marked "to confirm".

You don't need any user secrets for this guide. [contracts/configuration.md](contracts/configuration.md)
lists them for SwaggerHub and live use.

## 3. Run

```powershell
dotnet run --project PricingTool
```

Open `https://localhost:8101`. You are sent to the sign-in page. In Development mode it leads to
`/Dev/SignIn`, where you pick a made-up person:

| Person | Role | Use for |
|---|---|---|
| Alex Agent | Agent | Quoting (US1–US4, US6, US7) |
| Morgan Manager | Manager | Approvals (US5), quoting as a manager |
| Riley Manager | Manager | Approving Morgan's requests (US5-6) |
| Ada Admin | Admin | Settings (US8) and Reports (US9) |
| Pat Nobody | No role | The no-access page (FR-001) |

**Where files go** (both folders are git-ignored):
- PDFs: `PricingTool/App_Data/files/pdfs/…`
- Reporting exports: `PricingTool/App_Data/reporting/quotes/yyyy/MM/dd/…json`

## 4. Automated tests

```powershell
dotnet test
```

Expected: all tests pass, with no network, POWWR, browser or database needed (research R27). To run one
area, filter by class name, for example `dotnet test --filter "FullyQualifiedName~UpliftPolicyTests"`.

## 5. Mock data for the scenarios

The postcodes and meter numbers are listed in
[contracts/powwr-meter-lookup-mapping.md](contracts/powwr-meter-lookup-mapping.md#mock-adapter-behaviour-mockmeterlookupclient).
How the mock prices meters is in
[contracts/powwr-uk-pricing-mapping.md](contracts/powwr-uk-pricing-mapping.md#mock-adapter-behaviour-mockpowwrpricingclient).
The ones used below:

| Use | Value |
|---|---|
| Standard electricity site | Postcode `ZZ1 1AA`, MPAN `1600000001013` |
| Microbusiness (90,000 kWh) | `ZZ1 1AB` |
| Low use (30 kWh a day) | `ZZ1 1AC` |
| Lookup warnings | `ZZ1 1AD` |
| Half-hourly | `ZZ1 1AE` |
| MHHS meter with no SSC | `ZZ1 1AF` |
| Gas sites (IGT, switching, large) | `ZZ1 1AG` |
| No addresses | `ZZ9 9ZZ` |
| Lookup unavailable | `ZZ9 9ZY` |
| Every supplier declines | MPAN `2300000000019` |
| POWWR error | MPRN `9999999` |
| Wrong check digit | MPAN `1600000001014` |

## 6. Validation scenarios

Each check names the acceptance scenario or requirement it proves. Run them in order: later stories
reuse quotes made earlier.

### US1: Electricity quote, single site (as Alex Agent)

1. **New quote → Electricity → postcode `zz11aa`.**
   - Expected: the postcode is normalised to `ZZ1 1AA`.
   - Pick Unit 1, then MPAN `…1013`. The meter fields fill in and are marked "from lookup": PC 03,
     MTC 801, LLFC 631, SSC 0393, region North Western, current supplier British Gas, 18,000 kWh
     (US1-1, FR-010).
2. **Change the MPAN to `1600000001014` and click Get prices.**
   - Expected: a field error about the check digit, and **no** pricing run is recorded (US1-3, SC-002).
3. **Postcode `ZZ9 9ZY`.**
   - Expected: a notice saying the lookup is unavailable.
   - Enter the meter by hand and price it successfully (US1-2, SC-005).
4. **Price `ZZ1 1AA`, with terms 12, 24 and 36.** Expected:
   - `.load-steps` while fetching;
   - then a table with every supplier and term, the rates exactly as returned, the annual cost,
     outside-pricing-window flags (Yu Energy 60 months, if 60 is requested) and supplier notes
     (Octopus);
   - Valda Energy listed as declined with its reason (US1-4).
5. **The agent-only columns** show base rates and commission split into upfront and monthly.
   - Sorting by annual cost and by commission works.
   - The term and supplier `.filter-pill`s narrow the table.
   - Repeat with the browser's script turned off: the filters still work as links (US1-5).
6. **Change EDF 24-month uplift to 2.0 and click Re-price.**
   - Expected: new prices appear without re-entering anything.
   - The history shows run 1 and run 2 (US1-6, FR-039).
7. **`ZZ1 1AB`.**
   - Expected: a microbusiness `.tag`, which the agent can't remove (US1-7).
8. **`ZZ1 1AC`, with "include VAT and CCL" on.**
   - Expected: the low-use treatment, 5% VAT and no CCL (US1-8).
9. **`ZZ1 1AD`.** Expected:
   - The domestic meter needs confirmation before pricing.
   - The de-energised and Green Deal meters show amber warnings, which are saved with the quote
     (US1-9).
10. **Select three options and mark one Recommended.**
    - Expected: after reloading, the selection, the priced time and the pricebook version
      (`MOCK-yyyyMMdd`) are shown (US1-10).

### US2: Gas (as Alex Agent)

1. **Gas → `ZZ1 1AG`.**
   - Expected: only gas meters are offered, with MPRN, AQ, LDZ, current supplier and the smart
     suggestion filled in (US2-1).
2. **Pick `2345678`.**
   - Expected: the IGT warning, and it still prices (US2-4).
3. **Pick `3456789`.**
   - Expected: a switching warning naming E.ON Next (FR-012).
4. **As Ada Admin, set the large-gas AQ limit to 500,000. As Alex, pick `7654321`.**
   - Expected: it is flagged for a manual quote and **not** priced (US2-3).
   - With the limit blank, it isn't flagged (FR-045).
5. **Type MPRN `12345`.**
   - Expected: a field error, and no request (US2-2).
6. **The contract section** is prefilled with the **gas** default uplifts (US2-5).

### US3: Customer PDF (as Alex Agent)

1. **From the US1 quote (priced today, one option recommended), click Generate customer PDF.**
   Expected:
   - The PDF downloads as `{Customer}_ElectricityQuote_{today}.pdf` (US3-1).
   - It is A4, on 1–2 pages, with the logo, all the content in FR-047, and the recommended row
     highlighted (SC-010).
2. **Search the PDF text** for the base rates and commission shown on screen.
   - Expected: no matches. The only exception is the disclosure paragraph, if the wording uses
     `{CommissionTotal}` (US3-2, SC-003).
   - The supplier notes are absent (FR-049).
3. **The wording on the PDF:**
   - The microbusiness wording appears for the `ZZ1 1AB` quote and not for `ZZ1 1AA` (US3-3).
   - Placeholder wording sits in visible "PLACEHOLDER" frames (US3-4).
   - The indicative notice is present, and the footer link to the terms address works and is printed
     in full (US3-5).
4. **Compare every money figure** on the PDF with the screen.
   - Expected: identical to the penny (US3-6, SC-004).
5. **Price-change check:** set `Powwr:Mock:ChangedSuppliers` to `["EDF"]` and restart.
   - Generating the PDF says EDF has new prices and asks for a re-price (US3-7).
6. **Expiry:** run `UPDATE PricingRuns SET FetchedUtc = DATEADD(day, -1, FetchedUtc) WHERE Id = <id>`
   on the local database. This is the only way to age a run without waiting a day.
   - Expected: generating the PDF says the prices have expired (US3-8, SC-011).
7. **The validity statement** shows the fetched date and "valid until the end of that day" (US3-9).

### US4: Open in Outlook

With the Development sign-in, the app is in `Eml` mode
([contracts/outlook-draft.md](contracts/outlook-draft.md)).

1. **Click Open in Outlook** on today's PDF.
   - Expected: an `.eml` file downloads, with a toast explaining what to do.
   - It opens in classic Outlook as an unsent draft with only the PDF attached, and no recipient,
     subject or body (US4-1 in fallback mode).
2. **The quote history** shows "Outlook draft created by Alex Agent at …" (US4-4).
3. **Graph mode** (once IT provides the app registration): sign in through Entra with
   `Authentication:Mode = Entra` and `Outlook:Mode = Graph`.
   - Expected: the toast says "Saved to your Outlook Drafts", and Open shows the draft in Outlook on the
     web.
   - The draft also appears in Drafts in classic and new Outlook, and nothing is sent (US4-1, US4-2).
4. **Outlook unreachable:** in Graph mode, disconnect the network and click again.
   - Expected: a clear error, and Download PDF still works (US4-3).

### US5: Uplift limits and approvals

1. **As Ada Admin,** set the Agent electricity unit-rate limit to 1.0 p/kWh, and the British Gas
   maximum to 2.5.
2. **As Alex,** price with a 1.5 uplift. Expected:
   - The affected options say "Needs approval".
   - The PDF and Outlook are unavailable for them (US5-1, SC-009).
3. **Request approval** with a reason.
   - Expected: `.tag.tone-amber` "Awaiting approval".
   - The request is on Morgan Manager's Approvals page (US5-2).
4. **As Morgan, approve it with a comment.**
   - Expected: as Alex, the outcome is shown and the PDF can be generated (US5-3).
5. **Declines:** repeat with a new quote, and decline without a comment. The decline is rejected.
   Decline with a comment.
   - Expected: Alex sees the comment and must lower the uplift (US5-4).
6. **Raising an approved uplift:** raise the approved uplift to 1.6 and re-price.
   - Expected: a new approval is needed (US5-5).
7. **Self-approval:** as Morgan, create a quote that needs approval and request it.
   - Expected: on the Approvals page, Morgan can't approve their own request, but Riley can (US5-6).
8. **Supplier maximum:** set British Gas to 3.0.
   - Expected: "Above supplier maximum (2.5)", with no approval possible (US5-7).

### US6: Multi-site (as Alex Agent)

1. **Download the electricity `.xlsx` template** and check it opens with the expected columns. For the
   upload itself, use `PricingTool.Tests/TestData/sites-50-electricity.csv` (and its `.xlsx` twin). It
   has 50 made-up sites with valid MPANs. Three rows are deliberately bad: a wrong check digit, an MPAN
   repeated within the file, and an MPRN on an electricity quote. The mock prices any valid MPAN, so
   these sites don't need to exist in the mock lookup.
2. **Upload it.**
   - Expected: those three rows are listed with their errors, and nothing is priced (US6-2).
   - Fix one in the browser and remove two, then add the rows (SC-008).
3. **Price the quote.** Expected:
   - Each supplier and term shows a total and a per-site breakdown (US6-3).
   - Valda shows sites with reasons and is marked Partial (US6-4).
4. **Combined consumption:** several small sites adding up to more than 100,000 kWh are **not** a
   microbusiness. Sites adding up to 100,000 kWh or less are (US6-5).
5. **The PDF** shows the totals first, then the per-site breakdown, and lists any uncovered sites
   (US6-6).
6. **Failed sites:** add a site with MPRN `9999999` on a gas multi-site quote.
   - Expected: that site fails, and "Retry failed sites" re-sends only that site.

### US7: Quote history (as Alex Agent)

1. **Quotes:** search by customer name, by reference, by MPAN `1600000001013`, by postcode `ZZ1 1AA`,
   and by date range.
   - Expected: matching quotes only, and only Alex's own (US7-1, FR-003).
2. **As Morgan,** the same search shows all agents' quotes (FR-003).
3. **Reopen a quote** with `Powwr:Mock:ChangedSuppliers` set.
   - Expected: "Prices have changed since this was priced" (US7-2).
4. **Re-price** keeps the earlier runs and PDFs (US7-3).
5. **Re-download** an earlier PDF.
   - Expected: byte-identical to the original, showing its own validity date (US7-4).
6. **Open in Outlook on an aged PDF** (age it with `UPDATE CustomerPdfs SET PricesFetchedOn =
   DATEADD(day, -1, PricesFetchedOn) WHERE Id = <id>`).
   - Expected: the tool requires a re-price and a new PDF (US7-5).
7. **History** lists every request, result, uplift, approval, PDF and draft, with who and when (US7-6).
8. **Retention:** as Ada, set the retention period to 0 years. Restart the app with
   `Retention:RunAtUkTime` set one minute ahead of now.
   - Expected: the clean-up removes quotes, their PDFs and their reporting files, and the log shows
     the counts (US7-7). Set the period back to 6 afterwards.

### US8: Admin settings (as Ada Admin)

1. **Change the default electricity unit-rate uplift.**
   - Expected: a new quote is prefilled with it, and the change log shows who, when, and the old and
     new values (US8-1).
2. **Edit the commission disclosure and generate a new PDF.**
   - Expected: the new PDF uses the new wording, and the earlier PDF is unchanged and records the
     older version (US8-2, FR-083).
3. **Add a CCL rate effective from a future date.**
   - Expected: a quote starting on or after that date uses it, and an earlier start uses the old rate
     (US8-3).
4. **Map an unmapped lookup supplier name to a supplier.**
   - Expected: new quotes recognise it (US8-4).
5. **As Alex and as Morgan, open `/Settings`.**
   - Expected: they are refused, even by typing the URL (US8-5).

### US9: Reporting (as Ada Admin)

1. **Open Reports.**
   - Expected: the events from the scenarios above appear with every FR-084 field (US9-1).
2. **Filter** by agent, date, fuel, supplier, region and quote type.
   - Expected: the totals update. Export to CSV, and the export appears in the change log (US9-2,
     FR-086).
3. **As Alex and as Morgan, open `/Reports`.**
   - Expected: they are refused (US9-3).
4. **Store outage:** make `App_Data/reporting` read-only, then price a quote and generate a PDF.
   - Expected: the agent's speed is unchanged, and the outbox shows pending entries.
   - Restore write access. Within 15 minutes, the files appear under `quotes/yyyy/MM/dd/`, and each
     validates against [contracts/reporting-record.v1.schema.json](contracts/reporting-record.v1.schema.json)
     (US9-4, SC-007).

### Access and security checks

1. **As Pat Nobody** (no role): every page shows No access, with no data (FR-001).
2. **Development sign-in outside Development:** set `ASPNETCORE_ENVIRONMENT=Production` and run.
   - Expected: startup fails if `Authentication:Mode = Development`.
   - `/Dev/SignIn` returns 404 (Principle I; this is also an automated test).
3. **Secrets:** search the repository for `LicenceCode` values, and the database for the licence code
   in `PricingRunSites.RequestJson`.
   - Expected: only `***` (Principle I).
4. **Narrow widths:** resize to 1024, 720 and 390 px wide.
   - Expected: the sidebar becomes a rail and then a slide-out menu, the results table scrolls inside
     its card, and everything works by keyboard with the kit's focus ring (FR-091, FR-092).

## 7. Other POWWR modes (optional)

| Mode | Settings | Needs |
|---|---|---|
| SwaggerHub mock | `Powwr:Pricing:Mode = SwaggerHub` and/or `Powwr:MeterLookup:Mode = SwaggerHub` | Internet access only. Its data is generic, so many scenarios above won't apply |
| Live POWWR | `Powwr:Pricing:Mode = Live` plus the `Powwr:Pricing:LicenceCode` user secret. Meter lookup: `Sandbox` or `Live` plus `ClientId`, the `ClientSecret` user secret and `OrganisationPowwrId` | Credentials from POWWR. **Never** used by tests |

See [contracts/configuration.md](contracts/configuration.md) for every key.
