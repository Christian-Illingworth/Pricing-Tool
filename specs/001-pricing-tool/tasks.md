---

description: "Task list for the CU Pricing Tool (001-pricing-tool)"
---

# Tasks: CU Pricing Tool

**Input**: Design documents from `/specs/001-pricing-tool/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: Included. They were requested, and the constitution requires xUnit tests for input
validation (MPAN, MPRN and other fields), the pricing engine, POWWR request and response mapping,
uplift limits, spreadsheet uploads, customer-safe output and reporting records. Approvals were removed
from the spec in the 2026-10-30 security and compliance review, so their tests are replaced by the
uplift-limit tests (hard limits, supplier capping, re-check before PDF and Outlook). Tests are pure
unit tests: no live POWWR, no network, no database, no browser, and made-up data only. Write each
story's tests first and confirm they fail before implementing.

**Organisation**: tasks are grouped by user story, so each story can be built and tested on its own.
**Scope**: build and run locally only. **No Azure resources and no deployment tasks** (constitution
Principle VII; Azure is a later feature).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (a different file, with no dependency on an unfinished task)
- **[Story]**: the user story the task belongs to (US1–US9)
- Every task names the exact files it creates or changes

## Path conventions

These follow plan.md, Triage-style: one web project and one test project at the repository root.

| Path | Contents |
|---|---|
| `PricingTool/` | Web app: `Program.cs`, `Auth/`, `Data/`, `Domain/`, `Options/`, `Powwr/`, `Services/`, `Pages/`, `TagHelpers/`, `wwwroot/` |
| `PricingTool.Tests/` | xUnit tests, in folders by area (`Validation/`, `Pricing/`, `Powwr/`, `Limits/`, `Uploads/`, `CustomerSafe/`, `Reporting/`, `Time/`, `Security/`, `Design/`) |
| `PricingTool/wwwroot/cu/` | The design kit, copied **unchanged**. Never edit these files |

**Rules for every task**:
- **Colours**: use only the kit's tokens (`var(--…)`) in CSS, Razor and the PDF template. Never hex,
  `rgb()`, `hsl()` or named colours.
- **Secrets**: read only through `IConfiguration`, and never logged, audited or exported.
- **Calculations**: every money and energy calculation goes through the pricing engine, using
  `decimal`.
- **Audit**: every quote action writes an `AuditEntry` in the same `SaveChanges` as the action.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: create the solution, packages, configuration and design kit, so the empty app builds
and runs.

- [ ] T001 Create the solution as in plan.md "Project Structure":
  - `PricingTool.slnx`.
  - `PricingTool/PricingTool.csproj`:
    - `Microsoft.NET.Sdk.Web`, `net10.0`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`;
    - `RootNamespace` `PricingTool`, a `UserSecretsId`;
    - `<InternalsVisibleTo Include="PricingTool.Tests" />`.
  - `PricingTool.Tests/PricingTool.Tests.csproj`:
    - `Microsoft.NET.Sdk`, `net10.0`, `IsPackable=false`;
    - packages `xunit` 2.9.3, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `coverlet.collector`, `Microsoft.AspNetCore.Mvc.Testing`;
    - `<Using Include="Xunit" />`;
    - a project reference to `PricingTool`.
- [ ] T002 Add the runtime packages to `PricingTool/PricingTool.csproj`, each pinned to the latest stable version that supports `net10.0`: `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.Identity.Web`, `Microsoft.Playwright` and `DocumentFormat.OpenXml`. Add no other packages (plan.md "Packages"). There's no Microsoft.Graph SDK, FluentValidation, CsvHelper, MimeKit, Polly or Identity.Web.UI.
- [ ] T003 [P] Create `NuGet.config` (nuget.org only, as in Triage) and `.config/dotnet-tools.json` with the local tools `dotnet-ef` and `PowerShell` (research R26), so `dotnet tool restore` provides `dotnet ef` and `dotnet pwsh`.
- [ ] T004 [P] Add `PricingTool/App_Data/` (local PDFs and reporting exports), `.playwright-mcp/` and `*.eml` to `.gitignore`. Check that `bin/`, `obj/`, `.vs/` and `.claude/settings.local.json` are still ignored.
- [ ] T005 [P] Copy the `css/`, `js/`, `fonts/` and `logo/` folders **unchanged** from `C:\Users\ChristianIllingworth\Projects\Consultiv-Utilities-Brand\cu-web-design-kit\cu-web-design-kit\` into `PricingTool/wwwroot/cu/`, keeping them together (`base.css` loads `../fonts/`). Don't copy `index.html`, `standalone.html` or the Markdown files.
- [ ] T006 [P] Create `PricingTool/Properties/launchSettings.json` (`https://localhost:8101;http://localhost:8100`, `ASPNETCORE_ENVIRONMENT=Development`), plus `PricingTool/appsettings.json` and `PricingTool/appsettings.Development.json` with every key in [contracts/configuration.md](contracts/configuration.md):
  - `appsettings.json` holds safe defaults: `Powwr:Pricing:Mode=Mock`, `Powwr:MeterLookup:Mode=Mock`, `Authentication:Mode=Entra`, `Authentication:SessionHours=8`, `Outlook:Mode=Graph`, `Storage:Files:LocalRoot=App_Data/files`, `Storage:ReportingExport:LocalRoot=App_Data/reporting`, and `Company:*` placeholders marked "to confirm".
  - The Development file has the `.\SQLEXPRESS` `CuPricingTool` connection string (Windows sign-in), `Authentication:Mode=Development` and `Outlook:Mode=Eml`.
  - No secrets in either file.
- [ ] T007 Create the options classes in `PricingTool/Options/`: `AuthenticationOptions`, `PowwrPricingOptions`, `MeterLookupOptions`, `OutlookOptions`, `StorageOptions`, `CompanyOptions`, `RetentionOptions`, `PdfOptions`, `UploadOptions`, `ReportingOptions`. Bind each with `ValidateOnStart()`. In `Live`/`Sandbox` modes, a missing secret fails startup with a message naming the **key**, never a value. Then write the minimal `PricingTool/Program.cs`:
  - `AddRazorPages`, standard structured logging, `UseExceptionHandler("/Error")` and HSTS outside Development;
  - HTTPS redirection, static files, routing;
  - `TimeProvider.System` registered as a singleton;
  - **no database access at startup**.
- [ ] T008 [P] Replace the placeholder "Commands" section of `CLAUDE.md` with `dotnet build`, `dotnet test`, `dotnet run --project PricingTool` and `dotnet ef database update --project PricingTool`. Create a `README.md` skeleton covering prerequisites (the .NET 10 SDK and SQL Server Express only), then `dotnet tool restore`, `dotnet dev-certs https --trust`, `dotnet build`, `dotnet pwsh PricingTool/bin/Debug/net10.0/playwright.ps1 install chromium`, `dotnet ef database update --project PricingTool` and `dotnet run --project PricingTool`.
- [ ] T009 [P] Create `THIRD-PARTY-NOTICES.md` with the Lucide icons (ISC) and Poppins (SIL OFL, from `wwwroot/cu/fonts/OFL.txt`).

**Checkpoint**: `dotnet build` and `dotnet test` succeed with an empty test project, and the app starts on `https://localhost:8101`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: sign-in and roles, the app shell, the database, audit, settings, validation, both POWWR
adapters with mocks, **the pricing engine**, and the reporting recorder. Every user story needs these.

**⚠️ CRITICAL**: no user story work can begin until this phase is complete.

### Time and domain types

- [ ] T010 [P] Create `PricingTool/Services/Time/UkCalendar.cs`, with `Today(TimeProvider)` and `DateOf(DateTime utc)` on `Europe/London` (fallback "GMT Standard Time"), and `PricingTool.Tests/Support/FakeTimeProvider.cs`, a small `TimeProvider` subclass with a settable now. Add `PricingTool.Tests/Time/UkCalendarTests.cs` covering UK midnight and both British Summer Time changes (research R14).
- [ ] T011 [P] Create the domain enums in `PricingTool/Domain/Enums.cs`, stored as strings by EF, with the exact members listed in data-model.md:
  - **Quote and customer**: `Fuel`, `QuoteType` (Renewal, NewSupplier, ChangeOfTenancy), `BusinessType` (SoleTrader, Partnership, LimitedCompany, Llp, Plc, Charity, PublicSector), `PaymentMethod` (DirectDebit, Bacs, CashCheque), `PaymentFrequency`, `PaymentAmount`, `SupplierFilterMode`, `QuoteStatus` (Draft, Priced, Quoted).
  - **Pricing**: `VatCclTreatment` (Standard, Qualifying), `SiteTreatment` (Standard, LowUse, Qualifying), `ConsumptionEntryMode`, `ForcedRateStructure` (Standard, Economy7, EveningAndWeekend, EveningWeekendAndNight, OffPeak), `Register` (Day, Night, Wend), `PricingRunStatus` (Succeeded, Incomplete, Failed), `PricingRunSiteStatus` (Succeeded, Failed, NotPriced), `ErrorCategory` (Timeout, RateLimited, PowwrError, InvalidResponse, Network, CredentialsRejected), `CommissionBasis` (PerYear, WholeContract), `MicrobusinessSource` (Consumption, Declaration, Both, None).
  - **Outputs and settings**: `DraftMethod` (Graph, Eml), `ReportingEventType` (QuoteRequested, PricesReturned, Repriced, PdfGenerated, OpenedInOutlook, Failed), `EffectiveRateKind`, `SiteWarningCode` (Domestic, DeEnergised, GreenDealActive, SwitchInProgress, Igt, LargeGasSite, MhhsNoSsc, SupplierNotMapped), `FieldSource` (Lookup, Agent, AgentChanged).

### Validation (tests first)

- [ ] T012 [P] Write `PricingTool.Tests/Validation/MpanCoreTests.cs`:
  - The core must be exactly 13 digits.
  - The check digit is (Σ dᵢ×pᵢ for the first 12 digits, primes 3, 5, 7, 13, 17, 19, 23, 29, 31, 37, 41, 43) mod 11 mod 10.
  - POWWR's examples `1610028163273` and `1620000546231` and the mock cores `1600000001013`, `1200000000011` and `2300000000019` are valid; `1600000001014` is invalid.
  - Distributor IDs 10–23 map to the named regions in research R15, 24–99 to "Independent network (IDNO)", and 00–09 are invalid.
- [ ] T013 [P] Write `PricingTool.Tests/Validation/MpanTopLineTests.cs`:
  - A legacy 8-digit top line is PC(2) + MTC(3) + LLFC(3).
  - A 9-character MHHS top line is PC(2) + SSC(4) + DUoS tariff ID(3). POWWR's example `040151661` gives PC 04, SSC `0151` and DUoS `661`, and sets `MhhsMigrated = true`.
  - The top line must agree with the individual fields (spec FR-015, FR-016, edge case "MPAN top line and individual fields disagree").
- [ ] T014 [P] Write `PricingTool.Tests/Validation/FieldFormatTests.cs`:
  - **MPRN**: digits only, 6–10 long.
  - **Postcode**: `Postcode.Normalise` uppercases, strips spaces, checks `^[A-Z]{1,2}[0-9][A-Z0-9]?[0-9][A-Z]{2}$`, and inserts one space, so `zz11aa` becomes `ZZ1 1AA`.
  - **Company number**: only for LimitedCompany, Llp and Plc; 8 digits, or 2 letters + 6 digits.
  - **UK phone**: `^(\+44|0)\d{9,10}$` after removing spaces.
  - **Email**: a valid address with a dotted domain.
