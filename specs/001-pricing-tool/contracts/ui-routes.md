# Contract: pages, handlers and access

This is the app's own interface: every Razor Page, page handler and file endpoint, who can reach it,
and what it returns. Roles are enforced on the server through authorisation policies (research R2).
Hiding navigation is never the only check.

**Policies**:

| Policy | Who |
|---|---|
| `CanQuote` | Agent or Manager |
| `CanViewAllQuotes` | Manager or Admin |
| `CanApprove` | Manager |
| `CanAdmin` | Admin |
| Fallback | Signed in, in the "Pricing Tool Users" group (Entra mode), holding any app role |

People without access are sent to `/Account/NoAccess`.

**Quote access**: an owner can read and change their quote. A `CanViewAllQuotes` user can read any
quote. Anyone else gets **404**, so the app doesn't reveal that the quote exists.

**Swapped HTML**: a handler marked *fragment* returns a partial for `app.js` to swap in. Fragments must
not contain `data-menu`, `data-tab` or `.split` (research R24). Every fragment handler also works as a
normal form post that re-renders the whole page.

**Antiforgery**: every POST handler requires the antiforgery token. `app.js` sends it as the
`RequestVerificationToken` header on `fetch`.

## Account and standalone pages (`_StandaloneLayout`, `.standalone`)

| Route | Access | Purpose |
|---|---|---|
| `GET /Account/SignIn` | Anonymous | Logo, "Sign in with Microsoft" button. Goes to `/Account/Loading` |
| `GET /Account/Loading` | Anonymous | "Taking you to Microsoft sign-in…", then the OpenID Connect challenge (a button if script is off). In Development mode, it goes to `/Dev/SignIn` instead |
| `POST /Account/SignOut` | Signed in | Signs out of the cookie and, in Entra mode, out of Entra |
| `GET /Account/NoAccess` | Anonymous | Explains that access needs the "Pricing Tool Users" group and a role. Shows no data |
| `GET /Dev/SignIn`, `POST /Dev/SignIn` | Anonymous, **Development environment and `Authentication:Mode=Development` only; otherwise 404** | Pick a made-up person: Alex Agent (Agent), Morgan Manager (Manager), Riley Manager (Manager), Ada Admin (Admin), Pat Nobody (no role) |

## App pages (`_Layout`, the kit's app shell)

### `/` (Index)

Sends `CanQuote` users to `/Quotes/New`, and users with only `CanAdmin` to `/Reports`.

### `/Quotes/New`: New quote (`CanQuote`)

| Handler | Kind | In | Out |
|---|---|---|---|
| `GET` | page | `?fuel=Electricity\|Gas` | The empty form, with the fuel's default uplifts (FR-025, FR-044) |
| `POST ?handler=Addresses` | fragment | `postcode` | Postcode validated and normalised on the server → `IMeterLookupClient.SearchAddressesAsync` → address list (`.search-results` of buttons). If there are no results or the lookup is down: a `.notice` plus "Enter details yourself" (SC-005) |
| `POST ?handler=Meters` | fragment | `meterAddressId` | `SearchMetersAsync` → meters for the quote's fuel only (spec edge case) |
| `POST ?handler=MeterByNumber` | fragment | `mpan` or `mprn` | Validated, then `SearchMetersAsync` by number (FR-009) |
| `POST ?handler=PickMeter` | fragment | Meter identifier | The meter section, prefilled and marked "from lookup", with warnings (FR-010, FR-012) |
| `POST ?handler=RateStructure` | fragment | Electricity meter fields | Validated, then `/prompts` → consumption fields for the returned registers (FR-020); for an MHHS meter with no SSC, the forced-structure choice (FR-021) |
| `POST` (default) | page | The whole form | Validate everything (FR-032), then: errors → redisplay with `.form-error`s (no POWWR call); valid → save the quote as Draft, price (`PricingService`), and redirect to `/Quotes/{id}` (Post/Redirect/Get). If POWWR fails, the inputs are kept and the quote page shows the failure with Retry (FR-042) |
| `POST ?handler=SaveDraft` | page | The whole form | Saves without pricing, after field-level validation; redirects to `/Quotes/{id}/Edit` |

### `/Quotes`: Quote history (all roles)

| Handler | In | Out |
|---|---|---|
| `GET` | `q` (customer name or reference), `meter` (MPAN or MPRN, normalised), `postcode`, `from`, `to`, `page` | `.table` in a `.card--flush`. Own quotes only, unless `CanViewAllQuotes` (FR-003, FR-075) |

### `/Quotes/{id}`: Quote and results (read: owner or `CanViewAllQuotes`; change: owner only)

