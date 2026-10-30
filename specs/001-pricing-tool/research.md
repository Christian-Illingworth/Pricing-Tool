# Research and design decisions: CU Pricing Tool

**Feature**: `001-pricing-tool` | **Date**: 2026-10-30 | **Plan**: [plan.md](plan.md)

Each decision below resolves an unknown from the plan's Technical Context. They draw on the spec, the
constitution, the POWWR definitions in `docs/powwr/`, the design kit and the local copy of Triage. Each
has the format: **Decision**, **Rationale**, **Alternatives considered**. Items that only POWWR,
finance, compliance or IT can settle are listed under "Open items" at the end, together with what the
tool does until they answer.

---

## R1. Platform, solution layout and Triage conventions

**Decision**: .NET 10 (the current LTS release, SDK 10.0.301 installed), ASP.NET Core Razor Pages. The
solution follows Triage's layout: one web project and one xUnit test project at the repository root,
in an `.slnx` solution.

```text
PricingTool.slnx
PricingTool/          web app (Razor Pages, EF Core, services, adapters, wwwroot)
PricingTool.Tests/    xUnit tests
```

From Triage we also keep: `net10.0`, `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`,
`InternalsVisibleTo` for the test project, a `NuGet.config` pinned to nuget.org, a `_Head.cshtml` partial
for the kit's `<head>`, a fixed local port in `launchSettings.json`, and decision logic in small pure
classes that the tests call directly, with no HTTP or database. As in Triage's `IdentityService`, there is
a sign-in shortcut that works only in Development.

Triage differs in four places, and in each one the constitution's named stack wins:

| Triage | Pricing Tool | Why |
|---|---|---|
| MVC controllers and views | Razor Pages | Constitution: "Pages MUST be server-rendered with ASP.NET Core Razor Pages" |
| App Service Easy Auth headers | Microsoft.Identity.Web (OpenID Connect) | Constitution stack; Easy Auth doesn't exist locally, and Graph needs a delegated token |
| `.env` file for settings and secrets | `appsettings*.json` + .NET user secrets | Constitution Principle I |
| JSON files on disk, no database | EF Core + SQL Server | Constitution stack; quotes need relational history |

**Rationale**: Principle VIII asks for Triage's structure and conventions, and the constitution names the
stack. A single web project matches Triage and keeps the build simple. EF migrations then live in the web
project (`dotnet ef database update --project PricingTool`).

**Alternatives considered**: splitting the app into Web, Core and Data projects. That would be cleaner on
paper, but it isn't how Triage is built, and for a small team it adds friction with nothing to show for
it. Folders inside the one project (`Services/Pricing`, `Powwr/`, `Data/`) give the same separation.

## R2. Sign-in, roles and the "Pricing Tool Users" group

**Decision**:
- **Entra mode** (`Authentication:Mode = Entra`): Microsoft.Identity.Web, OpenID Connect, single-tenant
  (our tenant ID only), cookie session.
  - App roles `Agent` and `Admin` are defined in the app registration and arrive in the `roles` claim
    (FR-002). There is no Manager role.
  - **Session lifetime**: the sign-in cookie has an absolute lifetime of `Authentication:SessionHours`
    (8), with no sliding renewal, so group or role changes apply at the next sign-in (FR-094). Sign-ins
    and refused access are written to the app's logs (FR-098).
  - The group restriction is enforced twice:
    1. **IT, in Entra:** the enterprise app has "Assignment required" on, and only "Pricing Tool Users"
       (or role assignments within it) is assigned.
    2. **The app, on the server:** the fallback authorisation policy requires the configured group's
       object ID (`AzureAd:AllowedGroupId`) in the `groups` claim, plus at least one app role.
  - The registration emits only "groups assigned to the application", so the `groups` claim can't
    overflow.
- **Development mode** (`Authentication:Mode = Development`) is the test sign-in until IT provides an app
  registration (FR-095). It is a cookie scheme, and `/Dev/SignIn` lists made-up staff with fixed
  roles: two agents, an admin, a person with both roles, and a user with no role. It is registered only when
  `IHostEnvironment.IsDevelopment()` is true, and startup throws if the mode is set to `Development` in
  any other environment. Outside Development, `/Dev/SignIn` returns 404. A test checks both.
- **Authorisation policies**, enforced on the server on every page and handler:

  | Policy | Who |
  |---|---|
  | `CanQuote` | Agent |
  | `CanViewAllQuotes` | Admin (read-only) |
  | `CanAdmin` | Admin |

  The fallback policy is "signed in, in the group, and holding any app role". Anyone else is sent to
  `/Account/NoAccess` and sees no data.
- **Quote ownership**: only a quote's owner can change it (re-price, select, PDF, Outlook). Admins can
  view other people's quotes but not change them, and the Admin role alone can't quote (FR-002,
  FR-003). An agent asking for another agent's quote gets 404.
- **Staff record**: on each sign-in, a `StaffMember` row is created or updated from the token's `oid`,
  `name` and `preferred_username`. It also holds the person's "Customer view" choice (FR-099).

**Rationale**: two independent gates satisfy Principle I even if the Entra assignment is misconfigured.
Emitting only application groups is Microsoft's documented way to avoid group overage, so no extra Graph
permission is needed.

**Alternatives considered**: checking groups through Graph `GroupMember.Read.All`, which needs an extra
admin-consented permission. Relying on the role claim alone leaves no in-app check of the group.

## R3. "Open in Outlook": Graph draft, with an .eml fallback

**Decision**:
- **Graph mode** (`Outlook:Mode = Graph`, the default with Entra): the app gets a delegated token with
  `ITokenAcquisition` for `Mail.ReadWrite`, and makes one call: `POST https://graph.microsoft.com/v1.0/me/messages`.
  - The body holds only `attachments` (one `fileAttachment` containing the PDF), with no subject, body
    or recipients. See [contracts/outlook-draft.md](contracts/outlook-draft.md).
  - The response's `id` and `webLink` are stored on the `OutlookDraft` record. The page shows
    `CU.toast("Saved to your Outlook Drafts", { action: { label: "Open", … } })`, and the action opens
    `webLink` (Outlook on the web) in a new tab.
  - Because the draft is in the server-side mailbox, it also appears in Drafts in classic and new
    Outlook.
  - Inline attachments are limited to 3 MB, and a customer PDF is about 200 KB. The app refuses anything
    over 3 MB with a clear message instead of using an upload session.
  - Consent: `Mail.ReadWrite` is requested incrementally, so a signed-in agent is asked only the first
    time.
- **Eml mode** (`Outlook:Mode = Eml`) is the fallback, used only if IT won't grant `Mail.ReadWrite`. It is
  also the mode used with the Development test sign-in, which has no Graph token.
  - The app returns a hand-built MIME message: `X-Unsent: 1`, no From, To or Subject, an empty text part,
    and the PDF as a base64 attachment. It is served as `{same base name}.eml`.
  - Classic Outlook opens it as an editable draft. New Outlook may open it read-only, so the toast tells
    those agents to attach the downloaded PDF themselves.
  - The record is stored with `Method = Eml`.
- **Failures**: Graph errors (401/403 consent, 429, 5xx, network) are shown as a `.notice.tone-coral`
  plus an error toast, and the Download PDF button stays available (FR-057). The token cache is
  `AddDistributedTokenCaches()` over the in-memory distributed cache locally, and can switch to SQL Server
  or Redis distributed cache in Azure through configuration only.