- [ ] T015 [P] Write `PricingTool.Tests/Validation/RegisterSplitTests.cs`: a total with a percentage split must total exactly 100%, and the per-register kWh uses the largest-remainder method so it adds up to the total (FR-022, R15).
- [ ] T016 Implement the value types and validators so T012–T015 pass:
  - `PricingTool/Domain/MpanCore.cs`, with the region lookup;
  - `PricingTool/Domain/MpanTopLine.cs`, with the single `Parse` method that holds the 9-character layout (open item P-10);
  - `PricingTool/Domain/Mprn.cs` and `PricingTool/Domain/NormalisedPostcode.cs`;
  - `PricingTool/Services/Validation/Postcode.cs`, `CompanyNumber.cs`, `UkPhone.cs`, `EmailAddress.cs` and `RegisterSplit.cs`.
- [ ] T017 [P] Write `PricingTool.Tests/Validation/QuoteInputValidatorTests.cs`:
  - field-keyed errors for each rule in data-model.md "Validation rules by requirement", for FR-005 to FR-007, FR-012 (a domestic meter needs confirmation), FR-013 (change of tenancy needs `CotDate`), FR-015 to FR-018 (half-hourly needs kVA > 0), FR-022, FR-024 (start date not before today in UK time; at least one term from {12, 24, 36, 48, 60}), FR-025 (uplifts ≥ 0, at most 4 decimal places), FR-029 (a qualifying percentage of 0–100 when Qualifying) and FR-043;
  - valid input gives no errors.
- [ ] T018 Implement the input models (`QuoteInput`, `CustomerInput`, `SiteInput`, `MeterInput`, `ContractInput`) in `PricingTool/Services/Validation/QuoteInput.cs`, and `QuoteInputValidator` in `PricingTool/Services/Validation/QuoteInputValidator.cs`. It returns a dictionary of field path → messages, so that T017 passes. It is the single validator used by the form, the services (as a guard) and upload rows.

### Database

- [ ] T019 [P] Create the quote entities in `PricingTool/Data/Entities/`: `StaffMember.cs`, `Quote.cs` (with owned `Customer` and `Contract`), `Site.cs` (with an owned `Meter`) and `QuoteUpliftOverride.cs`, with the fields, types, lengths and nullability in data-model.md:
  - **StaffMember**: `EntraObjectId string(64)`, unique; `CustomerViewOn bool`.
  - **Quote**: `Reference string(16)`, unique, `CUQ-yy-nnnnnn`; `CotDate DateOnly?` ("Required when ChangeOfTenancy"); `CompanyNumber string(8)?`; `TermsMonths string(20)` (a set from {12, 24, 36, 48, 60}); `QualifyingPercent decimal(9,4)?`; `RowVersion rowversion`; `LastActivityUtc`.
  - **Site**: `Postcode string(8)`; JSON `Warnings`, `FieldSources` and `LookupSnapshot`.
  - **Meter**: `MpanTopLine string(9)?`, `MpanCore string(13)`, `Mprn string(10)?`, `IsLargeGasSite bool`.
  - **QuoteUpliftOverride**: unique (QuoteId, SupplierId, TermMonths).
- [ ] T020 [P] Create the pricing entities in `PricingTool/Data/Entities/`: `PricingRun.cs`, `PricingRunSite.cs`, `NoQuoteResult.cs`, `PriceOption.cs` and `PriceOptionSite.cs`, as in data-model.md:
  - **Money and engine figures**: `decimal(28,10)`. **Rate strings**: `nvarchar(32)`, stored exactly as returned.
  - **PricingRunSite**: `RequestJson` with `LicenceCode` masked; `ErrorMessage string(1000)?`, which never contains secrets.
  - **PriceOption**: `RequestedUnitRateUplift`, `RequestedStandingChargeUplift`, `UnitRateUplift`, `StandingChargeUplift` and `IsCappedAtSupplierMax`; unique (PricingRunId, SupplierName, TermMonths, PlanType).
  - **PriceOptionSite**: the agent-only `BaseDayRate`, `BaseNightRate`, `BaseWendRate`, `BaseStandingCharge`, `ExtraInfo` and `FooterMessage`.
- [ ] T021 [P] Create the settings entities in `PricingTool/Data/Entities/`, as in data-model.md:
  - `Supplier.cs`: `PricingName string(100)`, unique; `UpfrontPercent decimal(9,4)`, 0–100; nullable `MaxUnitRateUpliftElectricity`, `MaxUnitRateUpliftGas`, `MaxStandingChargeUpliftElectricity` and `MaxStandingChargeUpliftGas` (`decimal(18,6)?`, "Blank means no uplift is allowed"); `RowVersion`.
  - `SupplierAlias.cs`: at least one of `PowwrId string(20)?`, `Mpid string(10)?` or `LookupName string(200)?`.
  - `SettingVersion.cs`: unique (Key, Version); `IsPlaceholder`; `ComplianceApprover string(200)?`.
  - `EffectiveRate.cs`: unique (Kind, EffectiveFrom).
- [ ] T022 [P] Create the audit and reporting entities in `PricingTool/Data/Entities/`, as in data-model.md:
  - `AuditEntry.cs`: `long` key; `QuoteId` indexed.
  - `ReportingRecord.cs`: `Guid` key; `SchemaVersion int`; `EventType`; denormalised filter columns; `Payload nvarchar(max)`.
  - `ReportingRecordSupplier.cs`: composite key; indexed `SupplierName`.
  - `ReportingOutboxEntry.cs`: `NextAttemptUtc` and `ExportedUtc` indexed together.
- [ ] T023 Create `PricingTool/Data/PricingDbContext.cs` and one `IEntityTypeConfiguration` per entity in `PricingTool/Data/Configurations/`:
  - decimal precisions exactly as in data-model.md "Storage conventions";
  - enums as strings (`nvarchar(32)`);
  - a UTC `DateTime` converter (`datetime2(3)`) and `DateOnly` → `date`;
  - owned types `Customer`, `Contract` and `Meter` in their owner's table;
  - `rowversion` on `Quote` and `Supplier`;
  - every index in data-model.md;
  - the SQL Server sequence `QuoteNumbers` (`HasSequence`).

  Register it in `Program.cs` with `ConnectionStrings:PricingDb`.
- [ ] T024 Write the pure decision `AppendOnlyGuard.IsForbidden(entityType, state, entity)`, and the `SaveChangesInterceptor` that throws on it, in `PricingTool/Data/AppendOnlyGuard.cs`:
  - Modified or Deleted is forbidden for `AuditEntry`, `SettingVersion`, `EffectiveRate` (except deleting one whose `EffectiveFrom` is after today), `PricingRun`, `PricingRunSite`, `NoQuoteResult`, `PriceOptionSite`, `CustomerPdf`, `OutlookDraft`, `ReportingRecord` and `ReportingRecordSupplier`.
  - It's also forbidden for any `PriceOption` change other than `IsSelected` or `IsRecommended`.

  Test it in `PricingTool.Tests/Data/AppendOnlyGuardTests.cs` (research R19). Register the interceptor on the DbContext.
- [ ] T025 Create the seed data in `PricingTool/Data/Seed/SeedData.cs`, applied through `HasData` in the configurations:
  - **Suppliers**: British Gas, EDF, E.ON Next, Octopus Energy, Opus Energy, Scottish Power, Smartest Energy, Valda Energy, Crown Gas & Power and Yu Energy.
    - All active for both fuels, with `UpfrontPercent` 0.
    - Starting maximums set for **both fuels**, marked "to confirm", because a blank maximum allows no uplift (FR-060).
    - Made-up aliases, including `BRITISHG-001-S`/`ENRD`/"British Gas Business" → British Gas, `SCOTTISH-001-S`/"Scottish Power" → Scottish Power, and `EONNEXT-001-S`/"E.On Next" → E.ON Next.
  - **SettingVersion v1 rows** for every key in data-model.md "Keys":
    - default uplifts per fuel, each ≤ that fuel's maximum;
    - `UpliftLimit.*` maximums;
    - `Commission.Basis` = PerYear and `Payment.Default` = monthly Direct Debit;
    - microbusiness thresholds 100000 and 293000;
    - large-gas limits blank;
    - `Retention.Years` = 6;
    - every `Wording.*` key as visibly placeholder text with `IsPlaceholder = true`, including the credit check starting text "All prices are subject to supplier credit check and acceptance" and the three `Wording.Microbusiness.*` keys;
    - a placeholder `Terms.Url`.
  - **EffectiveRate rows**: VatStandard 20, VatReduced 5, CclElectricity and CclGas (marked "to confirm"), LowUseElectricityKwhPerDay 33 and LowUseGasKwhPerDay 145.
- [ ] T026 Create the first migration with `dotnet ef migrations add InitialCreate --project PricingTool` (files in `PricingTool/Data/Migrations/`). Check that `dotnet ef database update --project PricingTool` creates `CuPricingTool` on `.\SQLEXPRESS` with the seed data.

### Sign-in, roles and access

- [ ] T027 [P] Create the roles and policies in `PricingTool/Auth/`:
  - `RoleNames.cs` with `Agent` and `Admin` only.
  - `Policies.cs`: `CanQuote` = Agent; `CanViewAllQuotes` = Admin; `CanAdmin` = Admin.
  - `GroupAndRoleHandler.cs`: the fallback requirement. In Entra mode it needs the `groups` claim to contain `AzureAd:AllowedGroupId` and at least one app role; otherwise it needs at least one app role (FR-001, FR-002, R2).
- [ ] T028 Create `PricingTool/Auth/AuthenticationSetup.cs` for Entra mode:
  - Microsoft.Identity.Web OpenID Connect, single tenant;
  - `EnableTokenAcquisitionToCallDownstreamApi(["User.Read"])`, with `Mail.ReadWrite` requested later, incrementally;
  - `AddDistributedTokenCaches()` over `AddDistributedMemoryCache()`;
  - a cookie with an absolute lifetime of `Authentication:SessionHours` and `SlidingExpiration = false` (FR-094);
  - the fallback policy from T027, with access denied going to `/Account/NoAccess`;
  - structured log entries for every sign-in and every refused request, with no tokens (FR-098).

  Call it from `Program.cs`.
- [ ] T029 Create the Development test sign-in (FR-095) in `PricingTool/Auth/DevAuthentication.cs` and `PricingTool/Pages/Dev/SignIn.cshtml(.cs)`:
  - A cookie scheme that is registered **only** when `IHostEnvironment.IsDevelopment()` and `Authentication:Mode=Development`. Startup throws if the mode is `Development` in any other environment, and the page returns 404 outside Development.
  - It lists five made-up people with fixed GUID `oid`s, `@example.com` emails and phones: Alex Agent (Agent), Sam Agent (Agent), Ada Admin (Admin), Bea Both (Agent and Admin) and Pat Nobody (no role).
  - Use the `_StandaloneLayout` (T034).
- [ ] T030 Create `PricingTool/Auth/StaffMemberSync.cs`, which on each sign-in creates or updates the `StaffMember` from `oid`, `name` and `preferred_username`/`email` and records `LastRolesSeen`. Also create `PricingTool/Auth/CurrentStaff.cs`, a scoped service giving the current `StaffMember` and roles for the request.
- [ ] T031 [P] Write `PricingTool.Tests/Security/QuoteAccessTests.cs`, then implement `PricingTool/Services/Quotes/QuoteAccess.cs`:
  - `CanRead` is true for the owner or an Admin; `CanChange` only for the owner holding the Agent role (FR-003, FR-093).
  - An agent reading another agent's quote gets "not found", not "forbidden".