| Handler | Kind | In | Out |
|---|---|---|---|
| `GET` | page | `sort=cost\|commission`, `term`, `supplier`, `green` | Summary, flags (microbusiness `.tag`, warnings `.notice.tone-amber`), results, approvals, PDFs, history. Runs the price-change check for information (FR-077) |
| `GET ?handler=Results` | fragment | Same filters | The results card: `.table` in a `.card--flush`, `.num` figures, `.filter-pill`s, `.tag.tone-green` "Recommended", `.tag.tone-amber` "Awaiting approval", agent-only columns, the no-quote list. When nothing is priced: `.empty-state` with the reasons |
| `POST ?handler=Select` | fragment | `optionIds[]`, `recommendedId` | Saves the selection (FR-040). With one selected option, it becomes Recommended |
| `POST ?handler=Uplift` | fragment | `supplierId`, `term`, `unitRate`, `standingCharge` | Validated (≥ 0), saved as a QuoteUpliftOverride; returns the results card with "Re-price to apply" |
| `POST ?handler=Reprice` | page | none | New pricing run with the same inputs and current uplifts (FR-039); earlier runs kept |
| `POST ?handler=RetryFailed` | page | none | Re-sends only the failed sites into the same run (spec edge case) |
| `POST ?handler=RequestApproval` | page | `reason` | Creates an ApprovalRequest for the selected options that need approval (FR-061) |
| `POST ?handler=GeneratePdf` | page | none | Runs the eligibility checks (prices fetched today, no price changes, no option needing approval or above a supplier's maximum, a recommended option, no failed sites; FR-052, FR-053), then renders and stores the PDF and redirects. On failure, a `.notice.tone-coral` says exactly which check failed |
| `POST ?handler=OpenInOutlook` | JSON (or a file in Eml mode) | `pdfId` | Checks the PDF's prices were fetched today (FR-076). **Graph**: `{ "ok": true, "webLink": "…", "message": "Saved to your Outlook Drafts" }`, or `{ "ok": false, "message": "…" }`. **Eml**: an `.eml` file download. Records an OutlookDraft (FR-057) |
| `POST ?handler=ConfirmDomestic` | fragment | `siteId` | Records the agent's confirmation for a domestic meter (FR-012) |

**No-script fallback for Outlook**: the form post redirects back with a success `.notice` and an "Open
draft" link, or an error notice.

### `/Quotes/{id}/Edit`: Edit inputs (owner, `CanQuote`)

The same sections and handlers as `/Quotes/New`. Saving marks the quote Draft, so the agent re-prices.

### `/Quotes/{id}/Sites/Upload`: Multi-site upload (owner, `CanQuote`)

| Handler | In | Out |
|---|---|---|
| `GET` | none | Upload form (`.csv` or `.xlsx`, at most 2 MB) and links to both templates |
| `POST` | `file` | Every row validated (FR-070). If all are valid: rows added as sites, then redirect to the quote. Otherwise: a per-row error table, rows kept as a PendingUpload |
| `POST ?handler=FixRow` | `rowNumber` and the fields | Re-validates one row |
| `POST ?handler=RemoveRow` | `rowNumber` | Removes the row from the pending upload |
| `POST ?handler=AddValidRows` | none | Allowed only when no row has errors |

### Files (no layout)

| Route | Access | Returns |
|---|---|---|
| `GET /Quotes/{id}/Pdfs/{pdfId}` | Quote readers | The stored PDF, unchanged (US7-4). `Content-Disposition: attachment; filename="<FileName>"`. Audited as `PdfDownloaded` |
| `GET /Templates/MultiSite?fuel=Electricity\|Gas&format=csv\|xlsx` | `CanQuote` | The template (contracts/spreadsheet-template.md) |
| `GET /Reports?handler=Csv&…filters` | `CanAdmin` | A CSV of the filtered records (FR-085). Audited as `ReportExported` with the filters (FR-086) |

### `/Approvals` (`CanApprove`)

| Handler | Kind | Out |
|---|---|---|
| `GET` | page | A `.split`: pending requests in the list (newest first), and the selected request's detail (quote summary, items with the uplifts asked for against the limits, reason) with Approve and Decline. The `.split` is rendered on the server and never swapped. The manager's own requests show as "Needs another manager" with the buttons disabled |
| `POST ?handler=Approve` | page | `requestId`, `comment?`, `rowVersion`. Rejects the manager's own request (FR-063) and stale versions |
| `POST ?handler=Decline` | page | `requestId`, `comment` (required, FR-062), `rowVersion` |

### `/Reports` (`CanAdmin`)

| Handler | Out |
|---|---|
| `GET` | Filters (agent, from and to dates, fuel, supplier, region, quote type), a `.kpi-strip` of totals (quotes per agent, PDFs, opened in Outlook, estimated commission), and a `.table` of records with paging (FR-085) |

### `/Settings` (`CanAdmin`)

These are page sections, each edited in a `.drawer` (`data-open`, delegated) and saved by a POST
handler that writes an AuditEntry or SettingVersion.

| Handler | Edits |
|---|---|
| `POST ?handler=DefaultUplifts` | Default uplifts per fuel |
| `POST ?handler=Limit` / `?handler=DeleteLimit` | Role and individual limits (FR-058) |
| `POST ?handler=Supplier` | Pricing name, active flag, fuels, `UpfrontPercent`, maximum uplifts (FR-059) |
| `POST ?handler=SupplierAlias` / `?handler=DeleteSupplierAlias` | Lookup mappings (US8-4) |
| `POST ?handler=CommissionBasis` | FR-037 |
| `POST ?handler=DefaultPayment` | FR-081 |
| `POST ?handler=Rate` / `?handler=DeleteFutureRate` | VAT, CCL and low-use rates with an effective-from date (FR-031) |
| `POST ?handler=Thresholds` | Microbusiness thresholds and large gas limits |
| `POST ?handler=Retention` | Retention years |
| `POST ?handler=Wording` | One wording key: the text, plus "approved by compliance" (which clears the placeholder flag) |
| `POST ?handler=TermsUrl` | An absolute https URL |

`GET /Settings/ChangeLog` shows the setting and admin audit entries with old and new values (FR-082).

## Background work (no routes)

- **`ReportingOutboxWorker`**: exports pending reporting records (research R21).
- **`RetentionWorker`**: daily clean-up (research R22).

## Errors

Unhandled exceptions go to `/Error`. In production it shows a reference ID only. The full detail goes
to the log, and the reference ID is the request's trace ID.