- **Spec note**: Eml mode meets FR-054 and FR-055 only partly, because it doesn't save to Drafts and may
  be read-only in new Outlook. It stays off unless IT refuses the permission. If it is ever switched on
  in a deployed environment, FR-054 and FR-055 need a spec amendment first.

**Rationale**: one documented Graph call with the narrowest delegated permission meets "never send or
read email". MIME for one attachment is about 40 lines, so it needs no MimeKit.

**Alternatives considered**: the Microsoft.Graph SDK (a large dependency for one call), and `mailto:`
links (which can't attach files).

## R4. POWWR UK Pricing API client

**Decision**:
- **Interface**: `IPowwrPricingClient` has `GetElectricPricesAsync`, `GetGasPricesAsync`,
  `GetRateStructureAsync` (`/prompts`), `GetLatestPriceChangesAsync` (`/pricechange`) and `PingAsync`.
  It takes and returns domain types, never DTOs.
- **Implementations**, selected by `Powwr:Pricing:Mode`:
  - `Mock` (the default): `MockPowwrPricingClient` generates realistic, deterministic prices from the
    meter and consumption.
  - `SwaggerHub`: `PowwrPricingHttpClient` against `https://virtserver.swaggerhub.com/POWWR/UkPricingAPI/1.2.0`,
    with a placeholder licence code.
  - `Live`: the same client against `https://udcoreapi.co.uk/Service.svc/web`, with the licence code from
    user secrets.
- **DTOs** mirror the definition exactly. Property names are PascalCase, the request wrappers are
  `quoteDetails`, `promptsSearch` and `priceChangeRequest`, and the response wrappers are
  `GetElectricRatesResult`, `GetGasRatesResult`, `GetNumberOfPromptsResult` and `GetLatestPriceChangeResult`.
  `System.Text.Json` reads `Raw*` numbers straight into `decimal`, never through `double`.
- **Mapping**: `PowwrPricingMapper` is a pure static class, unit-tested both ways, and implements
  [contracts/powwr-uk-pricing-mapping.md](contracts/powwr-uk-pricing-mapping.md):
  - `SecurityDetails.mascaradeuser` is the agent's display name (Q13 open).
  - `QuoteReference` is our quote reference.
  - `Settings` is `[{ key: "ShowNoQuoteReason", value: "true" }]`.
  - Quote type maps to `Renewal`, `COT` and `CotDate`, plus `OutOfContract`, `PaymentMethod` and
    `BusinessType`.
  - `QuoteDefinitions` has one entry per included supplier from the admin supplier list, using its
    pricing name, with one `Plan` per requested term. Each plan carries that supplier and term's
    uplifts, capped at the supplier's maximum for the fuel (R13, FR-062). `PlanType` is left out until Q4 is answered, so every plan type returns, and
    "no standing charge" filters on the returned standing charge.
  - Quote-level `Uplift` and `StandingChargeUplift` carry the quote's default uplifts, as a fallback.
  - Profile class and MTC are sent as integers (R5). `ContractRenewalDate` carries the new contract's
    start date (Q9 open, see the open items).
- **Responses are external input** (Principle III). The mapper rejects or flags anything it can't trust,
  and logs it with the quote reference:
  - a `Rate` with a non-numeric `Term`;
  - a missing `RawAnnualPrice` on a priced rate;
  - negative raw figures;
  - a rate string that doesn't parse as a decimal (invariant culture).

  A `Rate` with a `NoQuoteReason` becomes a `NoQuoteResult`. `QuoteResult.Error` is checked on every
  response, because the API defines only a `default` response, so HTTP status alone can't be trusted.
- **Display and calculation**: rate strings are stored and shown exactly as returned. Calculations use
  the `Raw*` decimals (R7).
- **Stored request copy**: a copy of each request is stored with `LicenceCode` replaced by `***`, to
  satisfy Principle V without storing a secret.

**Rationale**: one HTTP client with three base URLs covers mock, SwaggerHub and live. Keeping DTOs
behind the adapter keeps POWWR's quirks out of the engine and pages.

**Alternatives considered**: NSwag or Kiota client generation. This would add a build-time tool, and the
generated code would still need hand mapping. The two definitions together have about 40 types.

## R5. POWWR Meter Lookup client

**Decision**:
- **Interface**: `IMeterLookupClient` has `SearchAddressesAsync(postcode)` and
  `SearchMetersAsync(meterAddressId | mpan | mprn)`.
- **Implementations**, selected by `Powwr:MeterLookup:Mode`: `Mock` (the default), `SwaggerHub`,
  `Sandbox` and `Live`.
- **Auth**: OAuth2 client credentials against `TokenUrl`, with scopes `udgs-lookupservices-meter-api`
  and `brokerage:{OrganisationPowwrId}`. The token is cached until shortly before it expires; a
  `SemaphoreSlim` stops two requests refreshing it at once. SwaggerHub mode sends no token.
- **`callerIdentification`**: `organisationPowwrId` comes from config and is validated against
  `^[A-Z0-9]{1,8}-\d{3}-[SB]$` at startup when not in Mock mode. `applicationId` is `"CU Pricing Tool"`,
  and `userId` is the agent's Entra object ID.
- **JSON** is read case-insensitively, because POWWR's own example mixes `postCode`, `Postcode` and
  `AddressLine1` with `addressLine1`.
- **Errors**: a non-2xx response with `application/problem+json` is parsed as RFC 7807. Its `title`,
  `detail` and `extensions.errors[].description` are logged without the token, and the agent sees "The
  meter lookup couldn't find that. You can enter the details yourself." The lookup never blocks quoting
  (SC-005). A timeout or 5xx gives the same message with "unavailable".
- **Postcodes**: `Postcode.Normalise` uppercases, removes all spaces, validates the UK pattern
  `^[A-Z]{1,2}[0-9][A-Z0-9]?[0-9][A-Z]{2}$`, then inserts one space before the last three characters. It
  runs on the server before any call (FR-008).
- **Redacted MPANs**: address results only show meter counts and redacted numbers. Picking an address
  always calls `meterSearch` with its `meterAddressId`, and the full MPAN comes from that response.
- **Type conversion**: `profileClass` and `meterTimeswitchClass` arrive as strings like `"03"` and
  `"801"`, and are parsed to integers for the Pricing API. A value that isn't numeric is treated as
  missing and flagged to the agent.
- **Supplier mapping**: the lookup's `currentSupplier.powwrId`, then `mpid` (or `shortCode`), then
  `name`, are matched against the admin supplier list's aliases (`SupplierAlias`). This gives the pricing
  name, for example `BRITISHG-001-S` gives "British Gas". If nothing matches, the current supplier
  is shown as returned with an amber notice, "Not in the supplier list. Ask an admin to map it", and
  the agent can pick the supplier by hand.
- **Gas**: `additionalInformation.amrIndicator` = `"Y"` suggests smart meter = yes, and anything else
  suggests no. The agent can change it. `isIGT` sets the IGT flag (FR-046). `incomingSupplier` present
  means a switch is in progress, which shows a warning (FR-012).
- **Electricity warnings**: `isDomesticConsumer = true` (the agent must confirm), `energisationStatus = "D"`
  (de-energised), and `greenDealStatus = "Active"`. Electricity has no documented "switch in progress"
  field (see open item P-16).
- **Region**: shown from the MPAN core (R16). `gspId` is stored as well, for reporting.

**Rationale**: these follow POWWR's documented flow and the "Things to watch" in `docs/powwr/README.md`.

**Alternatives considered**: matching suppliers by name only. That's fragile, as POWWR's own example
shows: "British Gas Business" in the lookup is "British Gas" in pricing.

## R6. Calling POWWR: concurrency, retries, timeouts

**Decision**:
- **Concurrency**: multi-site pricing sends one request per site in parallel, bounded by a `SemaphoreSlim`
  of `Powwr:Pricing:MaxConcurrentRequests`. This defaults to 4 until POWWR states its limits (Q1).
- **Timeouts**: each request has a timeout of `Powwr:Pricing:TimeoutSeconds` (default 45).
- **Retries**: a small `PowwrRetryHandler` (`DelegatingHandler`) retries **only** HTTP 429 and 503,
  honouring `Retry-After` (capped at 10 s) and making at most 2 retries. It never retries a timeout or
  another 5xx, because we don't yet know whether live calls are charged (Q1). The agent retries instead.
- **Failures** are recorded per site on the pricing run (`PricingRunSite.Status = Failed`, with the
  message). Everything entered stays saved, because the quote is saved as a Draft **before** POWWR is
  called. "Retry failed sites" re-sends only the failed sites, into the same run (spec edge case).

**Rationale**: this follows Triage's convention of explicit, testable rate handling in our own code
(`FreshdeskService.RequestAsync` lanes), and needs no extra package.

**Alternatives considered**: `Microsoft.Extensions.Http.Resilience`. It is a fine package, but it adds a
dependency, and its default policy retries timeouts, which we don't want yet.

## R7. Pricing engine: one set of numbers, one rounding rule

**Decision**:
- **Structure**: `PricingEngine` is a stateless service in `Services/Pricing/`, built from pure static
  calculators: `VatCclCalculator`, `CommissionCalculator`, `SavingCalculator`, `MicrobusinessTest`,
  `LowUseTest`, `MultiSiteCombiner`, `AnnualCostCrossCheck` and `Rounding`.
- **Inputs**: the mapped POWWR rates per site, plus a snapshot of the settings in effect: VAT and CCL
  rates for each site's start date, thresholds, the commission basis, and supplier payment models.
- **Output**: `PricingResult`, with per-site and per-option figures at full `decimal` precision.
- **When it runs**: once per pricing run (and again when failed sites are retried). Its output is
  **persisted** on `PricingRunSite`, `PriceOptionSite` and `PriceOption`. The screen, the PDF model
  builder and the reporting record builder all read those stored figures, so they can't disagree
  (SC-004), and later settings changes can't alter a past quote (FR-083).
- **Annual cost** excluding VAT and CCL is `RawAnnualPrice` (FR-034).
- **Rounding** has one rule, `Rounding.ToPennies(x) = Math.Round(x, 2, MidpointRounding.AwayFromZero)`. It
  is applied only when a figure is formatted for display (screen, PDF, CSV) or written into a reporting
  record's `display` fields. Totals are summed at full precision, then rounded. Reporting records carry
  both the full-precision and the rounded value. Rates are never rounded: they are shown exactly as
  POWWR returns them.
- **Known consequence**: on a multi-site PDF, the rounded per-site figures can differ from the rounded
  total by up to 1p per site. FR-035 requires full precision, so this is accepted. If finance wants a
  "figures may not add up due to rounding" note, it becomes a new admin wording item through a spec
  change.
- **Base rates**, which are agent-only, are each returned rate minus the uplift POWWR reports
  (`Rate.Uplift` for unit rates, `Rate.StandingChargeUplift` for the standing charge). They are parsed as
  decimals and shown with the same number of decimal places as the returned string.
- **Cross-check**: `AnnualCostCrossCheck` recomputes the cost as
  Σ(unit rate × register kWh) / 100 + standing charge × 365 / 100 + any fixed fee, compares it with
  `RawAnnualPrice`, and logs a structured warning when they differ by more than 1% or £1. The comparison
  is also stored on `PriceOptionSite.CrossCheckDifference`. Capacity, reactive and seasonal rates are
  included when present. The check never changes the figures shown.

**Rationale**: Principle VI. Storing the engine output is the only way to guarantee a PDF and its
reporting record show the same pennies days later.

**Alternatives considered**: recalculating on every read. That's cheaper to store, but a settings change
(VAT, the commission basis) would silently change a past quote's figures.

## R8. VAT and CCL treatment

**Decision**: for each site, using the rates in effect on **that site's** start date (FR-031):

| Treatment | When | CCL | VAT |
|---|---|---|---|
| Low use | Automatic when annual kWh ÷ 365 ≤ the low-use threshold for the fuel (33 electricity, 145 gas, admin settings) | 0 | cost × reduced rate (5%) |
| Standard | The quote's treatment is Standard and the site isn't low use | kWh × CCL rate | (cost + CCL) × standard rate (20%) |
| Charity or other qualifying use | The quote's treatment is Qualifying with declared percentage q, and the site isn't low use | kWh × (1 − q) × CCL rate | cost × q × reduced rate + (cost × (1 − q) + CCL) × standard rate |

- If the optional setting "whole supply qualifies at or above" is set and q is at or above it, q is
  treated as 100%. The setting is effective-dated and starts blank until finance confirms (spec
  assumption).
- Low use can't be chosen by the agent (FR-030). The engine decides it per site.
- CCL rates are stored in p/kWh per fuel. The "include VAT and CCL" switch only chooses which stored
  figure is displayed, because the engine always stores both. The switch is saved with the quote and used
  on the PDF (FR-034).
- **Seed values**: VAT is 20% and 5%. CCL starts at the HMRC main rates in force at build time, entered
  as effective-dated rows and confirmed by finance before go-live. Low-use thresholds are 33 and 145
  kWh a day.

**Rationale**: this matches FR-029 to FR-031, and keeps VAT on CCL, as HMRC requires.

## R9. Commission: basis and upfront/monthly split

**Decision**:
- **Basis**: the admin setting `Commission.Basis` is `PerYear` (initially) or `WholeContract` (FR-037).
  Each pricing run stores the basis it used.
- **Contract total**: with `PerYear`, it is RawCommission × term months ÷ 12. With `WholeContract`, it is
  RawCommission.
- **Supplier payment model**: one number, `UpfrontPercent` (0–100), on the supplier. Upfront is contract
  total × UpfrontPercent ÷ 100. Monthly is (contract total − upfront) ÷ term months.
  - "All upfront" is 100.
  - "All monthly" is 0. This is the starting value for every supplier until finance provides the models.
- **Multi-site**: commission figures are summed across the sites priced by that option.
- **Basis sanity check**: the engine logs RawCommission ÷ (RawAnnualPrice − RawBaseAnnualPrice) for each
  option. A ratio near 1 suggests a per-year figure, and one near term ÷ 12 suggests a whole-contract
  figure. This helps answer Q3 with real data, without changing any figures.

**Alternatives considered**: richer models, such as "first year upfront, the rest monthly". These can be
added once finance describes them. A single percentage covers all-upfront, all-monthly and simple
splits.

## R10. Microbusiness

**Decision**: `MicrobusinessTest.Evaluate` flags the quote when:
- the sum of annual kWh across **all** sites is at or below the fuel's threshold (100,000 electricity,
  293,000 gas, admin settings, inclusive per the spec); **or**