- [ ] T032 Write `PricingTool.Tests/Security/DevSignInTests.cs` and `PricingTool.Tests/Security/AuthorisationTests.cs` using `WebApplicationFactory<Program>`, with a dummy connection string and the database never touched:
  - `/Dev/SignIn` returns 404 in Production;
  - `Authentication:Mode=Development` in Production fails at startup;
  - an anonymous request is challenged;
  - a user with no role is sent to NoAccess;
  - an Admin-only user is refused `/Quotes/New`;
  - the auth cookie expires after 8 hours with no sliding renewal (FR-001, FR-002, FR-093 to FR-095).

### App shell

- [ ] T033 [P] Create the icons in `PricingTool/TagHelpers/Icons.cs` and `PricingTool/TagHelpers/IconTagHelper.cs`:
  - Lucide path data for each icon used: file-plus, list, bar-chart-2, settings, log-out, search, map-pin, zap, flame, alert-triangle, check, x, download, mail, eye, eye-off, upload, refresh-cw, chevron-left, info.
  - `<icon name="…">` renders an inline `<svg viewBox="0 0 24 24" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" fill="none" aria-hidden="true">` at 16px by default.
  - No npm and no emoji.
- [ ] T034 Create the layouts in `PricingTool/Pages/Shared/`, with `_ViewImports.cshtml` and `_ViewStart.cshtml`:
  - `_Head.cshtml`: `~/cu/css/tokens.css`, `base.css` and `components.css` with `asp-append-version="true"`; a preload of `~/cu/fonts/poppins-700.woff2`; the favicon `~/cu/logo/favicon-32.png`; then `~/css/app.css`; deferred `~/cu/js/kit.js` and `~/js/app.js`.
  - `_Layout.cshtml`: the kit app shell.
    - The `.sidebar` uses the white `cu-logo-150-white.png` with a `cu-logo-250-white.png 2x` srcset, and `cu-mark-white.png` for the rail.
    - Nav items are New quote (`CanQuote`), Quotes (signed-in), Reports and Settings (`CanAdmin`), each with a `title`.
    - It shows the signed-in person and Sign out, then the `.topbar` and `.content`.
  - `_StandaloneLayout.cshtml`: `.standalone` with the colour `cu-logo-250.png`.
- [ ] T035 [P] Create `PricingTool/wwwroot/css/app.css`: tokens only, loaded after the kit, with layout glue and each kit gap from plan.md "Components the kit lacks" marked `/* KIT GAP n: proposed for the kit */`. Create `PricingTool/wwwroot/js/app.js` with:
  - `CU`-friendly helpers;
  - a `fetch` wrapper that sends the `RequestVerificationToken` header;
  - `swapFragment(target, html)`, which **refuses** HTML containing `data-menu`, `data-tab` or `.split` (research R24);
  - delegated click and submit handlers.

  No inline scripts anywhere.
- [ ] T036 Create the standalone and root pages:
  - `PricingTool/Pages/Account/SignIn.cshtml(.cs)`, `Loading.cshtml(.cs)` (continues to the OpenID Connect challenge, or to `/Dev/SignIn` in Development mode, with a button if script is off), `NoAccess.cshtml(.cs)` (no data shown) and `SignOut.cshtml(.cs)` (POST);
  - `PricingTool/Pages/Index.cshtml(.cs)`, which sends `CanQuote` users to `/Quotes/New` and Admin-only users to `/Reports`;
  - `PricingTool/Pages/Error.cshtml(.cs)`, which shows only a reference ID (the trace ID) outside Development.
- [ ] T037 Create `PricingTool/Infrastructure/SecurityHeaders.cs`, middleware setting CSP `default-src 'self'; img-src 'self' data:; style-src 'self'; script-src 'self'; frame-ancestors 'none'; form-action 'self' https://login.microsoftonline.com`, `X-Content-Type-Options: nosniff` and `Referrer-Policy: same-origin`. Configure antiforgery with header name `RequestVerificationToken` in `Program.cs`.

### Audit and settings

- [ ] T038 [P] Create `PricingTool/Services/Audit/AuditWriter.cs`, which adds an `AuditEntry` (actor ID and name, UTC time, action from data-model.md "Actions", entity, quote ID, old and new JSON) to the **current** DbContext, so it saves in the same `SaveChanges` as the action. It never serialises secrets.
- [ ] T039 Write `PricingTool.Tests/Settings/EffectiveRateLookupTests.cs`: the latest row with `EffectiveFrom ≤ date` wins, and a date before any row is an error. Then create `PricingTool/Services/Settings/SettingsService.cs`, providing:
  - the current value per `SettingVersion` key, with typed getters;
  - effective-rate lookup by date;
  - `GetPricingSnapshot(fuel, siteStartDates)`, which returns VAT and CCL rates per site start date, low-use and microbusiness thresholds, the commission basis, supplier payment models and maximums, and per-fuel maximums, for `PricingRun.SettingsSnapshot`.
- [ ] T040 [P] Write `PricingTool.Tests/Settings/SupplierMatcherTests.cs` (match order: `powwrId`, then `mpid` or `shortCode`, then `name`; "British Gas Business"/`BRITISHG-001-S` → "British Gas"; no match → null). Then implement `PricingTool/Services/Settings/SupplierMatcher.cs` with a cached supplier list and an `Invalidate()` method.

### POWWR UK Pricing adapter (tests first)

- [ ] T041 [P] Create the DTOs in `PricingTool/Powwr/Pricing/Dtos/`, mirroring `docs/powwr/uk-pricing-api-1.2.0.json` exactly:
  - PascalCase names, with no naming policy and nulls left out;
  - request wrappers `quoteDetails`, `promptsSearch` and `priceChangeRequest`;
  - result wrappers `GetElectricRatesResult`, `GetGasRatesResult`, `GetNumberOfPromptsResult`, `GetLatestPriceChangeResult` and `PingResult`;
  - every `Rate` field, with the `Raw*` fields as `decimal`.
- [ ] T042 [P] Create the domain models and the interface in `PricingTool/Powwr/Pricing/Models.cs` and `PricingTool/Powwr/Pricing/IPowwrPricingClient.cs`, exactly as in [contracts/adapters.md](contracts/adapters.md): `ElectricPriceRequest`, `GasPriceRequest`, `SitePriceResponse`, `ReturnedRate`, `SupplierNoQuote`, `RateStructureResponse` and `SupplierPriceChange`.
- [ ] T043 [P] Write `PricingTool.Tests/Powwr/PowwrPricingRequestMappingTests.cs`, with one test per row of [contracts/powwr-uk-pricing-mapping.md](contracts/powwr-uk-pricing-mapping.md) for QuoteDetails, ElectricSupply and GasSupply:
  - `SecurityDetails` holds the licence code and `mascaradeuser` = the agent's display name, and the stored copy has `LicenceCode` = `***`;
  - `QuoteReference`; `Settings` `ShowNoQuoteReason=true`; `Renewal`, `COT`, `CotDate` and `OutOfContract`;
  - `PaymentMethodText`, for example "Monthly Direct Debit"; `BusinessType` through the configuration map;
  - `QuoteDefinitions`: one per active, included supplier for the fuel × each term, with `PlanType` left out;
  - profile class and MTC as integers; a 9-character top line passed through; `ForcedRateStructure`;
  - Day, Night and Wend consumption, and kVA and kVArh;
  - `ContractRenewalDate` = the start date; `NewContractEndDate` only when one term is requested;
  - gas `MPR` and `Consumption`.
- [ ] T044 [P] Write `PricingTool.Tests/Powwr/PowwrPricingResponseMappingTests.cs`:
  - rate strings kept exactly, and `Raw*` decimals exact (for example 4269.2327985);
  - `NoQuoteReason` → `SupplierNoQuote`;
  - `QuoteResult.Error` → `Succeeded=false`, `PowwrError`;
  - a non-numeric `Term` → `InvalidResponse`;
  - a missing or negative `RawAnnualPrice` → an invalid rate;
  - `/prompts` names → registers ("Day", "Unit" or "Standard" → Day; "Night" → Night; "Evening and Weekend", "Weekend" or "Wend" → Wend; unknown values logged);
  - `/pricechange` → `SupplierPriceChange`.
- [ ] T045 Implement `PricingTool/Powwr/Pricing/PowwrPricingMapper.cs`, `PaymentMethodText.cs` and `BusinessTypeText.cs` (reading `Powwr:Pricing:BusinessTypes`) so T043 and T044 pass.
- [ ] T046 Implement `PricingTool/Powwr/Pricing/PowwrPricingHttpClient.cs`, a typed `HttpClient`:
  - base URL by mode: SwaggerHub `https://virtserver.swaggerhub.com/POWWR/UkPricingAPI/1.2.0`, Live `https://udcoreapi.co.uk/Service.svc/web`;
  - a timeout of `TimeoutSeconds`;
  - it always checks the `Error` object;
  - a credentials rejection becomes `CredentialsRejected`.

  Also implement `PricingTool/Powwr/Pricing/PowwrRetryHandler.cs`, which retries only 429 and 503, honouring `Retry-After` capped at 10 s, with at most 2 retries. Test it in `PricingTool.Tests/Powwr/PowwrRetryHandlerTests.cs` with a fake inner handler and no network (research R6).
- [ ] T047 Implement `PricingTool/Powwr/Pricing/MockPowwrPricingClient.cs` exactly as in the contract's "Mock adapter behaviour" section:
  - **Suppliers and rates**: the ten suppliers; rates seeded by MPAN or MPRN and consumption; base rates in the given ranges plus 2% per extra year.
  - **Figures**: the plan's uplift applied; `RawAnnualPrice` = Σ(rate×kWh)/100 + SC×365/100; `RawBaseAnnualPrice` without uplift; `RawCommission` = the difference; `PricebookVersion` `MOCK-yyyyMMdd`.
  - **Variety**: Valda declines; Yu 60-month is outside the pricing window; Octopus has a `FooterMessage`; Opus has a product with SC `"0.00"`.
  - **Scenario triggers**: MPAN `2300000000019` → every supplier declines; MPRN `9999999` → `Error`; MPAN `1200000000020` → British Gas and Octopus only.
  - **`/pricechange`**: suppliers listed in `Powwr:Mock:ChangedSuppliers` return `MOCK-CHANGED`.

  Test in `PricingTool.Tests/Powwr/MockPowwrPricingClientTests.cs` that it's deterministic and that the cross-check matches `RawAnnualPrice`.

### POWWR Meter Lookup adapter (tests first)

- [ ] T048 [P] Create `PricingTool/Powwr/MeterLookup/Dtos/` (camelCase, read case-insensitively), and `PricingTool/Powwr/MeterLookup/Models.cs` and `IMeterLookupClient.cs` as in [contracts/adapters.md](contracts/adapters.md): `LookupResult<T>` (Found/NotFound/Unavailable/Rejected), `LookupAddress`, `LookupMeters`, `LookupElectricMeter`, `LookupGasMeter` and `MeterSearch`.
- [ ] T049 [P] Write `PricingTool.Tests/Powwr/MeterLookupMappingTests.cs`, using POWWR's example response bodies from `docs/powwr/meter-lookup-api-2.5.0.json` (with their mixed casing):
  - `profileClass` "03" → 3 and `meterTimeswitchClass` "801" → 801; a non-numeric value → null plus a warning;
  - redacted MPANs are kept as hints only;
  - `amrIndicator` "Y" → smart suggested;
  - `isIGT` → `Igt`; `incomingSupplier` → `SwitchInProgress`, naming the supplier;
  - `isDomesticConsumer` → `Domestic`; `energisationStatus` "D" → `DeEnergised`; `greenDealStatus` "Active" → `GreenDealActive`;
  - an unmatched supplier → `SupplierNotMapped`;
  - an RFC 7807 body → `Rejected`, with a safe summary.
