# Implementation Plan: CU Pricing Tool

**Branch**: `001-pricing-tool` | **Date**: 2026-10-30 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-pricing-tool/spec.md`

**Scope of this plan**: build and run everything **locally**. Deploying to Azure is a separate, later
feature. This plan creates **no Azure resources and no deployment work**, but every local-versus-Azure
difference sits behind configuration or an interface (Principle VII).

## Summary

An internal ASP.NET Core Razor Pages app for Consultiv Utilities' sales agents. An agent:
1. finds a business customer's electricity or gas meter (postcode → address → meter, through POWWR's
   Meter Lookup, or by hand);
2. enters the customer, supply and contract details;
3. gets live prices from POWWR's UK Pricing API with our uplift applied per supplier and term;
4. compares the options, using agent-only base rates and commission;
5. selects the options to offer and recommends one;
6. produces a branded A4 PDF quote;
7. can save it into a blank Outlook draft in their own mailbox.

Uplifts are capped by hard limits that are the same for everyone: a maximum per fuel, and a maximum for
each supplier and fuel. There is no manager approval. Admins manage settings, wording and limits, see
reports, and can view every quote read-only. The roles are Agent and Admin.

**Technical approach**:
- **Structure**: one web project and one xUnit project, following Triage's conventions.
- **Data**: EF Core on SQL Server Express.
- **Sign-in**: Microsoft.Identity.Web, with a test sign-in that works only in Development.
- **POWWR**: two POWWR adapters, each with a realistic mock as the default.
- **Calculations**: one stateless pricing engine whose full-precision `decimal` output is **stored**,
  so the screen, the PDF and reporting always show the same pennies.
- **PDF**: a Razor component rendered to HTML and printed by Playwright's Chromium.
- **Outlook**: a single Graph create-message call, with an `.eml` fallback.
- **Reporting**: a reporting outbox exported in the background to a local folder (Blob later).
- **UI**: the Consultiv Utilities design kit, unchanged, with progressive enhancement only.

The detailed decisions are in [research.md](research.md).

## Technical Context

| | |
|---|---|
| **Language/Version** | C# on .NET 10 (current LTS; SDK 10.0.301 installed) |
| **Primary Dependencies** | ASP.NET Core Razor Pages; Entity Framework Core 10 + SQL Server provider; Microsoft.Identity.Web (OpenID Connect, token acquisition, distributed token cache); Microsoft.Playwright (PDF); DocumentFormat.OpenXml (.xlsx). Built into .NET with no package: `HtmlRenderer` (Razor component → HTML), `TextFieldParser` (CSV), `System.Text.Json`, `TimeProvider`, `BackgroundService` |
| **Storage** | SQL Server Express `.\SQLEXPRESS` (Windows sign-in), database `CuPricingTool`, EF Core migrations that also work on Azure SQL. PDFs: `IFileStore` → local folder `PricingTool/App_Data/files`. Reporting exports: `IReportingExportStore` → `PricingTool/App_Data/reporting/quotes/yyyy/MM/dd/` |
| **Testing** | xUnit 2.9.3 (as in Triage). Pure-logic unit tests with no database, network, browser or POWWR. A few `Microsoft.AspNetCore.Mvc.Testing` checks for sign-in and role security |
| **Target Platform** | Windows developer machine, Kestrel on `https://localhost:8101`. Later Azure App Service, UK South (a separate feature) |
| **Project Type** | Server-rendered web application: one web project + one test project |
| **Performance Goals** | SC-001: postcode → PDF or Outlook draft in under 2 minutes for 9 in 10 single-site quotes. App overhead (excluding POWWR) under 500 ms at p95 per page. PDF render under 5 s. A 50-site quote priced in about 2 minutes at 4 concurrent POWWR requests (SC-008) |
| **Constraints** | `decimal` only for money and energy; customer output built only from a customer-safe model; mocks by default and never live POWWR in tests; no Python, Node.js or npm; design kit files unchanged; WCAG AA; works below 720 px, between 720 and 1100 px, and above 1100 px; reporting never blocks an agent; no Azure resources |
| **Scale/Scope** | Tens of agents. Quotes usually under 50 sites and up to 6 offered options. About 12 screens. 9 user stories, 92 FRs, 11 success criteria |