- the agent ticks both "fewer than 10 employees" and "turnover or balance sheet no more than €2m".

The flag's source (`Consumption`, `Declaration` or both) is stored. A consumption-based flag can't be
removed by the agent (FR-028). It is re-evaluated on every pricing run and stored on the run, so the flag
on a PDF is the one in force when it was priced.

## R11. Saving against current

**Decision**: the saving is calculated for a site only when the agent has given the current standing
charge and a current unit rate for every register the site prices on. Otherwise the screen and PDF say
"No current prices given".
- **Current annual cost** = Σ(current rate × register kWh) / 100 + current standing charge × 365 / 100.
  With VAT and CCL included, the same per-site treatment is applied.
- **Saving** = current − new. A positive figure shows a green mark and the words "Saves £x a year", and
  a negative one shows a coral mark and "Costs £x more a year". Text stays navy.
- **Multi-site**: a total saving appears only when every site covered by that option has a saving.

## R12. Multi-site combination and partial totals

**Decision**:
- **Matching**: options are matched across sites by (pricing supplier name, term months, `PlanType` as
  returned).
- **Per option**: `PriceOption` holds the totals, and its `PriceOptionSite` rows hold the per-site
  breakdown.
- **Partial**: if any site has no rate for the key, either because of a no-quote reason or because the
  supplier was silent, the option is `IsPartial`. The missing sites are listed with POWWR's reason
  (FR-073). A PDF with a partial option lists the sites it doesn't cover (FR-074).