- [ ] T050 Implement `PricingTool/Powwr/MeterLookup/MeterLookupMapper.cs` (passing T049), plus:
  - `MeterLookupTokenProvider.cs`: client credentials with scopes `udgs-lookupservices-meter-api brokerage:{OrganisationPowwrId}`, cached until 60 s before expiry, refreshed under a `SemaphoreSlim`;
  - `MeterLookupHttpClient.cs`:
    - base URLs by mode, as in [contracts/powwr-meter-lookup-mapping.md](contracts/powwr-meter-lookup-mapping.md);
    - `callerIdentification` with the `organisationPowwrId` (validated `^[A-Z0-9]{1,8}-\d{3}-[SB]$`), `applicationId` "CU Pricing Tool", and `userId` = the agent's Entra object ID;
    - on 401 or 403, clear the token and retry once;
    - timeouts and 5xx → `Unavailable`;
    - SwaggerHub mode sends no token.
- [ ] T051 Implement `PricingTool/Powwr/MeterLookup/MockMeterLookupClient.cs` with every postcode and meter in the contract's mock table (`ZZ1 1AA` to `ZZ1 1AG`, `ZZ9 9ZZ` → NotFound, `ZZ9 9ZY` → Unavailable). Searching by MPAN or MPRN finds any of those meters, plus `2300000000019`. All data is made up.
- [ ] T052 Create `PricingTool/Powwr/PowwrServiceCollectionExtensions.cs`, which registers both adapters by mode (Mock is the default), the typed `HttpClient`s with `PowwrRetryHandler`, and `SemaphoreSlim(MaxConcurrentRequests)`. Call it from `Program.cs`. Tests never register an HTTP client.

### Pricing engine (tests first)

- [ ] T053 [P] Write `PricingTool.Tests/Pricing/RoundingTests.cs`. `Rounding.ToPennies` = `Math.Round(x, 2, MidpointRounding.AwayFromZero)`; `MoneyFormat` uses en-GB `£1,234.56`; totals are rounded from the full-precision sum (research R7).
- [ ] T054 [P] Write `PricingTool.Tests/Pricing/VatCclCalculatorTests.cs`:
  - **Standard**: CCL = kWh×rate/100, VAT = (cost + CCL)×20%.
  - **Low use**: at or below 33 (electricity) or 145 (gas) kWh a day, worked out as annual ÷ 365 and including the exact boundary, gives 5% VAT and no CCL.
  - **Qualifying q%**: CCL = kWh×(1−q)×rate; VAT = cost×q×5% + (cost×(1−q) + CCL)×20%. The whole-supply percentage, when set and met, makes q = 100%.
  - Rates come from each site's start date (FR-029 to FR-031, research R8).
- [ ] T055 [P] Write `PricingTool.Tests/Pricing/CommissionCalculatorTests.cs`:
  - PerYear: contract total = RawCommission × term ÷ 12. WholeContract: contract total = RawCommission.
  - Upfront = total × UpfrontPercent ÷ 100, for 0, 100 and 40.
  - Monthly = (total − upfront) ÷ term months.
  - Multi-site sums (FR-037, research R9).
- [ ] T056 [P] Write `PricingTool.Tests/Pricing/SavingCalculatorTests.cs`:
  - A saving is only worked out when the current SC and a current rate for every register are given.
  - Current cost = Σ(rate×kWh)/100 + SC×365/100.
  - Saving = current − new, positive meaning "Saves" and negative "Costs more".
  - Including VAT and CCL uses the same treatment for both sides (research R11).
- [ ] T057 [P] Write `PricingTool.Tests/Pricing/MicrobusinessTestTests.cs`: ≤ 100,000 kWh electricity or ≤ 293,000 kWh gas is flagged (exactly at the threshold counts); consumption is combined across all of the quote's sites; the declaration flags only when **both** answers are yes; the source is Consumption, Declaration, Both or None (FR-027, FR-028).
- [ ] T058 [P] Write the remaining engine tests:
  - `PricingTool.Tests/Pricing/MultiSiteCombinerTests.cs`: options keyed by supplier, term and `PlanType`; partial when any site is missing; totals at full precision; a total saving only when every covered site has one; `HasNoStandingCharge` when every SC parses as 0.
  - `PricingTool.Tests/Pricing/AnnualCostCrossCheckTests.cs`: a warning is logged when the difference is over 1% or over £1, and the shown figures never change.
  - `PricingTool.Tests/Pricing/BaseRateTests.cs`: a base rate is the returned rate minus the uplift, with the returned string's number of decimal places.
- [ ] T059 Implement `PricingTool/Services/Pricing/Rounding.cs` and `MoneyFormat.cs` so T053 passes.
- [ ] T060 Implement `VatCclCalculator.cs`, `LowUseTest.cs`, `MicrobusinessTest.cs`, `CommissionCalculator.cs`, `SavingCalculator.cs`, `AnnualCostCrossCheck.cs` and `BaseRates.cs` in `PricingTool/Services/Pricing/`, all pure, static and `decimal`-only, so T054–T058 pass.
- [ ] T061 Implement `PricingTool/Services/Pricing/MultiSiteCombiner.cs` and `PricingTool/Services/Pricing/PricingEngine.cs`. The engine is stateless. Its input is each site's `SitePriceResponse`, the site inputs and the `PricingSettingsSnapshot`; its output is a `PricingResult` holding everything needed for `PricingRunSite`, `PriceOption` and `PriceOptionSite`:
  - per site: treatment, rates used, current costs;
  - per site and option: cost excluding VAT and CCL (= `RawAnnualPrice`), CCL, VAT, cost including both, savings, commission split, base rates, cross-check;
  - per option: totals, partial flags, the microbusiness result and the commission basis.

  It does no rounding (R7).

### Reporting foundation

- [ ] T062 [P] Write `PricingTool.Tests/Reporting/ReportingRecordBuilderTests.cs`:
  - `schemaVersion` = 1, and every field in FR-084 and [contracts/reporting-record.md](contracts/reporting-record.md) is present;
  - money as `{ value, display }` pairs, with `display` = `Rounding.ToPennies`;
  - the commission basis in `pricingRun`, and `cappedAtSupplierMax` on each price and option;
  - **no** contact name, email or phone, and no licence code;
  - the required properties of `contracts/reporting-record.v1.schema.json` are present for each event type.
- [ ] T063 Implement `PricingTool/Services/Reporting/ReportingRecordBuilder.cs` and `ReportingRecorder.cs`. The recorder adds a `ReportingRecord`, its `ReportingRecordSupplier` rows and a `ReportingOutboxEntry` to the current DbContext, so they save in the same transaction as the action, with no export on the request path (FR-089).
- [ ] T064 [P] Create `PricingTool/Services/Reporting/IReportingExportStore.cs` and `LocalFolderReportingExportStore.cs`, which writes to `Storage:ReportingExport:LocalRoot` through a temporary file and an atomic move, and deletes for retention. Also create `ReportingExportPath.cs`, which builds `quotes/yyyy/MM/dd/{HHmmssfff}-{eventType}-{recordId}.json` from UTC. Test it in `PricingTool.Tests/Reporting/ReportingExportPathTests.cs`.

**Checkpoint**: the app starts, and the Development sign-in shows the app shell with role-based navigation. Pat Nobody sees No access. `dotnet ef database update` seeds the database. Every validation, mapping, engine and reporting-builder test passes. **User stories can now begin.**

---

## Phase 3: User Story 1 - Quote electricity and compare prices for one site (Priority: P1) 🎯 MVP

**Goal**: an agent finds an electricity meter from a postcode (or types it in) and enters the
customer, supply and contract details. They get prices from every supplier with uplift, and compare
them using agent-only columns. They can sort, filter, change uplifts and re-price, select options and
recommend one. "Customer view" hides the agent-only data.

**Independent Test**: as Alex Agent, price `ZZ1 1AA` from the postcode and check the results,
agent-only columns, sorting, filtering, re-pricing, selection and Customer view, with no PDF, Outlook,
limit or admin features (quickstart.md "US1").

### Tests for User Story 1 ⚠️

- [ ] T065 [P] [US1] Write `PricingTool.Tests/Quotes/PricingRequestBuilderTests.cs`:
  - one request per site;
  - plans for each active, included supplier × each requested term;
  - the uplift for each plan is the override for (supplier, term), else the quote default;
  - the agent's display name as `mascaradeuser`;
  - a domestic meter without confirmation is refused.
- [ ] T066 [P] [US1] Write `PricingTool.Tests/Quotes/RateStructureTests.cs`: each `ForcedRateStructure` maps to its registers (Standard → Day; Economy7 → Day, Night; EveningAndWeekend → Day, Wend; EveningWeekendAndNight → Day, Night, Wend; OffPeak → Day, Night), and an MHHS meter with no SSC requires a forced structure (FR-020, FR-021, research R16).
- [ ] T067 [P] [US1] Write `PricingTool.Tests/CustomerSafe/CustomerViewTests.cs`. With `CustomerViewOn = true`, the results view model built by `ResultsViewModelBuilder` contains **no** base rates, commission figures, capped tags or supplier notes; with it off, they are present (FR-099).
- [ ] T068 [P] [US1] Write `PricingTool.Tests/Quotes/ResultsViewModelBuilderTests.cs`:
  - sort by annual cost or commission; filter by term and supplier; the green filter is hidden while `IsGreen` is null;
  - "no standing charge" shows only `HasNoStandingCharge` options (FR-026);
  - figures are shown with VAT and CCL or without, following `IncludeVatCcl` (FR-034);
  - the outside-pricing-window flag and supplier notes are shown (FR-033, FR-038).

### Implementation for User Story 1

- [ ] T069 [US1] Implement `PricingTool/Services/Quotes/QuoteReference.cs`, giving `CUQ-{yy}-{number:000000}` from the `QuoteNumbers` sequence (FR-078). Implement `PricingTool/Services/Quotes/QuoteService.cs`:
  - Create or update a Draft quote and its sites from a validated `QuoteInput`, and record `FieldSources` (Lookup, Agent or AgentChanged) (FR-011).
  - Set `LastActivityUtc` and write the audit entries `QuoteCreated`, `QuoteInputsChanged`, `DomesticConfirmed` and `MicrobusinessDeclarationChanged` (FR-028).
  - Saving inputs marks the quote Draft.
- [ ] T070 [US1] Implement `PricingTool/Services/Quotes/PricingRequestBuilder.cs`, which builds `ElectricPriceRequest`/`GasPriceRequest` from the quote, site, overrides and settings, so T065 passes.
- [ ] T071 [US1] Implement `PricingTool/Services/Quotes/PricingService.cs`:
  - Run `QuoteInputValidator` again as a guard.
  - Call `IPowwrPricingClient` per site in parallel under the `MaxConcurrentRequests` semaphore.
  - Run `PricingEngine` with `SettingsService.GetPricingSnapshot`.
  - In **one** `SaveChanges`, persist the `PricingRun` (RunNumber, InputsSnapshot, SettingsSnapshot, CommissionBasis, microbusiness result, FetchedUtc = earliest successful response, Status), the `PricingRunSite`s (masked request JSON, raw response, PowwrReference, FooterNotes, error category and safe message), the `NoQuoteResult`s, the `PriceOption`s and the `PriceOptionSite`s.
  - In the same `SaveChanges`, write the audit entries `PricingRequested`, `PricingReturned` or `PricingFailed`, and the reporting events `QuoteRequested` (first pricing), `PricesReturned`, `Repriced` or `Failed`.
  - Set the quote's status to Priced (FR-032, FR-035, FR-036, FR-039, FR-041, FR-072).