There are no remaining **NEEDS CLARIFICATION** items. Each unknown is resolved in research.md (R1–R28).
Questions only POWWR, finance, compliance or IT can answer are listed there with the tool's behaviour
in the meantime. None blocks the build.

## Constitution Check

*GATE: must pass before Phase 0 research, and again after Phase 1 design.*

| # | Principle | How this design complies | Pre-research | Post-design |
|---|---|---|---|---|
| I | **Security First** (non-negotiable) | **Secrets**: the licence code, the Meter Lookup client secret and the Entra client secret are only in user secrets, read through `IConfiguration`. Key Vault is added later as a configuration provider. **Tokens**: acquired and cached on the server. **Logs and stored copies**: the stored POWWR request masks `LicenceCode`, and no secret or token is logged, audited or exported (R4, R25). **Access**: every page requires sign-in by fallback policy; the server checks the tenant, the group (`AllowedGroupId`) and an app role, with the Agent and Admin roles and quote ownership enforced through policies on every handler (R2, FR-093). Sessions last at most 8 hours (FR-094). **Dev sign-in**: works only in the Development environment, startup fails otherwise, and `/Dev/SignIn` returns 404 (automated test). **Other protections**: antiforgery on every POST, security headers and CSP (R24). ⚠️ **The constitution names Agent, Manager and Admin roles**; the spec now has Agent and Admin only, so the constitution needs a small amendment (see the gate result) | ✅ | ⚠️ |
| II | **Customer-Safe Output** (non-negotiable) | The PDF is built only from `CustomerQuoteDocumentModel`, which has no agent-only fields ([contracts/customer-pdf.md](contracts/customer-pdf.md)). The Outlook draft carries only the PDF. File names contain only the customer name, fuel and date, and the PDF title only our name and the reference. Commission appears only through disclosure placeholders. Placeholder wording can't reach a customer outside Development. "Customer view" renders the results without agent-only data for screen sharing. Reflection and rendered-content tests are required (R18) | ✅ | ✅ |
| III | **Validate Before Calling Out** | `QuoteInputValidator` and the MPAN, MPRN and postcode validators run on the server before every POWWR call (pricing, `/prompts`, lookup), and again as a guard in the services. Upload rows go through the same rules (R15, R23). POWWR responses are mapped and checked in the adapter (R4, R5) | ✅ | ✅ |
| IV | **On-Brand and Accessible** | The kit is copied into `wwwroot/cu/` unchanged. `app.css` and `pdf.css` use tokens only, with no hex, `rgb()` or named colours (a test scans the `.css` and `.cshtml`/`.razor` files). Text is navy, and teal only for links and active controls. Savings use marks plus words. Components are listed per screen below, and missing ones are listed as kit gaps rather than restyled. Keyboard and focus ring come from the kit. Layouts are checked at 390, 1024 and 1280 px (quickstart). The older brand guide is ignored | ✅ | ✅ |
| V | **Auditable** | Inputs and raw POWWR responses (`PricingRunSite`), engine results, uplifts (requested and applied), microbusiness declaration changes, PDFs and PDF downloads (with wording versions), Outlook drafts, report exports and settings changes (with old and new values) are all stored, with the Entra identity and a UTC time. The audit row is written in the same `SaveChanges` as the action, so failure aborts the action. `AppendOnlyGuard` blocks edits and deletes, and only retention deletes (R19, R22) | ✅ | ✅ |
| VI | **One Set of Numbers** | A single `PricingEngine` with one `Rounding.ToPennies`. Its output is persisted, and the screen, PDF and reporting only format stored figures (R7). `decimal` throughout. Engine tests are required. Both POWWR APIs sit behind adapter interfaces with config-selected mocks as the default. Tests never call live POWWR (R4, R5) | ✅ | ✅ |
| VII | **Local First, Azure-Ready** | Runs with only the .NET SDK and SQL Server Express. The secrets, database, file store, reporting export, logging and redirect URI are all behind configuration or an interface. EF migrations run on SQL Server and Azure SQL. **No Azure resources are created by this feature.** Bicep and CI/CD belong to the deployment feature | ✅ | ✅ |
| VIII | **Keep It Simple** | C# and .NET only. Playwright's Chromium is installed with its own PowerShell script, run through `pwsh` from the repo's local .NET tool manifest (R26). Server-rendered Razor Pages, with `app.js` for small progressive enhancements. Triage's layout and conventions, with the four constitution-mandated differences in R1. Packages beyond the named stack are justified below | ✅ | ✅ |
| IX | **Compliance Wording Is Managed** | Every customer-facing statement comes from versioned admin settings, starting as placeholders that are visibly marked: validity, indicative notice, credit check, commission disclosure, the three microbusiness items, VAT/CCL note, terms text, and the terms URL. Placeholders block PDFs and Outlook drafts outside Development, and approval records the compliance approver's name. Each PDF records the wording versions it used. Changes are audited by an Admin. Company contact details are configuration, not code (R19) | ✅ | ✅ |