- **Failed requests**: a technical failure (`PricingRunSite.Status = Failed`) is not a no-quote reason.
  The run is "incomplete", and the PDF is blocked until those sites are retried.

## R13. Uplift limits (hard limits, no approvals)

**Decision**: `LimitPolicy` is a pure static class with unit tests. It applies FR-058 to FR-063. There
are no role or per-person limits and no approval process.

- **The limits**:
  - **Per-fuel maximums**, for everyone: the settings `UpliftLimit.{Electricity|Gas}.UnitRate`
    (p/kWh) and `.StandingCharge` (p/day).
  - **Supplier maximums**: four nullable columns on `Supplier`, a unit-rate and standing-charge pair
    for each fuel.
  - **Blank** means **0**: no uplift above zero is allowed (FR-060). That's why the seed data sets
    starting maximums for every fuel and supplier.
- **On entry** (`QuoteInputValidator`): the default uplifts and every per-supplier, per-term override
  must be ≥ 0 and ≤ the per-fuel maximum. Going over either measure on its own is enough. The error
  names the maximum, and nothing is sent to POWWR (FR-061).
- **Building the request** (`PricingService`): for each supplier and term, the applied uplift for each
  measure is the lower of the requested uplift and the supplier's maximum for the fuel. When it is
  lower than requested, the option stores `IsCappedAtSupplierMax`, and the results show the agent the
  amber tag "Capped at supplier maximum (x)" (FR-062). Both the requested and applied uplifts are
  stored.
- **Before a PDF or Outlook draft** (`PdfEligibility`): every selected option's *applied* uplifts are
  checked against the per-fuel and supplier maximums **in force now** (FR-063). Any option over a
  limit blocks the action, with a message naming the options, until the quote is re-priced. PDFs
  already stored stay downloadable.
- **Admin side**: a default uplift above its fuel's maximum is refused when saved (US5-5). Every limit
  change is audited with the old and new values (FR-082).
- **Reporting**: each price records whether it was capped (FR-084).

**Rationale**: these are the decisions from the security and compliance review in spec.md. Capping
at the supplier's maximum, instead of refusing, means one default uplift still works across suppliers
whose maximums differ.

## R14. Price validity and price-change checks

**Decision**:
- **Expiry** (FR-052): `PriceValidity.IsCurrent(fetchedUtc, nowUtc)` is true when both instants fall on
  the same `Europe/London` calendar date. This uses `TimeZoneInfo.FindSystemTimeZoneById("Europe/London")`,
  which works on Windows in .NET 6+ through ICU, and is mapped to "GMT Standard Time" if ICU is
  unavailable.
  - A run's fetched time is its **earliest** successful site response, which is the cautious choice
    for a multi-site run.
  - `TimeProvider` is injected everywhere, and the tests use a small fake.
- **Same-day changes**: before generating a PDF, the app calls `/pricechange` once (with no
  `SupplierName`, returning all suppliers). A supplier counts as changed when any `LatestPriceChange` for
  the quote's utility, a selected term (`Duration`) and the meter's half-hourly status has a `Version`
  different from the `PricebookVersion` stored for that selected option.
  - If `/pricechange` fails, the PDF is blocked with "We couldn't check for price changes. Try again.",
    because generating it without the check would risk a non-compliant quote.
  - On reopening a quote, the same check runs for information only (FR-077). If it fails, the page shows
    a quiet notice.
- **Outlook**: allowed only when the PDF's stored `PricesFetchedOn` date is today in UK time (FR-076).

**Rationale**: the spec's clarification (prices valid until the end of the UK day) and FR-077, which
names POWWR's price change history. Whether `LatestPriceChange.Version` matches `Rate.PricebookVersion`
needs POWWR's confirmation (P-17).

## R15. MPAN and MPRN validation

**Decision**: these live in `Services/Validation/`, as pure static classes covered by unit tests.

- **MPAN core**: 13 digits. The check digit is (Σ dᵢ × pᵢ for the first 12 digits, with primes
  3, 5, 7, 13, 17, 19, 23, 29, 31, 37, 41, 43) mod 11 mod 10. The algorithm was verified against POWWR's
  own example MPANs `1610028163273` and `1620000546231`, which both pass.
- **Distribution region**, from the first two digits (FR-017):

  | Digits | Region |
  |---|---|
  | 10 | Eastern |
  | 11 | East Midlands |
  | 12 | London |
  | 13 | Merseyside and North Wales |
  | 14 | Midlands |
  | 15 | Northern |
  | 16 | North Western |
  | 17 | North Scotland |
  | 18 | South Scotland |
  | 19 | South Eastern |
  | 20 | Southern |
  | 21 | South Wales |
  | 22 | South Western |
  | 23 | Yorkshire |
  | 24–99 | "Independent network (IDNO)" |
  | 00–09 | Invalid |

- **Top line, legacy (8 digits)**: profile class (2), MTC (3), LLFC (3).
- **Top line, post-MHHS (9 characters)**: the first 2 characters are the profile class, then the SSC
  (4), then the DUoS tariff ID (3). This follows POWWR's example `040151661`, where the SSC is `0151` and
  the DUoS tariff ID is `661`. Giving a 9-character top line sets `MhhsMigrated = true` and fills in the
  SSC and DUoS tariff ID (FR-016). The layout lives in one method, `MpanTopLine.Parse`, so a correction
  from POWWR (P-10) is a one-line change.
- **Top line against individual fields**: any field that appears in both places must match, or the field
  shows an error (spec edge case).