- [ ] T072 [US1] Create `PricingTool/Pages/Quotes/New.cshtml(.cs)` (policy `CanQuote`):
  - **Fuel**: a `.seg` choice.
  - **Sections**: one `.card` with a `.section-heading` per section: Customer (FR-005 to FR-007, the microbusiness declaration as two yes/no `.check-chip`s), Site and meter, Supply, Consumption, Current supplier (FR-023), and Contract.
  - **Contract**: a quote-type `.seg`, a COT date, an out-of-contract switch, the start date, term `.check-chip`s (12, 24, 36, 48, 60), the payment method `.form-select`s, the supplier include/exclude, a "No standing charge" `.switch-row`, the VAT/CCL treatment with a qualifying %, an "Include VAT and CCL" `.switch-row`, and default uplifts prefilled from the fuel's settings (FR-024, FR-025).
  - **Fields**: `.field` / `.field-label` / `.form-input` / `.form-select` / `.field-hint` / `.form-error`.
  - **POST**: run `QuoteInputValidator`. On errors, re-display them with no POWWR call. When valid, save through `QuoteService`, price through `PricingService`, and redirect to `/Quotes/{id}`.
  - `?handler=SaveDraft` saves without pricing.
- [ ] T073 [US1] Add the meter picker handlers to `PricingTool/Pages/Quotes/New.cshtml.cs`, with partials in `PricingTool/Pages/Quotes/_AddressList.cshtml`, `_MeterList.cshtml` and `_MeterFields.cshtml`:
  - `?handler=Addresses`: validate and normalise the postcode on the server, then call `SearchAddressesAsync` and return a `.search-results` list of buttons, hiding addresses with no meters for the fuel.
  - `?handler=Meters` (by `meterAddressId`) and `?handler=MeterByNumber` (a validated MPAN): return the meters for the quote's fuel only.
  - `?handler=PickMeter`: return the prefilled meter fields with the full MPAN, marked "from lookup". Show `.notice.tone-amber` warnings, and a confirm control for a domestic meter (FR-008 to FR-012).
  - NotFound or Unavailable shows a `.notice` plus "Enter details yourself" (`.btn-link`) and never blocks quoting (SC-005).
  - Fragments must contain no `data-menu`, `data-tab` or `.split`.
- [ ] T074 [US1] Add `?handler=RateStructure` to `PricingTool/Pages/Quotes/New.cshtml.cs`, with the `PricingTool/Pages/Quotes/_ConsumptionFields.cshtml` partial:
  - Validate the electricity meter fields, then call `/prompts`. Send `MPANTop` only for an 8-digit top line.
  - Show only the matching register fields, or a total plus a % split (FR-020, FR-022).
  - For an MHHS meter with no SSC, show the forced-structure choice with the note that only some suppliers can price it (FR-021).
  - If `/prompts` fails, let the agent choose the registers, with an amber notice.
- [ ] T075 [US1] Implement `PricingTool/Services/Quotes/ResultsViewModelBuilder.cs` (passing T067 and T068), and create the `PricingTool/Pages/Quotes/_Results.cshtml` partial:
  - a `.table` in a `.card--flush` with `.table-scroll` and `.num` figures;
  - `.filter-pill` links for term, supplier and "no standing charge";
  - sort links;
  - per option: rates exactly as returned (day, night, Wend, STOD 1–6, SC, capacity, excess capacity, reactive, fixed fee, DD discount, cheque surcharge); the annual cost; the saving as a `.delta--good` or `.delta--bad` mark plus words, never coloured text; and the outside-window flag;
  - agent-only columns: base rates and commission upfront and monthly;
  - supplier notes; a select checkbox and Recommended (`.tag.tone-green`);
  - the no-quote list with POWWR's reasons;
  - `.empty-state` when nothing priced, with each supplier's reason (FR-033 to FR-038).
- [ ] T076 [US1] Create `PricingTool/Pages/Quotes/Details.cshtml(.cs)` (route `/Quotes/{id:int}`):
  - **Access**: read through `QuoteAccess`; any other user gets 404.
  - **Content**: a summary, the microbusiness `.tag`, saved warnings, the latest run's `_Results`, and earlier runs listed by number and time with their pricebook versions (FR-041).
  - **Read-only view**: an Admin who isn't the owner sees no changing controls (FR-003).
  - **Handlers**: `?handler=Results` (fragment), and `?handler=Select` (option IDs and a recommended ID; a single selected option becomes recommended; audited `OptionsSelected` and `RecommendedChanged`; FR-040).
- [ ] T077 [US1] Add to `PricingTool/Pages/Quotes/Details.cshtml(.cs)` and `PricingTool/Pages/Quotes/_UpliftDialog.cshtml`:
  - `?handler=Uplift`: a `.modal-overlay` > `.modal` opened through `data-open`. It saves a `QuoteUpliftOverride` for the supplier and term, ≥ 0, audited as `UpliftChanged` with the old and new values.
  - `?handler=Reprice`: a new `PricingRun` with the same inputs and current uplifts. Earlier runs are kept (FR-039).
- [ ] T078 [US1] Add `?handler=CustomerView` to `PricingTool/Pages/Quotes/Details.cshtml.cs`, with the "Customer view" `.switch-row`. It saves `StaffMember.CustomerViewOn` and re-renders `_Results` **on the server** without base rates, commission, capped tags or supplier notes. The choice stays on across pages until turned off (FR-099, US1-11).
- [ ] T079 [US1] Create `PricingTool/Pages/Quotes/Edit.cshtml(.cs)` (route `/Quotes/{id:int}/Edit`, owner only), reusing New's sections, partials and handlers. Saving marks the quote Draft and returns to Details to re-price.
- [ ] T080 [US1] Add the US1 enhancements to `PricingTool/wwwroot/js/app.js`:
  - the picker `fetch` and swap for Addresses, Meters, PickMeter and RateStructure;
  - result filter and sort links swapping in `?handler=Results`;
  - the Customer view toggle;
  - `.load-steps` while "Get prices" is posting (checking details → asking suppliers → working out costs), with `.skeleton` rows.

  Every flow must still work with script off.
- [ ] T081 [US1] Show POWWR failures clearly (FR-042, FR-097). Errors, timeouts and rate limits show a `.notice.tone-coral` naming what happened, with Retry, and everything entered is kept. `CredentialsRejected` shows "Pricing is unavailable. Tell an admin." Messages never include tokens, credentials or raw responses; the detail goes to the logs only. Files: `PricingTool/Pages/Quotes/Details.cshtml(.cs)` and `PricingTool/Pages/Shared/_Notice.cshtml`.

**Checkpoint**: User Story 1 works end to end with the mock adapters (quickstart US1, steps 1–11).

---

## Phase 4: User Story 2 - Quote gas (Priority: P2)

**Goal**: the same flow as electricity, for a gas meter identified by its MPRN.

**Independent Test**: price `ZZ1 1AG` from the postcode and from a typed MPRN, and check the
large-site and IGT flags (quickstart.md "US2").

### Tests for User Story 2 ⚠️

- [ ] T082 [P] [US2] Write `PricingTool.Tests/Validation/LargeGasSiteTestTests.cs`:
  - AQ, meter capacity and daily metered quantity each have an **optional** limit, and a site is flagged when any value is above its limit;
  - a blank limit isn't checked;
  - a site entered by hand without lookup data is checked against the AQ limit only (FR-045).
- [ ] T083 [P] [US2] Add gas cases to `PricingTool.Tests/Quotes/PricingRequestBuilderTests.cs`: `GasSupply` with `MPR`, `Consumption.Amount` = AQ and `SmartMeter`; gas default uplifts; and no electricity fields (FR-043, FR-044).

### Implementation for User Story 2

- [ ] T084 [US2] Implement `PricingTool/Services/Validation/LargeGasSiteTest.cs`, so T082 passes. In `PricingTool/Services/Quotes/PricingService.cs`, mark large gas sites `NotPriced` with the `LargeGasSite` warning ("needs a manual quote"), and never send them to POWWR (FR-045).
- [ ] T085 [US2] Add the gas fields to the New and Edit pages and their partials (`PricingTool/Pages/Quotes/New.cshtml(.cs)` and `_MeterFields.cshtml`):
  - the MPRN, typed in or picked by postcode, offering only gas meters;
  - the AQ, prefilled;
  - the LDZ shown read-only (blank when entered by hand);
  - smart meter suggested from `amrIndicator`;
  - the current gas supplier and single rate;
  - IGT and switch-in-progress `.notice.tone-amber` warnings, with IGT sites still priced;
  - **gas** default uplifts prefilled (FR-043, FR-044, FR-046, FR-012).
- [ ] T086 [US2] Show gas results in `PricingTool/Pages/Quotes/_Results.cshtml` and `ResultsViewModelBuilder.cs`: single-rate columns (unit rate and SC, plus any transportation charge), with no day, night or half-hourly columns.

**Checkpoint**: User Stories 1 and 2 both work on their own (quickstart US2, steps 1–6).

---

## Phase 5: User Story 3 - Customer PDF quote (Priority: P2)

**Goal**: from the selected options, generate a branded A4 PDF with no agent-only data.

**Independent Test**: from a priced quote with selected options, generate the PDF and check its
content, branding, file name and properties, and that it holds no agent-only figures
(quickstart.md "US3").

### Tests for User Story 3 ⚠️

- [ ] T087 [P] [US3] Write `PricingTool.Tests/CustomerSafe/CustomerModelShapeTests.cs`. It reflects over `CustomerQuoteDocumentModel` and every type inside it, and fails if any property name contains `Base`, `Uplift`, `Commission`, `Note`, `ExtraInfo`, `Footer`, `Capped`, `Limit` or `Pricebook`. The only exception is the `CommissionDisclosure` text (FR-049, FR-100, constitution Principle II).
- [ ] T088 [P] [US3] Write `PricingTool.Tests/CustomerSafe/CustomerPdfContentTests.cs`. Render `CustomerQuoteDocument.razor` with `HtmlRenderer` from a quote with distinctive agent-only values (base rate `12.3456`, uplift `0.9876`, base annual price `£3,210.98`, commission `£4,321.09`), then check:
  - none of them appears in any common format in the HTML, the `<title>` or the file name;
  - with the disclosure placeholders, only the disclosure paragraph contains the commission;
  - all three microbusiness items appear only for microbusiness quotes;
  - placeholder wording sits in the "PLACEHOLDER – NOT APPROVED WORDING" frame;
  - the terms URL is both the `href` and the printed text;
  - the `<title>` is "Consultiv Utilities quote {reference}";
  - every money string equals the stored engine figure formatted by `MoneyFormat` (SC-003, SC-004).