**Gate result**: PASS before research. After design, PASS **once the constitution is amended**.

- **Why it needs amending**: the security and compliance review (spec Clarifications, 2026-10-30)
  removed the Manager role and manager approval. Principle I and the Technology section still name
  "Agent, Manager and Admin" roles. Principle I can't be waived, so this isn't recorded as a
  Complexity Tracking exception.
- **The fix**: an amendment through `/speckit-constitution` that names the Agent and Admin roles. The
  amendment changes no security rule: every role is still enforced on the server.
- **Triage differences**: those in R1 (Razor Pages, Microsoft.Identity.Web, user secrets, SQL Server)
  are what the constitution's own Technology section requires, so they are not deviations.

## Project Structure

### Documentation (this feature)

```text
specs/001-pricing-tool/
├── spec.md                 # what and why (from /speckit-specify and /speckit-clarify)
├── plan.md                 # this file
├── research.md             # Phase 0: decisions R1–R28 and open items
├── data-model.md           # Phase 1: entities, fields, rules, state transitions
├── quickstart.md           # Phase 1: setup, run and validation scenarios per user story
├── contracts/              # Phase 1
│   ├── ui-routes.md                     # pages, handlers, fragments, access rules
│   ├── adapters.md                      # IPowwrPricingClient, IMeterLookupClient, IFileStore, …
│   ├── powwr-uk-pricing-mapping.md      # request and response mapping + mock behaviour
│   ├── powwr-meter-lookup-mapping.md    # lookup mapping, OAuth, RFC 7807, mock data
│   ├── outlook-draft.md                 # Graph create-message, .eml fallback
│   ├── customer-pdf.md                  # customer-safe model allow-list, layout, file name
│   ├── spreadsheet-template.md          # multi-site CSV and .xlsx columns and rules
│   ├── reporting-record.md              # reporting record v1, export path, CSV columns
│   ├── reporting-record.v1.schema.json  # JSON Schema for the record
│   └── configuration.md                 # config keys, modes, user secrets
├── checklists/requirements.md
└── tasks.md                # Phase 2 (/speckit-tasks; not created here)
```

### Source code (repository root)