- **Half-hourly**: profile class `00`, or measurement class C, E, F or G (the half-hourly classes). A
  half-hourly meter requires kVA > 0 and allows kVArh (FR-018).
- **MPRN**: digits only, 6–10 long (FR-043). No check digit, because the spec doesn't define one.
- **Other fields**:

  | Field | Rule |
  |---|---|
  | Company number | Optional, for Limited company, LLP and PLC only; when given, 8 digits or 2 letters + 6 digits (FR-006) |
  | Email | `System.Net.Mail.MailAddress` parse, plus a domain-with-a-dot check |
  | UK phone | After removing spaces, `^(\+44|0)\d{9,10}$` |
  | Consumption | Whole kWh > 0, per register; or a total plus a split that adds up to exactly 100% (FR-022) |
  | Start date | Not before today, UK time |
  | Change-of-tenancy date | Required for COT quotes |
  | Uplifts | ≥ 0, at most 4 decimal places |

  When consumption is entered as a total plus a split, each register's kWh is split using the
  largest-remainder method, so the parts add up exactly.
- **One validator for both routes**: `QuoteInputValidator` returns field-keyed errors and is the single
  entry point for the form and for upload rows (FR-070). Pages call it before any POWWR call
  (Principle III), and pricing services call it again as a guard.

**Alternatives considered**: FluentValidation. It's a dependency, and DataAnnotations can't express the
cross-field MPAN rules cleanly. Plain validators are easier to test.

## R16. Rate structure (`/prompts`)

**Decision**:
- **When**: `/prompts` is called after the electricity meter fields pass validation.
- **Legacy top line**: send `MPANTop` and `MPANBottom`.
- **9-character top line**: send `MPANBottom`, `ProfileClass`, `MeterTimeswitchClass`,
  `StandardSettlementConfig`, `DuosTariffId` and `MhhsMigrated`, and leave `MPANTop` out. The definition
  allows only 8 characters there (Q15).
- **`LineLossFactorClass`**: sent as the 3-digit LLFC despite the definition's 8–9 length rule (Q15). If
  POWWR rejects it, the field is left out and the result is logged.
- **Prompt names** are mapped case-insensitively to consumption registers: "Day", "Unit" or "Standard"
  → Day; "Night" → Night; "Evening and Weekend", "Weekend" or "Wend" → Wend. An unknown prompt is logged,
  and the agent sees the total-only field.
- **MHHS-migrated meter with no SSC**: the agent picks a `ForcedRateStructure` (FR-021) and `/prompts` is
  skipped. The registers are:

  | Structure | Registers |
  |---|---|
  | Standard | Day |
  | Economy7 | Day, Night |
  | EveningAndWeekend | Day, Wend |
  | EveningWeekendAndNight | Day, Night, Wend |
  | OffPeak | Day, Night |

- **If `/prompts` fails**, the agent can still choose the registers by hand, with an amber notice.

## R17. Customer PDF rendering

**Decision**:
- **Template**: a Razor component, `Services/Pdf/CustomerQuoteDocument.razor`, rendered to an HTML
  string with ASP.NET Core's built-in `HtmlRenderer` (`Microsoft.AspNetCore.Components.Web`, part of the
  shared framework since .NET 8, so no package is needed). Its only input is `CustomerQuoteDocumentModel`
  (R18). Unit tests can render it without a browser.
- **Conversion**: Playwright for .NET turns the HTML into a PDF.
  - One Chromium browser is a lazily created singleton. Each PDF gets a new context and page.
  - `Pdf:MaxConcurrentRenders` (default 2) limits concurrent renders.
  - Settings: `Format = "A4"`, `PrintBackground = true`, `PreferCSSPageSize = true`.
- **Assets**: the HTML has `<base href="https://pdf.cu.local/">` and links the kit's `cu/css/tokens.css`,
  `base.css` and `components.css`, then the app's `css/pdf.css`. A Playwright route handler serves every
  `https://pdf.cu.local/**` request from `IWebHostEnvironment.WebRootFileProvider` and **aborts every
  other request**. That puts the kit's Poppins fonts, tokens and colour logo (`cu/logo/cu-logo-250.png`)
  into the PDF without changing the kit or touching the network. It also means a PDF can never fetch an
  external resource.
- **Footer**: a `position: fixed` footer in `pdf.css`, which Chromium repeats on every printed page. It
  holds our company registration number and an `<a href>` to the terms address, with the full address
  printed (FR-047). Unlike Playwright's `footerTemplate`, links in it stay clickable.
- **File name**: built by `PdfFileName.For(customerName, fuel, date)`. Characters in
  `Path.GetInvalidFileNameChars()` plus `/\:*?"<>|` are replaced by `-`, runs of whitespace become `-`,
  and the name is trimmed to 100 characters (FR-048).
- **Document properties** (FR-048): the HTML `<title>` is "Consultiv Utilities quote {reference}",
  which Chromium writes as the PDF's title. Chromium sets no author, subject or keywords. Its own
  creator, producer and date fields are allowed by FR-048. The quickstart checks the properties in a
  PDF viewer.
- **Storage**: the PDF is stored through `IFileStore` (R20) and recorded as `CustomerPdf`, with the
  wording version IDs it used (FR-051) and its SHA-256 hash. Re-downloading returns the stored bytes
  unchanged (US7-4).
- **Chromium install**: Playwright's script, `pwsh PricingTool/bin/Debug/net10.0/playwright.ps1 install chromium`,
  run through the repository's local PowerShell .NET tool (R26). The tests never launch a browser. They
  test the HTML.
- **Page count**: SC-010 (one or two pages for up to six options) is checked in the quickstart, and the
  layout aims for one page plus the terms.

**Alternatives considered**:
- Rendering a `.cshtml` view to a string with `IRazorViewEngine`. It needs an `ActionContext`, which
  makes unit tests awkward.
- `page.GotoAsync` to an app URL. The PDF would then depend on a running site and the user's cookies.
- `footerTemplate`, which doesn't keep links clickable or load the kit's fonts.

## R18. Customer-safe output

**Decision**:
- **A separate model**: customer-facing output is built only from `CustomerQuoteDocumentModel`. It is
  assembled by `CustomerQuoteModelBuilder` from the stored engine output. It has **no** property for
  any agent-only data in FR-100: base rates, uplift, annual cost before uplift (`RawBaseAnnualPrice`),
  commission and its basis, supplier notes (`ExtraInfo`, `FooterMessage`, `FooterNotes`), limits,
  capped flags, POWWR references or cross-check figures. The full list of allowed fields is in
  [contracts/customer-pdf.md](contracts/customer-pdf.md).
- **Customer view** (FR-099): when the agent turns it on, the results fragment is rendered on the
  server **without** the agent-only columns, capped tags and notes. They aren't hidden with CSS. The
  choice is stored on `StaffMember`, so it stays on across pages until turned off.
- **Commission disclosure**: the wording may contain named placeholders: `{CommissionTotal}`,
  `{CommissionUpfront}`, `{CommissionMonthly}` and `{CommissionPerYear}` for the recommended option, and
  `{SupplierName}`. The builder fills them in **only inside the disclosure paragraph** (FR-049).
- **Placeholder wording**: in the Development environment, any wording still marked as a placeholder
  renders inside a visible "PLACEHOLDER – NOT APPROVED WORDING" frame. In every other environment,
  `PdfEligibility` refuses to generate a PDF or create an Outlook draft while any wording item the PDF
  would use is a placeholder, and names the items (FR-050).