- [ ] T089 [P] [US3] Write `PricingTool.Tests/CustomerSafe/PdfFileNameTests.cs`: `{CustomerName}_{Electricity|Gas}Quote_{yyyy-MM-dd}.pdf`; invalid characters and `/\:*?"<>|` become `-`; whitespace runs become `_`; at most 100 characters; empty becomes `Customer` (FR-048).
- [ ] T090 [P] [US3] Write `PricingTool.Tests/Quotes/PriceValidityTests.cs` (same UK date means valid, including around midnight and the BST changes; FR-052) and `PricingTool.Tests/Quotes/PriceChangeCheckTests.cs` (a `Version` different from the stored `PricebookVersion` for the utility, a selected term and the meter's half-hourly status means changed; the fallback is `LastPriceChange` ≥ the fetched date).
- [ ] T091 [P] [US3] Write `PricingTool.Tests/Quotes/PdfEligibilityTests.cs`. It refuses, naming the reason, when:
  - the prices were fetched on an earlier day;
  - a selected supplier's prices have changed;
  - the price-change check failed;
  - no option is recommended;
  - a site failed technically;
  - outside Development, any wording used is a placeholder.

  A single selected option counts as recommended (FR-050, FR-052, FR-053).
- [ ] T092 [P] [US3] Write `PricingTool.Tests/Storage/LocalFileStoreTests.cs` in a temporary folder: save, open and delete; keys containing `..`, rooted keys and backslashes are rejected.

### Implementation for User Story 3

- [ ] T093 [US3] Add `PricingTool/Data/Entities/CustomerPdf.cs`, as in data-model.md:
  - `PricesFetchedOn DateOnly`, `FileName string(160)`, `StorageKey string(300)`, `Sha256 string(64)`;
  - `OptionIds string(400)`, JSON `WordingVersions`, `IncludedVatCcl`, `IsMicrobusiness`;
  - append-only.

  Add its configuration, then run `dotnet ef migrations add AddCustomerPdf --project PricingTool`.
- [ ] T094 [P] [US3] Implement `PricingTool/Services/Storage/IFileStore.cs` and `LocalFileStore.cs`, rooted at `Storage:Files:LocalRoot`, so T092 passes. Register them in `Program.cs`.
- [ ] T095 [US3] Implement these in `PricingTool/Services/Quotes/`, so T090 and T091 pass:
  - `PriceValidity.cs`;
  - `PriceChangeCheck.cs`, which calls `/pricechange` once with no supplier name and compares;
  - `PdfEligibility.cs`, which checks placeholders through `SettingsService` and `IHostEnvironment`.
- [ ] T096 [P] [US3] Implement `PricingTool/Services/Staff/IStaffDirectory.cs`, plus:
  - `GraphStaffDirectory.cs`: `GET https://graph.microsoft.com/v1.0/me?$select=businessPhones,mobilePhone` with `User.Read`, caching the phone on `StaffMember` for 24 hours;
  - `DevStaffDirectory.cs`: returns the dev sign-in phone.

  The company phone is the fallback.
- [ ] T097 [US3] Implement `PricingTool/Services/Pdf/CustomerQuoteDocumentModel.cs` with exactly the allow-list in [contracts/customer-pdf.md](contracts/customer-pdf.md), and `PricingTool/Services/Pdf/CustomerQuoteModelBuilder.cs`:
  - It builds only from stored engine figures, formatted by `MoneyFormat`; it does no arithmetic.
  - It records the current wording version for each key.
  - The validity text gets the prices-fetched date filled in.
  - It fills `{CommissionTotal}`, `{CommissionPerYear}`, `{CommissionUpfront}`, `{CommissionMonthly}`, `{SupplierName}` and `{TermMonths}` for the **recommended** option, only inside the disclosure. Unknown placeholders are left as they are and logged.
  - It includes the three microbusiness items only when the run's flag is set.
  - It adds the agent's name, email and phone.
- [ ] T098 [US3] Create `PricingTool/Services/Pdf/CustomerQuoteDocument.razor` and `PricingTool/wwwroot/css/pdf.css` (tokens only, `@page` A4, 14 mm margins with 18 mm at the bottom, Poppins), following the layout in contracts/customer-pdf.md:
  - header with the colour logo, company details, the title, reference and date;
  - customer and sites with the full MPAN or MPRN;
  - validity and indicative notice;
  - the comparison table, with the recommended row highlighted by a teal left edge, a tint and a "Recommended" tag;
  - savings as green or coral dots plus words;
  - for multi-site quotes, totals, then the per-site breakdown, then uncovered sites;
  - assumptions; credit check; disclosure; the microbusiness items;
  - agent contact;
  - a fixed footer with the company registration number and a clickable terms link, printed in full;
  - placeholder frames;
  - `<title>Consultiv Utilities quote {reference}</title>`, and no author or keywords meta.
- [ ] T099 [US3] Implement `PricingTool/Services/Pdf/ICustomerPdfRenderer.cs` and `PlaywrightCustomerPdfRenderer.cs`:
  - `RenderHtmlAsync` uses `HtmlRenderer`.
  - `RenderPdfAsync` uses a lazily created singleton Chromium, a new context and page per PDF, `SetContentAsync` with `<base href="https://pdf.cu.local/">`, a route handler that serves `https://pdf.cu.local/**` from `WebRootFileProvider` and **aborts every other request**, and A4 with `PrintBackground` and `PreferCSSPageSize`.
  - `Pdf:MaxConcurrentRenders` is a semaphore (research R17).
  - Implement `PricingTool/Services/Pdf/PdfFileName.cs`, so T089 passes.
- [ ] T100 [US3] Add `?handler=GeneratePdf` to `PricingTool/Pages/Quotes/Details.cshtml.cs`:
  - Run `PdfEligibility`. On failure, a `.notice.tone-coral` names the check that failed.
  - On success, build the model, render, and save it through `IFileStore` under `pdfs/{yyyy}/{MM}/{reference}/{pdfId}.pdf`.
  - In one `SaveChanges`: the `CustomerPdf` row, audit `PdfGenerated`, the `PdfGenerated` reporting event, status Quoted, and `LastActivityUtc`.
  - The `.btn-cta` with `.btn-count` "Generate customer PDF (n)", and a PDF list on the page.
- [ ] T101 [US3] Create `PricingTool/Pages/Quotes/Pdf.cshtml.cs` (route `/Quotes/{id:int}/Pdfs/{pdfId:int}`, handler only). It returns the stored bytes unchanged with `Content-Disposition: attachment`, for the owner or an Admin only (others get 404). Each download is audited as `PdfDownloaded` (FR-051, FR-098, US7-4).

**Checkpoint**: User Story 3 works on its own (quickstart US3, steps 1–7).

---

## Phase 6: User Story 4 - Open the quote in Outlook (Priority: P2)

**Goal**: save a blank draft containing only the PDF to the agent's own Drafts folder, and open it.

**Independent Test**: generate a PDF and choose "Open in Outlook". In Development, an `.eml` downloads
(Eml mode). With Entra, check Drafts in classic and new Outlook (quickstart.md "US4").

### Tests for User Story 4 ⚠️

- [ ] T102 [P] [US4] Write `PricingTool.Tests/CustomerSafe/EmlDraftWriterTests.cs`, checking the output against [contracts/outlook-draft.md](contracts/outlook-draft.md):
  - it has `X-Unsent: 1` and `MIME-Version`;
  - it has **no** From, To, Subject or Date;
  - it uses CRLF line endings;
  - it is `multipart/mixed` with an empty text part and the PDF as `application/pdf`, base64-encoded in 76-character lines, with the right file name;
  - it has no other parts.
- [ ] T103 [P] [US4] Write `PricingTool.Tests/Outlook/GraphDraftRequestTests.cs`:
  - the request body contains only `attachments`, with one `#microsoft.graph.fileAttachment` holding the name, `application/pdf` and base64 content;
  - it has no `subject`, `body`, `toRecipients` or `from`;
  - a PDF over 3 MB is refused.

### Implementation for User Story 4

- [ ] T104 [US4] Add `PricingTool/Data/Entities/OutlookDraft.cs`, as in data-model.md (`Method` Graph/Eml, `GraphMessageId string(200)?`, `WebLink string(1000)?`, append-only). Add its configuration, then run `dotnet ef migrations add AddOutlookDraft --project PricingTool`.
- [ ] T105 [US4] Implement these in `PricingTool/Services/Outlook/`:
  - `IOutlookDraftService.cs` and `EmlWriter.cs` (hand-written MIME), so T102 passes;
  - `GraphDraftRequest.cs`, so T103 passes;
  - `EmlOutlookDraftService.cs`;
  - `GraphOutlookDraftService.cs`: `ITokenAcquisition.GetAccessTokenForUserAsync(["https://graph.microsoft.com/Mail.ReadWrite"])`, then `POST https://graph.microsoft.com/v1.0/me/messages` **only**, returning `id` and `webLink`. `MsalUiRequiredException` leads to incremental consent. It never sends or reads mail (FR-054 to FR-056).

  Register them by `Outlook:Mode`. Eml is forced when `Authentication:Mode=Development`.
- [ ] T106 [US4] Add `?handler=OpenInOutlook` to `PricingTool/Pages/Quotes/Details.cshtml.cs`:
  - Allowed only for the owner, and only when the PDF's `PricesFetchedOn` is today in UK time (FR-076). Otherwise it says the prices have expired.
  - **Graph**: JSON `{ ok, message, webLink }`.
  - **Eml**: an `.eml` download named after the PDF.
  - **On success**, in one transaction: the `OutlookDraft`, audit `OutlookDraftCreated`, the reporting event `OpenedInOutlook`, and `LastActivityUtc`.
  - **On failure**: audit `OutlookDraftFailed` and a reporting `Failed` event, with a clear message. Download PDF stays available (FR-057).
  - Show the draft history (who and when) on the page (US4-4).
- [ ] T107 [US4] Add the Outlook enhancements to `PricingTool/wwwroot/js/app.js`:
  - Graph: `CU.toast('Saved to your Outlook Drafts', { action: { label: 'Open', onClick: () => window.open(webLink, '_blank', 'noopener') } })`.
  - Eml: a toast explaining the download, including the new Outlook read-only note.
  - Errors: `CU.toast(message, { error: true })`.
  - With script off, the post redirects back with a `.notice`.

**Checkpoint**: User Story 4 works in Eml mode locally (quickstart US4, steps 1–2), and in Graph mode once IT provides the app registration.

---

## Phase 7: User Story 5 - Uplift limits (Priority: P2)

**Goal**: hard limits that are the same for everyone: a maximum per fuel, and a maximum per supplier
and fuel. Uplifts above the per-fuel maximum are refused, suppliers with a lower maximum are capped,
and limits are checked again before every PDF and Outlook draft. There is no approval process.

**Independent Test**: set the electricity maximums and a lower supplier maximum, try to price above
the per-fuel maximum, then price within it and check the capped supplier, the blank-limit supplier and
the re-check after a limit is lowered (quickstart.md "US5").

### Tests for User Story 5 ⚠️

- [ ] T108 [P] [US5] Write `PricingTool.Tests/Limits/LimitPolicyTests.cs`:
  - a unit-rate or standing-charge uplift above the per-fuel maximum is refused on its own, and the message names the maximum (FR-061);
  - the applied uplift is the lower of the requested uplift and the supplier's maximum for the fuel, and `IsCappedAtSupplierMax` is set when it is lowered (FR-062);
  - a blank per-fuel or supplier maximum means 0 (FR-060);
  - the re-check against lowered current limits names the options over a limit (FR-063).
- [ ] T109 [P] [US5] Write `PricingTool.Tests/Limits/DefaultUpliftValidationTests.cs`: saving a default uplift above that fuel's maximum is refused (US5-5, FR-081).

### Implementation for User Story 5

- [ ] T110 [US5] Implement `PricingTool/Services/Limits/LimitPolicy.cs` (pure, static) so T108 and T109 pass (research R13).
- [ ] T111 [US5] Wire the limits in:
  - `QuoteInputValidator.cs`: per-fuel maximum errors on the default and override uplift fields, before any POWWR call (FR-061, SC-009).
  - `PricingRequestBuilder.cs`: cap each plan's uplift at the supplier's maximum, and store the requested and applied uplifts plus `IsCappedAtSupplierMax` on `PriceOption` (FR-062).
  - `PdfEligibility.cs`: re-check the selected options against the limits in force now (FR-063).
  - The Outlook handler in `Details.cshtml.cs`: the same re-check, plus the placeholder check.
- [ ] T112 [US5] Show the agent-only `.tag.tone-amber` "Capped at supplier maximum (x)" in `PricingTool/Pages/Quotes/_Results.cshtml` and `ResultsViewModelBuilder.cs`. It's hidden in Customer view, and the uplift dialog shows the per-fuel maximum.
- [ ] T113 [US5] Create `PricingTool/Pages/Settings/Index.cshtml(.cs)` (policy `CanAdmin`, refused for others) with its first section, "Uplift limits", using `.card` sections and a `.drawer-wrap` > `.drawer` opened through `data-open`:
  - `?handler=UpliftLimits` sets the per-fuel maximums.
  - `?handler=SupplierMaximums` sets each supplier's four maximums.
  - `?handler=DefaultUplifts` is refused above the maximum.
  - Every change writes a `SettingVersion` and/or an `AuditEntry` (`LimitChanged` or `SettingChanged`) with the old and new values (FR-058, FR-059, FR-082).

**Checkpoint**: User Story 5 works on its own (quickstart US5, steps 1–6).

---

## Phase 8: User Story 6 - Multi-site quotes (Priority: P3)

**Goal**: one quote covering several sites for the same customer and fuel, added one at a time or
uploaded from CSV or `.xlsx`, with totals and a per-site breakdown.

**Independent Test**: upload the template with good and bad rows, fix the errors, price the quote,
and check the totals, per-site breakdown and PDF (quickstart.md "US6").

### Tests for User Story 6 ⚠️

- [ ] T114 [P] [US6] Write `PricingTool.Tests/Uploads/CsvSiteReaderTests.cs` against [contracts/spreadsheet-template.md](contracts/spreadsheet-template.md):
  - UTF-8 with and without a BOM; RFC 4180 quoting with embedded commas and line breaks;
  - case-insensitive headers, with an unknown column as an error;
  - both date formats;
  - `12,000` is an error.
- [ ] T115 [P] [US6] Write `PricingTool.Tests/Uploads/XlsxSiteReaderTests.cs` with workbooks built in memory using DocumentFormat.OpenXml: shared and inline strings, numbers, numeric date cells turned into dates (`FromOADate`), and meter numbers kept as text.
- [ ] T116 [P] [US6] Write `PricingTool.Tests/Uploads/SiteUploadValidatorTests.cs`:
  - every row is validated with `QuoteInputValidator`'s site rules, and errors are listed per row and column with the value given;
  - a meter number repeated in the file or already on the quote is an error, and so is a row for the other fuel;
  - the example row is rejected;
  - files over 2 MB or 200 rows are refused;
  - nothing is accepted while any row has an error (FR-070, SC-008).
- [ ] T117 [P] [US6] Add multi-site cases to `PricingTool.Tests/Pricing/MultiSiteCombinerTests.cs` and `PricingTool.Tests/Quotes/PdfEligibilityTests.cs`:
  - a supplier missing some sites is partial, and the missing sites carry POWWR's reasons;
  - a retried failed site joins the same run;
  - the PDF is blocked while a site has failed technically;
  - the microbusiness test uses the combined consumption (FR-073, US6-5).

### Implementation for User Story 6

- [ ] T118 [US6] Add `PricingTool/Data/Entities/PendingUpload.cs` and `PendingUploadRow.cs`, as in data-model.md (`Format` Csv/Xlsx, `RowNumber`, JSON `Values` and `Errors`, cascade delete). Add their configuration, then run `dotnet ef migrations add AddPendingUpload --project PricingTool`.
- [ ] T119 [US6] Implement these in `PricingTool/Services/Uploads/`, so T114–T116 pass:
  - `CsvSiteReader.cs`, using `Microsoft.VisualBasic.FileIO.TextFieldParser`;
  - `XlsxSiteReader.cs`, using DocumentFormat.OpenXml;
  - `SiteUploadValidator.cs`;
  - `TemplateWriter.cs`: CSV and `.xlsx` per fuel, with the header row, one made-up example row, Text-formatted meter columns, and a hidden "Lists" sheet feeding data validation.
- [ ] T120 [US6] Create `PricingTool/Pages/Quotes/Upload.cshtml(.cs)` (route `/Quotes/{id:int}/Sites/Upload`, owner only), with the upload form and template links:
  - `POST` reads and validates the file. Errors go into the per-row table (Row · Column · Value · Message) and are kept as a `PendingUpload`.
  - `?handler=FixRow`, `?handler=RemoveRow` and `?handler=AddValidRows` (only when no row has errors) create the `Site`s and delete the pending upload, audited as `SitesUploaded` with row and error counts.

  Also create `PricingTool/Pages/Templates/MultiSite.cshtml.cs` (route `/Templates/MultiSite`, `CanQuote`) to serve the templates (FR-069).
- [ ] T121 [US6] On `PricingTool/Pages/Quotes/Edit.cshtml(.cs)`, let the agent add and remove sites one at a time with the same postcode picker. Each site has its own meter, consumption, current supplier, rates and an optional start date. Terms, payment, uplifts and VAT/CCL apply to the whole quote. Audited as `SiteAdded` and `SiteRemoved` (FR-068, FR-069, FR-071).
- [ ] T122 [US6] In `_Results.cshtml` and `ResultsViewModelBuilder.cs`, show for each supplier and term the total across all sites and an expandable per-site breakdown. Partial totals are marked, listing the missing sites and reasons (FR-073). In `Details.cshtml.cs`, add `?handler=RetryFailed`, which re-sends only the failed sites into the same run (a new attempt) and re-runs the engine (spec edge case).
- [ ] T123 [US6] In `CustomerQuoteDocument.razor` and `CustomerQuoteModelBuilder.cs`, show the totals first for multi-site quotes, then the per-site breakdown, then "Sites not covered by {supplier}" for partial options (FR-074).
- [ ] T124 [P] [US6] Create `PricingTool.Tests/TestData/sites-50-electricity.csv` and `sites-50-electricity.xlsx`. They hold 50 made-up sites with MPAN cores whose check digits are valid (generated by a small test helper in `PricingTool.Tests/Support/MpanFactory.cs`), with three bad rows: a wrong check digit, an MPAN repeated within the file, and an MPRN. Copy both files to the test output.

**Checkpoint**: User Story 6 works on its own (quickstart US6, steps 1–6).

---

## Phase 9: User Story 7 - Quote history (Priority: P3)

**Goal**: find past quotes, reopen them, see price changes, re-price, re-download PDFs, open a PDF in
Outlook again while its prices are valid, see the full history, and delete quotes past the retention
period.

**Independent Test**: create quotes, find one by customer name and by meter number, reopen it,
re-price it, re-download its PDF, and confirm another agent can't see it (quickstart.md "US7").

### Tests for User Story 7 ⚠️

- [ ] T125 [P] [US7] Write `PricingTool.Tests/Quotes/QuoteSearchCriteriaTests.cs`: MPAN and MPRN input normalised to digits, postcode normalised, and the date range checked. Agents get their own quotes only; Admins get all (FR-003, FR-075).
- [ ] T126 [P] [US7] Write `PricingTool.Tests/Retention/RetentionPlanTests.cs`: the cutoff is the UK date minus `Retention.Years`, compared with `LastActivityUtc`; the next run is the next `Retention:RunAtUkTime` in UK time across BST changes; batches hold 50 quotes (FR-080).

### Implementation for User Story 7

- [ ] T127 [US7] Create `PricingTool/Pages/Quotes/Index.cshtml(.cs)` (route `/Quotes`):
  - search by customer name or reference, MPAN or MPRN, postcode, and from/to dates, with paging;
  - a `.table` in a `.card--flush`, with a search `.field-inline` and `.pagination`;
  - own quotes only, unless Admin.

  Implement `PricingTool/Services/Quotes/QuoteSearchCriteria.cs` so T125 passes.
- [ ] T128 [US7] On `PricingTool/Pages/Quotes/Details.cshtml(.cs)`:
  - when the page opens, run `PriceChangeCheck` for information, with a quiet notice if it fails (FR-077);
  - list every PDF with its validity date and a re-download link (US7-4);
  - allow Open in Outlook again only on the day the PDF's prices were fetched (FR-076);
  - add a `_History.cshtml` partial: a timeline (`.timeline-entry` / `.timeline-event`) of the quote's `AuditEntry` rows, showing every request, result, uplift, microbusiness declaration change, PDF, PDF download and draft, with who and when (FR-079, US7-6).
- [ ] T129 [US7] Implement `PricingTool/Services/Retention/RetentionWorker.cs` (`BackgroundService`) and `RetentionPlan.cs`:
  - It runs daily at `Retention:RunAtUkTime`, in batches of 50 quotes, each batch in its own transaction.
  - It deletes PDF files through `IFileStore` and exported reporting files through `IReportingExportStore` first.
  - Then it deletes with `ExecuteDeleteAsync`: sites, runs, run sites, no-quotes, options, option sites, PDFs, drafts, pending uploads, overrides, the quote's audit entries, its reporting records, their supplier rows and outbox entries, and the quote.
  - It **keeps** the settings change log.
  - It writes one system `AuditEntry` (`RetentionRun`) with counts only (FR-080, US7-7).

  Register it in `Program.cs`.

**Checkpoint**: User Story 7 works on its own (quickstart US7, steps 1–9).

---

## Phase 10: User Story 8 - Admin settings (Priority: P3)

**Goal**: Admins manage defaults, suppliers, tax rates, thresholds, retention, customer-facing wording
(with compliance approval) and the terms address. Every change is logged with the old and new values.

**Independent Test**: as Ada Admin, change a default uplift, a CCL rate with a future effective
date and the commission disclosure (marking it approved with an approver's name). Check that new
quotes and PDFs use them, earlier PDFs are unchanged, and the change log shows each change
(quickstart.md "US8").

### Tests for User Story 8 ⚠️

- [ ] T130 [P] [US8] Write `PricingTool.Tests/Settings/SettingsValidationTests.cs`:
  - marking wording approved needs a non-empty `ComplianceApprover`, and clears `IsPlaceholder`;
  - the terms URL must be an absolute https URL;
  - only rates whose effective date is in the future can be deleted;
  - thresholds must be positive, and large-gas limits may be blank;
  - `UpfrontPercent` is 0–100;
  - a supplier alias needs at least one of `PowwrId`, `Mpid` or `LookupName` (FR-081, FR-082).

### Implementation for User Story 8

- [ ] T131 [US8] Add these sections to `PricingTool/Pages/Settings/Index.cshtml(.cs)` (created in T113, or create it here if US5 isn't built yet), each in a `.drawer` with its own POST handler, as in [contracts/ui-routes.md](contracts/ui-routes.md) "Settings":
  - `Supplier` (pricing name, active, fuels, `UpfrontPercent`);
  - `SupplierAlias` and `DeleteSupplierAlias`, which call `SupplierMatcher.Invalidate()`;
  - `CommissionBasis`, `DefaultPayment` and `Rate` (with `DeleteFutureRate`);
  - `Thresholds` (microbusiness and the three optional large-gas limits) and `Retention`;
  - `Wording` (text, plus "approved by compliance" with the approver's name), with a preview showing the placeholder frame and any unknown `{Placeholder}`;
  - `TermsUrl`.

  Every change is a new `SettingVersion` or `EffectiveRate`, or an audited supplier change, with the old and new values (FR-081 to FR-083). It never changes stored PDFs.
- [ ] T132 [US8] Create `PricingTool/Pages/Settings/ChangeLog.cshtml(.cs)` (`CanAdmin`): a `.table` of settings and admin `AuditEntry` rows (`SettingChanged`, `WordingApproved`, `RateAdded`, `RateDeleted`, `SupplierChanged`, `LimitChanged`, `ReportExported`) showing who, when, the old and new values, and the approver for wording (FR-082, US8-6).

**Checkpoint**: User Story 8 works on its own (quickstart US8, steps 1–7).

---

## Phase 11: User Story 9 - Management reporting (Priority: P3)

**Goal**: every quote event reaches the reporting store in the background. Admins filter, total and
export the records on the Reports page, and are warned when exports keep failing.

**Independent Test**: create, price, generate and open several quotes. Check the Reports page totals,
filters and CSV export, check the JSON files under `App_Data/reporting/quotes/yyyy/MM/dd/`, and
simulate an outage (quickstart.md "US9").

### Tests for User Story 9 ⚠️

- [ ] T133 [P] [US9] Write `PricingTool.Tests/Reporting/CsvWriterTests.cs`: RFC 4180 quoting of commas, quotes and line breaks; cells starting with `=`, `+`, `-` or `@` are prefixed with `'`; the columns are exactly those in [contracts/reporting-record.md](contracts/reporting-record.md) "Reports page columns", with no contact details (FR-085).
- [ ] T134 [P] [US9] Write `PricingTool.Tests/Reporting/OutboxBackoffTests.cs` (exponential backoff capped at 5 minutes; retried until it succeeds; SC-007) and `PricingTool.Tests/Reporting/ExportFailureNoticeTests.cs` (the notice shows when the oldest unexported entry is more than an hour old; FR-089).
- [ ] T135 [P] [US9] Write `PricingTool.Tests/Reporting/EstimatedCommissionTests.cs`: for each quote with a PDF, take the recommended option's whole-contract commission from its **latest** `PdfGenerated` record, and sum over the filtered quotes (FR-085).

### Implementation for User Story 9

- [ ] T136 [US9] Implement `PricingTool/Services/Reporting/ReportingOutboxWorker.cs` (`BackgroundService`):
  - It polls every `Reporting:Outbox:PollSeconds`.
  - It takes up to 100 entries due, oldest first, and writes each payload through `IReportingExportStore` at `ReportingExportPath`.
  - It then sets `ExportedUtc` and `ExportPath`, or increments `Attempts`, sets `NextAttemptUtc` with the backoff from T134, and stores a safe `LastError`.
  - It never blocks a request.

  Register it in `Program.cs` (FR-087, FR-089).
- [ ] T137 [US9] Implement `PricingTool/Services/Reporting/ReportsQuery.cs` and `CsvWriter.cs`, so T133–T135 pass. The query filters by agent, date range, fuel, supplier (`ReportingRecordSupplier`), region (`Regions` LIKE) and quote type, and returns totals: quotes per agent, PDFs, opened in Outlook, and estimated commission.
- [ ] T138 [US9] Create `PricingTool/Pages/Reports/Index.cshtml(.cs)` (`CanAdmin`; Agents refused even by URL):
  - filters as `.select-trigger[data-menu]` with `.menu` in the page, never swapped;
  - a `.kpi-strip` of `.kpi-mini` totals;
  - a paged `.table` of records;
  - a `.btn-outline` "Export CSV" (`?handler=Csv`), which returns the filtered CSV and is audited as `ReportExported` with the filters and row count (FR-086);
  - a `.notice.tone-amber` with the number of records waiting, when exports have been failing for over an hour (US9-5).

**Checkpoint**: all user stories work on their own (quickstart US9, steps 1–4).

---

## Phase 12: Polish & Cross-Cutting Concerns

**Purpose**: checks and documentation across all stories.

- [ ] T139 [P] Write `PricingTool.Tests/Design/ColourTokenScanTests.cs`. It scans `PricingTool/wwwroot/css/app.css`, `PricingTool/wwwroot/css/pdf.css`, every `PricingTool/Pages/**/*.cshtml` and `PricingTool/Services/Pdf/*.razor`, and fails on any hex colour, `rgb(`, `rgba(`, `hsl(` or named CSS colour. `wwwroot/cu/` is excluded (constitution Principle IV).
- [ ] T140 [P] Write `PricingTool.Tests/Security/SecretHygieneTests.cs`:
  - `appsettings*.json` contain no value for `LicenceCode`, `ClientSecret` or a password connection string;
  - masked request JSON never contains the configured licence code;
  - `AuditWriter` and `ReportingRecordBuilder` output never contain a test secret value (FR-096).
- [ ] T141 Accessibility and responsive pass on every page at 390, 1024 and 1280 px wide:
  - the sidebar becomes a rail at 1024 px and slides out at 390 px;
  - tables scroll inside their cards;
  - everything works by keyboard with the kit's focus ring;
  - navy text, with teal only for links and active controls;
  - states are never shown by colour alone.

  Take screenshots with the Playwright MCP into `.playwright-mcp/` (git-ignored), and fix issues in `PricingTool/wwwroot/css/app.css` and the pages (FR-090 to FR-092).
- [ ] T142 [P] Finish `README.md`:
  - prerequisites and the one-time setup;
  - user secrets (`dotnet user-secrets set …` for the keys in contracts/configuration.md);
  - database setup;
  - running with the mocks, SwaggerHub's mock server or live POWWR;
  - the Development test sign-in people;
  - the spreadsheet template columns, linked to contracts/spreadsheet-template.md;
  - the reporting record format, linked to contracts/reporting-record.md;
  - the list of kit gaps from plan.md for the design kit owner.
- [ ] T143 Review logging across `PricingTool/` against FR-096 to FR-098 and research R25. Every log entry must be structured with the quote reference in its scope, never log a token, licence code, client secret or customer contact detail, and log sign-ins and refused access. Fix anything that doesn't.
- [ ] T144 Run every scenario in [quickstart.md](quickstart.md) with the mock adapters and Development sign-in, then `dotnet build` (no errors) and `dotnet test` (all passing). Record anything that fails as a new task, and mark completed tasks `[X]` in this file.

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (Phase 1)**: no dependencies.
- **Foundational (Phase 2)**: depends on Setup. **It blocks every user story.** Within it:
  - T010–T018 (domain and validation) and T019–T022 (entities) can run alongside each other.
  - T023–T026 (DbContext, guard, seed data, migration) need the entities.
  - T027–T032 (auth) need T023 for `StaffMember`, and T034 for the standalone layout used by T029.
  - T041–T052 (adapters) need T011 and T016.
  - T053–T061 (engine) need T011.
  - T062–T064 (reporting) need T022 and T059.
- **User stories (Phases 3–11)**: all depend on Foundational.
- **Polish (Phase 12)**: depends on the stories you're delivering.

### User story dependencies

| Story | Depends on | Notes |
|---|---|---|
| US1 (P1) | Foundational only | The MVP |
| US2 (P2) | US1 | Reuses US1's form, picker, pricing service and results |
| US3 (P2) | US1 | Needs a priced quote with selected options |
| US4 (P2) | US3 | Needs a stored PDF |
| US5 (P2) | US1 | Hooks into US3's and US4's checks when those exist (T111) |
| US6 (P3) | US1 | T123 extends US3's PDF |
| US7 (P3) | US1 | Re-download and Outlook parts use US3 and US4 when present |
| US8 (P3) | Foundational | Extends the Settings page from US5 if it exists (T131) |
| US9 (P3) | Foundational | Its reporting recorder runs from US1 on; US9 adds the worker, Reports and CSV |

### Within each story

1. Tests first; make sure they fail.
2. Entities and migrations.
3. Services.
4. Pages and handlers.
5. `app.js` enhancements.
6. Run the story's quickstart checks.

### Parallel opportunities

- **Setup**: T003–T006, T008 and T009.
- **Foundational**:
  - T010–T015, T017 (tests) and T019–T022 (entities) together;
  - T027, T031, T033 and T035;
  - T038 and T040;
  - T041–T044 and T048–T049;
  - T053–T058 (engine tests);
  - T062 and T064.
- **US1**: T065–T068. **US2**: T082–T083. **US3**: T087–T092, then T094 and T096. **US4**: T102–T103.
  **US5**: T108–T109. **US6**: T114–T117 and T124. **US7**: T125–T126. **US9**: T133–T135.
- **Polish**: T139, T140 and T142.
- Once US1 is done, US3, US5, US6, US7, US8 and US9 can be built in parallel by different people.
  US2 also needs only US1, and US4 needs US3.

---

## Parallel Examples

```text
# Foundational: the pricing engine tests, together
Task: "T053 RoundingTests in PricingTool.Tests/Pricing/RoundingTests.cs"
Task: "T054 VatCclCalculatorTests in PricingTool.Tests/Pricing/VatCclCalculatorTests.cs"
Task: "T055 CommissionCalculatorTests in PricingTool.Tests/Pricing/CommissionCalculatorTests.cs"
Task: "T056 SavingCalculatorTests in PricingTool.Tests/Pricing/SavingCalculatorTests.cs"
Task: "T057 MicrobusinessTestTests in PricingTool.Tests/Pricing/MicrobusinessTestTests.cs"
Task: "T058 MultiSiteCombiner, cross-check and base rate tests in PricingTool.Tests/Pricing/"

# US1: tests together
Task: "T065 PricingRequestBuilderTests"   Task: "T066 RateStructureTests"
Task: "T067 CustomerViewTests"            Task: "T068 ResultsViewModelBuilderTests"

# US3: tests and independent services together
Task: "T087 CustomerModelShapeTests"  Task: "T088 CustomerPdfContentTests"  Task: "T089 PdfFileNameTests"
Task: "T090 PriceValidity/PriceChangeCheck tests"  Task: "T091 PdfEligibilityTests"  Task: "T092 LocalFileStoreTests"
Then: "T094 LocalFileStore" alongside "T096 IStaffDirectory"

# US6: readers and test data together
Task: "T114 CsvSiteReaderTests"  Task: "T115 XlsxSiteReaderTests"  Task: "T116 SiteUploadValidatorTests"  Task: "T124 50-site test data"

# US9: tests together
Task: "T133 CsvWriterTests"  Task: "T134 Outbox backoff and failure notice tests"  Task: "T135 EstimatedCommissionTests"
```

---

## Implementation Strategy

### MVP first (User Story 1 only)

1. Phase 1: Setup.
2. Phase 2: Foundational. This is critical: sign-in, roles, app shell, database, validation, adapters
   and the pricing engine with its tests.
3. Phase 3: User Story 1.
4. **Stop and validate**: run quickstart US1 and `dotnet test`. Commit and push on the feature branch.
   This is the MVP: agents can price and compare electricity.

### Incremental delivery (one phase at a time, as CLAUDE.md requires)

After each phase, stop, tell the owner how to run and test it, commit on `001-pricing-tool`, and
push. The suggested order:

1. Setup + Foundational.
2. US1, the MVP.
3. US2 (gas).
4. US3 (PDF), then US4 (Outlook). Together they make a complete customer quote.
5. US5 (uplift limits). This must be in place before quotes go to customers in volume.
6. US6, US7, US8 and US9, in any order.
7. Polish, then `/speckit-converge`, then the pull request into `main`.

### Notes

- [P] tasks touch different files and have no unfinished dependencies.
- [USn] labels map each task to its user story in spec.md, for traceability.
- When the code and the spec disagree, stop and ask (CLAUDE.md). Don't change either quietly.
- Mark tasks `[X]` as they're finished.
- No Azure resources, Bicep, pipelines or deployment work are part of this feature.