```text
PricingTool.slnx
NuGet.config                         # nuget.org only (as in Triage)
.config/dotnet-tools.json            # dotnet-ef, PowerShell (pwsh): local .NET tools (R26)
README.md                            # prerequisites, user secrets, database, run modes, template, record format
THIRD-PARTY-NOTICES.md               # Lucide (ISC), Poppins (OFL)

PricingTool/
├── PricingTool.csproj               # net10.0, Nullable, ImplicitUsings, UserSecretsId, InternalsVisibleTo PricingTool.Tests
├── Program.cs                       # composition: auth mode, policies, EF, adapters by mode, workers, headers
├── Properties/launchSettings.json   # https://localhost:8101, http://localhost:8100, Development
├── appsettings.json / appsettings.Development.json
├── Auth/                            # RoleNames, Policies, GroupAndRoleRequirement, DevAuthentication (Development only), StaffMemberSync
├── Data/
│   ├── PricingDbContext.cs
│   ├── Entities/                    # Quote, Site, Meter, PricingRun, PricingRunSite, PriceOption, PriceOptionSite, …
│   ├── Configurations/              # IEntityTypeConfiguration per entity (precision, owned types, indexes)
│   ├── AppendOnlyGuard.cs           # SaveChanges interceptor (R19)
│   ├── Seed/                        # starting settings, placeholder wording, suppliers (made-up aliases)
│   └── Migrations/
├── Domain/                          # enums and value types: Fuel, QuoteType, MpanCore, MpanTopLine, Mprn, NormalisedPostcode, …
├── Powwr/
│   ├── Pricing/                     # IPowwrPricingClient, PowwrPricingHttpClient, MockPowwrPricingClient, Dtos/, PowwrPricingMapper, PowwrRetryHandler
│   └── MeterLookup/                 # IMeterLookupClient, MeterLookupHttpClient, MockMeterLookupClient, Dtos/, MeterLookupMapper, MeterLookupTokenProvider
├── Services/
│   ├── Validation/                  # QuoteInputValidator, Postcode, CompanyNumber, UkPhone, RegisterSplit, LargeGasSiteTest
│   ├── Pricing/                     # PricingEngine, VatCclCalculator, CommissionCalculator, SavingCalculator, MicrobusinessTest,
│   │                                # LowUseTest, MultiSiteCombiner, AnnualCostCrossCheck, Rounding, MoneyFormat
│   ├── Quotes/                      # QuoteService, PricingService (orchestrates the adapter → engine → store), PriceValidity,
│   │                                # PriceChangeCheck, PdfEligibility, QuoteAccess, QuoteReference
│   ├── Limits/                      # LimitPolicy (per-fuel and supplier maximums, capping, re-check)
│   ├── Pdf/                         # CustomerQuoteModelBuilder, CustomerQuoteDocument.razor, PlaywrightCustomerPdfRenderer, PdfFileName
│   ├── Outlook/                     # IOutlookDraftService, GraphOutlookDraftService, EmlOutlookDraftService, EmlWriter
│   ├── Staff/                       # IStaffDirectory, GraphStaffDirectory, DevStaffDirectory
│   ├── Uploads/                     # CsvSiteReader, XlsxSiteReader, SiteUploadValidator, TemplateWriter
│   ├── Settings/                    # SettingsService (versioned values, effective rates), SupplierMatcher, WordingService
│   ├── Audit/                       # AuditWriter
│   ├── Reporting/                   # ReportingRecorder, ReportingRecordBuilder, ReportingOutboxWorker, ReportingExportPath,
│   │                                # IReportingExportStore, LocalFolderReportingExportStore, ReportsQuery, CsvWriter
│   ├── Storage/                     # IFileStore, LocalFileStore
│   ├── Retention/                   # RetentionWorker
│   └── Time/                        # UkCalendar
├── Pages/
│   ├── Shared/                      # _Layout (app shell), _StandaloneLayout, _Head, _Sidebar, _Notice, _Icon partials
│   ├── Index.cshtml                 # sends people to the right page for their role
│   ├── Account/                     # SignIn, Loading, NoAccess, SignOut
│   ├── Dev/                         # SignIn (Development only)
│   ├── Quotes/                      # New, Index (history), Details (results), Edit, Upload, _AddressList, _MeterList,
│   │                                # _MeterFields, _ConsumptionFields, _Results partials
│   ├── Reports/Index.cshtml
│   ├── Settings/                    # Index (sections + drawers), ChangeLog
│   └── Error.cshtml
├── TagHelpers/IconTagHelper.cs      # Lucide inline SVG (R24)
└── wwwroot/
    ├── cu/                          # design kit: css/, js/, fonts/, logo/ — copied UNCHANGED (never edited)
    ├── css/app.css                  # app layout glue + listed kit gaps, tokens only, loaded after the kit
    ├── css/pdf.css                  # PDF print styles (@page A4, footer), tokens only
    └── js/app.js                    # progressive enhancement: picker, result filters, load steps, toasts, antiforgery

PricingTool.Tests/
├── PricingTool.Tests.csproj         # xUnit 2.9.3, Microsoft.NET.Test.Sdk, xunit.runner.visualstudio, coverlet.collector, Mvc.Testing
├── Validation/  Pricing/  Powwr/  Limits/  Uploads/  CustomerSafe/  Reporting/  Time/  Security/  Design/
├── Support/FakeTimeProvider.cs, TestData builders (made-up data only)
└── TestData/                        # sites-50-electricity.csv/.xlsx and bad-row samples (made-up MPANs with valid check digits)
```