- **Disclosure scope**: compliance decides whether the disclosure uses the pound placeholders. The tool
  never requires an amount (FR-049).
- **The Outlook draft**: it has no body, subject or recipients, so the only customer content is the PDF.
- **Required tests** (Principle II):
  1. A reflection test fails if the customer model, or any type inside it, gains a property whose name
     contains `Base`, `Uplift`, `Commission`, `Note`, `ExtraInfo` or `Footer`, other than the approved
     disclosure text.
  2. A rendering test builds a quote whose agent-only values are distinctive (for example a base rate
     of `12.3456`, an uplift of `0.9876` and a commission of `£4,321.09`). It renders the PDF HTML and
     the file name, and asserts that none of those values appear in any common format, except the
     commission figure inside the disclosure when the wording asks for it.
  3. The `.eml` writer test asserts that the only part besides an empty text part is the PDF.

## R19. Settings, wording and audit

**Decision**:
- **Scalar and wording settings** live in `SettingVersion` rows. Each change inserts a new version
  (key, version, JSON value, placeholder flag, who, when) and never updates an old one.
  - The current value is the highest version for the key.
  - The wording a PDF used is recorded by version ID.
  - The change log (FR-082) is the version history plus `AuditEntry` rows, with the old and new values.
  - **Wording approval** (FR-050, FR-082): to clear a wording item's placeholder flag, the admin must
    tick "approved by compliance" and give the approver's name, which is stored as
    `ComplianceApprover` on that version, next to who saved it and when.
  - **Microbusiness wording** is three keys, all required on a microbusiness PDF (FR-047):
    commission disclosure, dispute resolution scheme, and cooling-off period.
  - **Uplift maximums per fuel** are four keys (R13). Saving a default uplift above its fuel's maximum
    is refused.
- **Effective-dated values** are `EffectiveRate` rows: VAT standard and reduced, CCL per fuel, low-use
  thresholds per fuel, and the "whole supply qualifies" percentage. Each has an `EffectiveFrom` date, and
  a lookup takes the latest row on or before the date given. Rows are never edited. A correction is a new
  row, and deleting a future-dated row is allowed and audited.
- **Structured settings** are tables: `Supplier` and its aliases, the payment model, and maximum
  uplifts for each fuel. Each change writes an `AuditEntry` with JSON old and new values. There are no
  role or per-person limits (FR-058).
- **Our company details** for the PDF (name, address, phone, email, website and company registration
  number) are in configuration (`Company:` in `appsettings.json`). They are not hard-coded, and they are
  not regulatory wording, so the spec doesn't make them admin settings. The terms address *is* an admin
  setting (FR-081).
- **Seed data**: a migration seeds starting values. Wording is placeholder text with `IsPlaceholder =
  true`. Suppliers are POWWR's example names with made-up aliases. Starting limits and thresholds follow
  the spec, and every seeded value is marked "to confirm". Uplift maximums are seeded for every fuel and
  supplier, because a blank one allows no uplift (FR-060).
- **Audit entries**: `AuditEntry` (who, when in UTC, action, entity, quote ID, old and new JSON) is
  written in the **same** `SaveChanges` as the action. If saving fails, the action fails (Principle V).