**Structure decision**: a single web project and a single test project at the root, in an `.slnx`
solution, mirroring Triage (`Triage/`, `Triage.Tests/`, `Triage.slnx`). Areas are folders, not projects
(R1). EF migrations are in the web project, so the commands are:

```powershell
dotnet build
dotnet test
dotnet run --project PricingTool
dotnet ef database update --project PricingTool
```

The Setup phase writes these into the "Commands" section of `CLAUDE.md`.

**Repository hygiene** (Setup phase): add `PricingTool/App_Data/` (local PDFs and reporting exports),
`.playwright-mcp/` and `*.eml` to `.gitignore`. `bin/`, `obj/`, `.vs/` and
`.claude/settings.local.json` are already covered.

## Design overview

### Pricing flow (single and multi-site)

```text
Form POST ─▶ QuoteInputValidator (server) ──errors──▶ redisplay with .form-error (no POWWR call)
               │ ok
               ▼
          save Quote + Sites (Draft) + AuditEntry + ReportingRecord(QuoteRequested)   ← one transaction
               │
               ▼
          PricingService: per site (≤ MaxConcurrentRequests in parallel)
               ├─ large gas site? → NotPriced (FR-045)
               └─ IPowwrPricingClient (Mock | SwaggerHub | Live) → SitePriceResponse (mapped, checked)
               │
               ▼
          PricingEngine (settings snapshot: rates by start date, thresholds, basis, payment models)
               → per-site costs, VAT, CCL, savings, commission → options + totals + partial flags
               │
               ▼
          save PricingRun + PricingRunSites + PriceOptions + NoQuotes + AuditEntry + ReportingRecord   ← one transaction
               │
               ▼
          redirect → /Quotes/{id} (results read the stored figures)

Uplift limits (R13): the per-fuel maximum is checked by QuoteInputValidator before anything is saved
or sent; supplier maximums cap each plan's uplift when the request is built (capped options flagged).
```

### Generating the PDF and opening it in Outlook

```text
GeneratePdf ─▶ PdfEligibility (fetched today? no price changes? within current limits? no placeholder
               wording outside Development? recommended? no failed sites?)
           ─▶ CustomerQuoteModelBuilder (stored figures + current wording versions → customer-safe model)
           ─▶ ICustomerPdfRenderer (HtmlRenderer → Playwright Chromium, assets via route handler)
           ─▶ IFileStore.Save + CustomerPdf + AuditEntry + ReportingRecord(PdfGenerated)
OpenInOutlook ─▶ PricesFetchedOn == today? ─▶ IOutlookDraftService (Graph | Eml)
              ─▶ OutlookDraft + AuditEntry + ReportingRecord(OpenedInOutlook) ─▶ toast "Saved to your Outlook Drafts" [Open]
```

### Screens and kit components

| Screen | Kit components |
|---|---|
| App shell | `.app-shell` > `.sidebar` (navy, **white** logo `cu-logo-150-white.png` with a 250 `srcset`, `cu-mark-white.png` for the rail) + `.app-main` > `.topbar` + `.content`. Nav: New quote (`CanQuote`, Agent), Quotes (all; Admins see every quote read-only), Reports and Settings (`CanAdmin`) |
| Sign-in, loading, no access, dev sign-in | `.standalone`, `.standalone-card`, `.standalone-logo` (colour logo), `.standalone-icon`, `.standalone-actions`, `.btn-primary` |
| New and Edit quote | Postcode → address → meter picker at the top, using `.field-inline` + `.btn-primary`, a `.search-results` list of `.search-result` buttons, and "Enter details yourself" (`.btn-link`). One `.card` per section with a `.section-heading`. `.field` / `.field-label` / `.form-input` / `.form-select` / `.field-hint` / `.form-error`. `.seg` for fuel and quote type, `.check-chip` for terms, `.switch-row` for "no standing charge" and "include VAT and CCL", `.tag` for microbusiness, `.notice.tone-amber` for meter warnings, `.form-actions` with `.btn-cta` "Get prices" |
| While pricing | `.load-steps` (checking details → asking suppliers → working out costs), with `.skeleton` rows for the results |
| Results (quote details) | A `.table` inside a `.card--flush` with `.table-scroll`, `.num` figures, `.filter-pill` filters, `.tag.tone-green` "Recommended", `.tag.tone-amber` "Capped at supplier maximum (x)" (agent-only). A `.switch-row` "Customer view" re-renders the results without agent-only data (FR-099). Savings use `.delta--good`/`.delta--bad` marks plus words, never coloured text. `.btn-cta` with `.btn-count` for "Generate customer PDF (n)". `CU.toast()` with an "Open in Outlook" action. `.notice.tone-coral` for errors, `.notice.tone-amber` for warnings, `.empty-state` when nothing is priced. The uplift dialog uses `.modal-overlay` > `.modal` through `data-open` |
| Quote history | `.table` in a `.card--flush`, a search `.field-inline`, `.pagination`. The history timeline uses `.timeline-entry` / `.timeline-event` |
| Reports | Filter `.select-trigger[data-menu]` + `.menu` (not inside swapped HTML), `.kpi-strip` > `.kpi-mini` totals, `.table`, `.btn-outline` "Export CSV", and a `.notice.tone-amber` when exports have been failing for over an hour |
| Settings | `.card` sections, each edited in a `.drawer-wrap` > `.drawer` (`data-open`), and a change log `.table` |
| Icons | Lucide inline SVG through `<icon name="…">`: 24×24 viewBox, `currentColor`, 2px stroke, usually 16 px |