- **Append-only tables**: an EF Core `SaveChangesInterceptor` (`AppendOnlyGuard`) throws if any
  append-only entity is Modified or Deleted. These are `AuditEntry`, `SettingVersion`, `EffectiveRate`
  (except deleting a row whose date hasn't arrived yet), `PricingRun`'s inputs and results, `CustomerPdf`,
  `OutlookDraft` and `ReportingRecord`. The retention job deletes with `ExecuteDeleteAsync`, which
  bypasses the change tracker by design and is the only deletion path (FR-079 and FR-080).
- **Secrets are never audited**: they aren't settings at all. They live only in configuration.

## R20. File storage for PDFs

**Decision**: `IFileStore` has `SaveAsync(key, stream, contentType)`, `OpenReadAsync(key)` and
`DeleteAsync(key)`, with `LocalFileStore` rooted at `Storage:Files:LocalRoot` (default
`PricingTool/App_Data/files`, which is git-ignored).
- **Keys** look like `pdfs/{yyyy}/{MM}/{quoteReference}/{pdfId}.pdf`.
- **Path safety**: keys are built by the app, never from user input, and the store also rejects `..` and
  rooted keys.
- **Downloads** go only through `GET /Quotes/{id}/Pdfs/{pdfId}`, which checks quote access (FR-003).
  Nothing is under `wwwroot`.
- **Later**: an Azure Blob Storage implementation is a new class plus configuration, with no other code
  changes (Principle VII).

## R21. Reporting: record, outbox, export, Reports page

**Decision**:
- **Recording**: each quote event (FR-084) is recorded by `ReportingRecorder` as a `ReportingRecord` row,
  added in the **same** database transaction as the action it describes.
  - The row has a JSON `Payload` with `schemaVersion: 1` (see
    [contracts/reporting-record.md](contracts/reporting-record.md)), plus denormalised columns for the
    Reports page filters and totals: agent, date, fuel, quote type, regions, recommended option's
    commission, PDF and Outlook flags. Suppliers are in a `ReportingRecordSupplier` child table.
  - A `ReportingOutboxEntry` row is added with it.
  - A single database insert is the only reporting cost on the agent's request (FR-089).
- **Export**: `ReportingOutboxWorker` is a `BackgroundService` that polls every
  `Reporting:Outbox:PollSeconds` (default 30). It takes up to 100 pending entries and writes each payload
  through `IReportingExportStore` to `quotes/yyyy/MM/dd/{occurredUtc:HHmmssfff}-{eventType}-{recordId}.json`,
  which is unique and sortable.
  - It marks each entry as exported, or increments `Attempts` and sets `NextAttemptUtc` with exponential
    backoff capped at 5 minutes. That cap keeps delivery within SC-007's 15 minutes after the store
    recovers.
  - Entries are retried until they succeed. Writes are idempotent (overwrite by name).
  - `LocalFolderReportingExportStore` writes to `Storage:ReportingExport:LocalRoot` (default
    `PricingTool/App_Data/reporting`, git-ignored), through a temporary file and an atomic move, as
    Triage's `AtomicFile` does. Blob Storage comes later.
- **Failed events**: a pricing failure is recorded as a `Failed` event, with the error category but not
  the raw message if that could contain a secret.
- **Reports page** (`CanAdmin` only, FR-088):
  - Filters: agent, date range, fuel, supplier, region, quote type.
  - Totals:
    - quotes per agent;
    - PDFs produced;
    - PDFs opened in Outlook;
    - estimated commission: the sum, over the filtered quotes with a PDF, of the recommended option's
      whole-contract commission in each quote's latest PDF event (FR-085).
  - CSV export of the filtered list (FR-085):
    - the same columns as the Reports table, with no customer contact details;
    - a small RFC 4180 writer, which prefixes any cell starting with `=`, `+`, `-` or `@` with `'` to
      block formula injection;
    - each export writes an `AuditEntry` with who, when, the filters used and the number of rows
      (FR-086).
  - **Export failure notice** (FR-089): when the oldest unexported outbox entry is more than an hour
    old, the Reports page shows Admins a `.notice.tone-amber` saying how many records are waiting.
- **Contents**: records never include the customer's contact name, email or phone (FR-084). Each price
  carries the commission basis and whether it was capped.
- **Reporting store access** (FR-087): the local folder is under the git-ignored `App_Data`. In Azure,
  access to the Blob container is limited to Admins and the Power BI service (deployment feature).

## R22. Retention

**Decision**: `RetentionWorker` is a `BackgroundService` that runs daily at `Retention:RunAtUkTime`
(default `02:00` UK time).
- **What it deletes**: quotes whose `LastActivityUtc` is older than the admin setting
  `Retention.Years` (default 6, from the most recent activity, as the spec assumes). With each quote go
  its sites, runs, options, PDFs (files first, then rows), Outlook draft records, the quote's audit
  entries, its reporting records and outbox entries, and the exported reporting files (through
  `IReportingExportStore.DeleteAsync`) (FR-080). Downloaded CSVs are outside the tool's reach.
- **Batching**: it deletes in batches of 50 quotes, each batch in its own transaction.
- **Logging**: it logs counts and writes one system `AuditEntry` per run, with counts only.
- **What it keeps**: the admin settings change log isn't quote data, so retention leaves it alone.

## R23. Spreadsheet upload

**Decision**:
- **Template**: one column layout for CSV and `.xlsx`
  ([contracts/spreadsheet-template.md](contracts/spreadsheet-template.md)). Both are downloadable from
  the tool and generated on request.
- **CSV parsing**: `Microsoft.VisualBasic.FileIO.TextFieldParser`, which is part of the .NET shared
  framework, so no package is needed. It handles quotes and embedded commas and line breaks.
- **`.xlsx`**: read and written with **DocumentFormat.OpenXml** (Microsoft's Open XML SDK). The reader
  handles shared strings, inline strings and numbers, and turns numeric date cells in the date columns
  into dates with `DateTime.FromOADate`. The template writer adds the header row, a hidden "Lists" sheet
  with data validation for the fixed lists, and one example row.
- **Validation**:
  - Every row is validated with the same `QuoteInputValidator` site rules (FR-070), plus two checks: a
    meter number repeated within the upload or the quote, and a row for the other fuel. Both are errors.
  - Errors are listed per row and column, and nothing is priced until every row is fixed or removed.
  - Valid rows are kept as a pending upload on the quote, so the agent can fix only the bad rows,
    either in the browser or by uploading again (SC-008).
- **Limits**: files up to 2 MB, with at most 200 data rows (`Uploads:MaxRows`). The spec's "usually under
  50" sits well inside both.

**Alternatives considered**:
- ClosedXML: a friendlier API, but heavier dependencies.
- ExcelDataReader: read-only, and the template still needs writing.
- CsvHelper: not needed, because `TextFieldParser` covers it.

## R24. UI, design kit and progressive enhancement

**Decision**:
- **Kit files**: `css/`, `js/`, `fonts/` and `logo/` are copied unchanged from the brand folder into
  `PricingTool/wwwroot/cu/`.
- **Head**: `Pages/Shared/_Head.cshtml` links `~/cu/css/tokens.css`, `base.css` and `components.css`
  with `asp-append-version="true"`, preloads `~/cu/fonts/poppins-700.woff2`, and uses
  `~/cu/logo/favicon-32.png`. It then loads `~/css/app.css` (tokens only) and, deferred,
  `~/cu/js/kit.js` and `~/js/app.js`.
- **App shell**: `_Layout.cshtml` follows the kit's shell. The navy `.sidebar` holds the white
  `cu-logo-150-white.png` (with a 250 `srcset`), `cu-mark-white.png` for the rail, and these nav items:
  - New quote (`CanQuote`)
  - Quotes (all roles: an agent's own quotes, or every quote for an Admin)
  - Reports and Settings (`CanAdmin`)

  Then come the `.topbar` and `.content`. Nav items are left out for roles that don't have them, and the
  server enforces access as well.
- **Standalone pages**: `_StandaloneLayout.cshtml` (`.standalone`) serves sign-in, loading ("Taking you
  to Microsoft sign-in…", which continues automatically, with a button if script is off), no-access, and
  the Development test sign-in.
- **Icons**: Lucide icons are inline SVG from a small `Icons` class (the path data for the icons we
  use) and rendered by an `<icon name="…">` tag helper. They follow the kit's rules: 24×24 viewBox,
  `currentColor`, 2px stroke. Lucide's ISC licence goes in `THIRD-PARTY-NOTICES.md`. There's no npm.
- **Progressive enhancement**: every flow works with plain form posts (Post/Redirect/Get). `app.js` adds
  these on top:
  - the postcode → address → meter picker, fetching server-rendered partials from page handlers;
  - result filters and sorting, swapping in the results partial;
  - `.load-steps` while prices are fetched;
  - `CU.toast()` with an Open action after "Open in Outlook";
  - the "Customer view" `.switch-row`, which re-fetches the results partial without the agent-only
    data (FR-099);
  - the antiforgery token header on `fetch`.
- **Swapped HTML and the kit's menus**: `kit.js` binds `[data-menu]`, `.nav-item[data-tab]` and `.split`
  **once, per element, at page load**, and has no re-initialise function. Dialogs and drawers
  (`data-open` and `data-close`) use delegated listeners, so they work in swapped HTML. So the rule is:
  **HTML swapped in after load must not contain `data-menu`, `data-tab` or `.split`.** Swapped fragments
  use buttons, `data-open` dialogs and drawers, `.filter-pill` links and `app.js`'s own delegated
  handlers. Menus, tabs and split views stay in the parts of the page that are never swapped. A
  `CU.init(root)` hook in the kit would remove this limit (kit gap 1 in plan.md).
- **Security headers**: CSP `default-src 'self'; img-src 'self' data:; style-src 'self'; script-src 'self'; frame-ancestors 'none'; form-action 'self' https://login.microsoftonline.com`,
  plus `X-Content-Type-Options` and `Referrer-Policy`. There's no inline script, so `app.js` does all
  wiring.

## R25. Secrets and configuration

**Decision**:
- **Secrets**: all secrets are read only through `IConfiguration`. They are:
  - `AzureAd:ClientSecret`
  - `Powwr:Pricing:LicenceCode`
  - `Powwr:MeterLookup:ClientSecret`
  - `ConnectionStrings:PricingDb`, if it ever carries a password (locally it uses Windows sign-in, so it
    can sit in `appsettings.Development.json`)
- **Local storage**: .NET user secrets (`UserSecretsId` in the csproj).
- **Azure, later**: Key Vault through `AddAzureKeyVault` with managed identity is added by the
  deployment feature as one more configuration provider. No code that reads settings changes.
- **Options classes**: `PowwrPricingOptions`, `MeterLookupOptions`, `OutlookOptions`, `StorageOptions`,
  `CompanyOptions` and `RetentionOptions`, each with `ValidateOnStart()`. In `Live` or `Sandbox` modes, a
  missing secret stops startup with a message naming the **key**, never the value.
- **Logging**: no secret or token is logged. The stored request copy masks `LicenceCode` (R4). Log
  scopes carry the quote reference, not customer contact details.

## R26. Tooling

**Decision**: a local tool manifest, `.config/dotnet-tools.json`, pins:
- `dotnet-ef`, for migrations;
- `PowerShell` (`pwsh`), published as a .NET tool, so Playwright's own `playwright.ps1` script can install
  Chromium on a machine that only has the .NET SDK. This machine has Windows PowerShell 5.1 and no
  PowerShell 7.

The commands are `dotnet tool restore`, then `dotnet pwsh PricingTool/bin/Debug/net10.0/playwright.ps1 install chromium`.
There's no Python, Node.js or npm anywhere (Principle VIII). The local port is fixed in
`launchSettings.json`: `https://localhost:8101` (with `http://localhost:8100`), which leaves Triage's
8000 free. The local Entra redirect URI is `https://localhost:8101/signin-oidc`, because OpenID
Connect's form-post cookies need HTTPS. The developer trusts the dev certificate once with
`dotnet dev-certs https --trust`.

## R27. Testing

**Decision**: xUnit 2.9.3, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk` and `coverlet.collector`,
as in Triage. The tests call pure logic directly, with no database, HTTP or POWWR (mocks only, made-up
data). These areas are required by the constitution:

| Area | Test classes (indicative) |
|---|---|
| MPAN and MPRN validation | `MpanCoreTests` (check digit, POWWR examples, regions, IDNO), `MpanTopLineTests` (8-digit, 9-character MHHS, field agreement), `MprnTests`, `PostcodeTests`, `CompanyNumberTests`, `QuoteInputValidatorTests` |
| POWWR mapping | `PowwrPricingRequestMappingTests` (every request field in the mapping contract), `PowwrPricingResponseMappingTests` (rates, raw decimals, no-quote reasons, `Error`, bad data), `MeterLookupMappingTests` (string to int, redaction, AMR, warnings, RFC 7807), `SupplierMatcherTests` |
| Pricing engine | `VatCclCalculatorTests` (all three treatments and thresholds), `CommissionCalculatorTests` (both bases and payment models), `SavingCalculatorTests`, `MicrobusinessTestTests` (on the threshold, combined sites, declaration), `LowUseTestTests`, `MultiSiteCombinerTests` (totals, partial), `RoundingTests`, `AnnualCostCrossCheckTests`, `RegisterSplitTests` |
| Uplift limits | `LimitPolicyTests` (per-fuel maximum refused at entry; either measure alone; supplier cap applied and flagged; blank limit = 0; re-check against lowered limits before PDF and Outlook), `DefaultUpliftValidationTests` (default above the maximum refused) |
| Spreadsheet uploads | `CsvSiteReaderTests`, `XlsxSiteReaderTests` (with an in-memory workbook), `SiteUploadValidatorTests` (repeated meter, other fuel, per-row errors) |
| Customer-safe output | `CustomerModelShapeTests` (reflection), `CustomerPdfContentTests` (rendered HTML, file name and `<title>`), `EmlDraftWriterTests`, `PdfFileNameTests`, `CustomerViewTests` (results fragment with Customer view on holds no agent-only data), `PlaceholderGateTests` (PDF and Outlook refused outside Development while wording is a placeholder; three microbusiness items required) |
| Reporting records | `ReportingRecordBuilderTests` (schemaVersion, every FR-084 field, rounding pairs), `ReportingExportPathTests`, `OutboxBackoffTests`, `CsvWriterTests` (escaping, formula injection) |
| Time | `PriceValidityTests` (UK midnight, British Summer Time changes) |
| Security | `DevSignInTests` and `AuthorisationTests`, using `Microsoft.AspNetCore.Mvc.Testing` with the database left untouched: `/Dev/SignIn` returns 404 in Production; a dev mode outside Development fails at startup; an anonymous request is challenged; a role-less user gets NoAccess; an Admin-only user is refused New quote; the session cookie lasts 8 hours. `QuoteAccessTests` (pure): an agent can't read another agent's quote; an Admin can read but not change one |

The test project carries a `FakeTimeProvider`, a subclass of `TimeProvider` of about 10 lines, so it
doesn't need the testing package.

## R28. Quote reference

**Decision**: a SQL Server `SEQUENCE` (`QuoteNumbers`, through EF's `HasSequence`) gives the format
`CUQ-{yy}-{number:000000}`, for example `CUQ-26-000042`. The number is global and never resets, so
references stay unique (FR-078) across years and work the same on Azure SQL.

---

## Open items (not blocking the plan)

The tool's behaviour until each is answered is fixed by the spec or the decisions above.

| ID | For | Question | Until answered |
|---|---|---|---|
| P-1 (Q1) | POWWR | Test licence, charging and rate limits | Mocks; at most 4 requests at once; retry only 429 and 503 |
| P-3 (Q3) | POWWR | What `RawCommission` covers | Admin basis setting, starting per year; ratio logged (R9) |
| P-4 (Q4) | POWWR | Allowed `PlanType`, `BusinessType`, `MeterType` values | `PlanType` left out; business types mapped through a configuration table (`Powwr:Pricing:BusinessTypes`), so an answer needs no code change; meter type shown as returned |
| P-5 (Q5) | POWWR | Uplift units | p/kWh and p/day |
| P-9 (Q9) | POWWR | Which date field is the new contract's start date | `ContractRenewalDate` = start date; `NewContractEndDate` = start + term − 1 day, per requested term (one value only when one term is requested, otherwise left out) |
| P-10 (Q10) | POWWR | Layout of the 9-character top line, and whether it means MHHS | Layout in R15, in one method |
| P-13 (Q13) | POWWR | What `mascaradeuser` should be | The agent's display name |
| P-14 (Q14) | POWWR | Supplier name and ID map | Admin aliases (R5) |
| P-15 (Q15) | POWWR | `/prompts` length rules | R16 |
| **P-16 (new)** | POWWR | Does any lookup field show a pending electricity switch? `tradingStatus` (N/R/T/X) isn't explained. | No electricity switching warning (gas uses `incomingSupplier`). **This leaves FR-012 partly unmet for electricity.** Added to `docs/powwr/README.md` as question 16. |
| **P-17 (new)** | POWWR | Does `LatestPriceChange.Version` match `Rate.PricebookVersion`? | Compared directly (R14); if the formats differ, the check falls back to `LastPriceChange` ≥ the fetched date. Added to `docs/powwr/README.md` as question 17 |
| F-1 | Finance | VAT and CCL partial-use rule, and the CCL rates | R8 settings, starting blank or "to confirm" |
| F-2 | Finance | Supplier payment models | `UpfrontPercent` = 0 (all monthly) |
| C-1 | Compliance | All customer wording and the terms address | Placeholders, visibly marked |
| IT-1 | IT | App registration: Agent and Admin roles, group assignment, `Mail.ReadWrite` (delegated), `User.Read`, emitting application groups, redirect URI | Development test sign-in and Eml mode |

**Agent phone number on the PDF** (FR-047): the token doesn't carry it. In Entra mode, the app reads
`businessPhones[0]` (else `mobilePhone`) from Graph `GET /me?$select=businessPhones,mobilePhone`, using
`User.Read`, which is part of the standard sign-in consent. It's read the first time it's needed and
cached on `StaffMember` for 24 hours. If neither is set, the PDF shows the company phone from
configuration. This is a separate, read-only call about the agent, not their mailbox, but IT should
know about it (IT-1).