**HTML swapped in after page load** (research R24): `kit.js` binds menus, tabs and split views to each
element once at page load, and has no re-initialise function. Dialogs and drawers are delegated and keep
working. So fragments returned by page handlers must not contain `data-menu`, `data-tab` or `.split`.
Menus, tabs and splits stay in parts of the page that are never swapped. Everything in a fragment
either uses delegated behaviour (`data-open`/`data-close`, `app.js` handlers) or is a plain link or
form.

### Components the kit lacks (to add to the kit)

The kit in `wwwroot/cu/` is never edited. Until the kit gains these, minimal versions go in
`app.css`/`pdf.css` using tokens only, each marked `/* KIT GAP n: proposed for the kit */` so they can
be deleted when the kit catches up.

1. **A re-initialise hook**: `CU.init(root)`, or delegated listeners for `[data-menu]`, `[data-tab]` and
   `.split`, so swapped-in HTML can use menus, tabs and split views.
2. **Sortable table header**: a `th` button with an `aria-sort` indicator (ascending, descending, none).
3. **Table row selection**: a checkbox column style for selecting rows in a `.table`, with a "selected"
   row state distinct from `.is-active`.
4. **Highlighted (recommended) table row**: a row variant with a left edge plus a tint, for the
   recommended option on screen and in the PDF.
5. **File picker / drop zone**: a styled `<input type="file">` with the chosen file name, for the
   spreadsheet upload. The existing `.file-chip` is for attachments, not picking.
6. **Print/A4 document styles**: `@page` size and margins, a repeating page footer, page-break rules for
   tables, and print-safe type sizes. These are needed for the customer PDF (`pdf.css`).
7. **Placeholder frame**: a visibly marked "not approved wording" block for compliance placeholders,
   on the PDF and in the Settings preview.
8. **Row-level error table**: a compact table pattern for upload errors (row, column, value, message)
   with an error mark, for per-row validation.

## Packages

| Package | Why | Stack status |
|---|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design` | Data access and migrations | Named in the constitution |
| `Microsoft.Identity.Web` | Entra sign-in, token acquisition for Graph, distributed token cache | Named |
| `Microsoft.Playwright` | Headless Chromium PDF rendering | Named |
| `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `coverlet.collector` | Tests, as in Triage | Named (xUnit) |
| **`DocumentFormat.OpenXml`** | Reading uploaded `.xlsx` files and writing the `.xlsx` template (FR-069). Microsoft-maintained and dependency-light. ClosedXML would add more transitive packages (R23) | **Justified addition** |
| **`Microsoft.AspNetCore.Mvc.Testing`** (tests only) | Proves that roles are enforced on the server and that the dev sign-in can't be reached outside Development (Principle I) | **Justified addition** |
| Local tools: `dotnet-ef`, `PowerShell` | Migrations; running Playwright's own install script on a machine with only the .NET SDK (R26) | Tooling, not runtime |

Deliberately **not** added:
- the Microsoft.Graph SDK (one call is made with `HttpClient`);
- FluentValidation;
- CsvHelper (`TextFieldParser` is built in);
- MimeKit (the `.eml` is about 40 lines);
- Polly and Http.Resilience (a small `PowwrRetryHandler`);
- Microsoft.Identity.Web.UI (a few lines of sign-in and sign-out);
- `Microsoft.Extensions.TimeProvider.Testing` (a small fake).

Package versions are pinned in the csproj to the latest stable versions that support `net10.0` when
the Setup phase runs.

## Guidance for `/speckit-tasks`

| Phase | Contents |
|---|---|
| **Setup** | Solution, projects and tool manifest; copy the kit unchanged into `wwwroot/cu/`; `.gitignore`; `NuGet.config`; `launchSettings.json`; the `CLAUDE.md` Commands section; README skeleton; Playwright install documented |
| **Foundational** (blocks every story) | Sign-in (Entra + Development mode), roles and policies, NoAccess and the standalone pages; app shell (`_Layout`, `_Head`, sidebar by role, `app.css`, `app.js` base, icons); `PricingDbContext`, the core entities, the first migration and seed data; `AuditWriter` + `AppendOnlyGuard`; `SettingsService` (versioned values, effective rates); validation (MPAN, MPRN, postcode and the rest) with tests; both POWWR adapter interfaces + mocks + mapping with tests; **the pricing engine with its full tests**; `ReportingRecorder` + outbox table (the worker can follow in US9); `TimeProvider` and `UkCalendar` |
| **US1** (P1) | Electricity form, picker, rate structure, pricing service, results, filters, uplift dialog, re-price, selection and warnings |
| **US2** | Gas |
| **US3** | PDF |
| **US4** | Outlook |
| **US5** | Uplift limits: per-fuel maximums, supplier maximums per fuel, capping, re-check before PDF and Outlook |
| **US6** | Multi-site and upload |
| **US7** | History, price-change check and retention |
| **US8** | Settings pages and change log |
| **US9** | Reports, CSV and the outbox worker |
| **Polish** | Accessibility pass at 390, 1024 and 1280 px; colour-token scan test; README completion; quickstart run-through |

**Every phase includes its unit tests.** No Azure tasks.

## Risks and follow-ups

- **Constitution amendment**: Principle I and the Technology section still name a Manager role. Amend
  them with `/speckit-constitution` before `/speckit-tasks`, or `/speckit-analyze` will report the
  mismatch as CRITICAL. `CLAUDE.md` ("Roles: Agent, Manager, Admin") follows in the same change.
- **Blank limits allow no uplift** (FR-060): the seed data must set every per-fuel and supplier
  maximum. A supplier added later with no maximum is priced at zero uplift until an admin sets one.
- **POWWR answers** (research open items). The biggest risks are:
  - the commission basis (Q3);
  - the start date field (Q9);
  - the 9-character top line layout (Q10);
  - whether `Version` matches `PricebookVersion` (new question P-17);
  - electricity switching detection (new question P-16, **leaves FR-012 partly unmet for
    electricity until answered**).

  Each one is isolated in a single class or setting, so an answer is a small change. P-16 and P-17
  have been added to `docs/powwr/README.md` as questions 16 and 17.
- **Eml fallback**: meets FR-054 and FR-055 only partly. It is used only if IT refuses `Mail.ReadWrite`,
  and would need a spec amendment before being used in a deployed environment (research R3).
- **Rounding**: on multi-site PDFs, per-site rounding can differ from the total by up to 1p per site
  (research R7). This follows FR-035. Finance may want a wording note.
- **Long multi-site pricing requests**: a 50-site quote is priced in one request, which takes about
  1–2 minutes at 4 concurrent calls. That is acceptable locally and on App Service, which has a limit
  of about 230 s. If POWWR's real latency or rate limits are worse, pricing moves to a background job
  with progress, as a follow-up feature.
- **Agent phone on the PDF**: uses Graph `/me` with `User.Read` (research, open items). IT should
  confirm.

## Complexity Tracking

No waivable deviations. The Manager-role mismatch is fixed by amending the constitution (see the gate
result), not by recording an exception here.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
