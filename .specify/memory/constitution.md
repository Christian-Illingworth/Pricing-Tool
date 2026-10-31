# Consultiv Utilities Pricing Tool Constitution

The Consultiv Utilities Pricing Tool is an internal web app. Our sales agents use it to get live
business energy prices from the POWWR API, apply our uplift, compare suppliers and terms, and
produce branded PDF quotes for customers. The rules below are binding on every specification,
plan, task and line of code in this project.

## Core Principles

### I. Security First (NON-NEGOTIABLE)

- POWWR credentials (the UK Pricing API licence code and the Meter Lookup OAuth client secret),
  Entra and Microsoft Graph client secrets, and every other secret MUST NOT appear in source code,
  the repository, `appsettings*.json` files, Azure App Service application settings, or anything
  sent to the browser (pages, scripts, hidden fields, cookies or responses).
- On developer machines, secrets MUST live in .NET user secrets. Once deployed, they MUST live in
  Azure Key Vault, read through the app's managed identity. Code MUST read secrets only through
  the .NET configuration system, so the source can change without code changes.
- Access tokens for POWWR and Microsoft Graph MUST be acquired and held on the server. Secrets and
  tokens MUST NOT be written to logs, audit records, reporting exports or error messages.
- Only signed-in staff who belong to the approved Microsoft Entra group (currently
  "Pricing Tool Users") in our tenant may use the app. Every page and handler MUST require sign-in
  by default, and the Agent and Admin roles MUST be enforced on the server, not only by hiding
  navigation.
- Any development-only sign-in shortcut MUST work only in the Development environment and MUST NOT
  be reachable in any deployed environment.

**Rationale:** the POWWR credentials give access to live market pricing under our licence, and the
app holds customer and commission data. A single leaked secret or open page is a commercial and
data-protection incident.

### II. Customer-Safe Output (NON-NEGOTIABLE)

- Base (pre-uplift) rates, the uplift applied and commission (upfront and monthly) are for agents
  only. They MUST NOT appear on anything a customer sees: the PDF quote, the Outlook draft and its
  attachment, file names, or any other customer-facing output.
- The only exception is a figure that the approved commission disclosure wording (Principle IX)
  requires, shown exactly as that wording specifies.
- Customer-facing output MUST be built from a customer-facing model that does not carry the
  agent-only fields. Hiding agent-only values with CSS, conditional markup or blank cells is not
  enough.
- Automated tests MUST check that customer-facing output contains no agent-only values.

**Rationale:** showing a customer our base rates or commission outside the approved disclosure
damages trust, undermines our commercial position and can breach Ofgem broker rules.

### III. Validate Before Calling Out

- Every input MUST be validated on the server before any POWWR request (pricing or meter lookup)
  is sent. Validation in the form gives quick feedback but is never trusted on its own.
- MPANs (top line and 13-digit core, including the check digit and distribution region), MPRNs,
  postcodes, consumption figures, dates and every other field MUST pass validation first. Each row
  of a multi-site upload MUST be validated, with errors shown per row, before any site is priced.
- Invalid input MUST return clear, field-level errors and MUST NOT reach POWWR.
- Data returned by POWWR is external input too: it MUST be mapped and checked in the adapter
  before it reaches the pricing engine or the screen.

**Rationale:** bad meter numbers produce wrong or misleading prices, waste POWWR's rate limits and
put inaccurate quotes in front of customers.

### IV. On-Brand and Accessible

- Every screen and the customer PDF MUST follow `DESIGN.md` from the Consultiv Utilities web
  design kit. The older `Consultiv-Utilities-Brand-Guide.md` does not apply.
- Markup MUST use the kit's classes, and colours MUST come from the kit's tokens
  (`var(--token)`). Hard-coded colours (hex, `rgb()`, `hsl()` or named colours) are not allowed in
  stylesheets, Razor views or the PDF template, and new shades MUST NOT be invented.
- No other CSS framework (Bootstrap, Tailwind, jQuery UI or similar) may be added. The kit's
  files in `wwwroot/cu/` MUST be used unchanged. App-specific styles go in a separate stylesheet,
  loaded after the kit, that uses tokens only. A component the kit lacks is listed for adding to
  the kit, not restyled locally.
- Text is navy. Teal is the only other text colour, and only for links and active controls.
- All text and controls MUST meet WCAG AA contrast and work by keyboard with the kit's focus ring.
- Layouts MUST work above, between and below the kit's 1100px and 720px breakpoints.

**Rationale:** agents and customers should see one consistent Consultiv Utilities brand, matching
our other apps, and every member of staff must be able to use the tool.

### V. Auditable

- The app MUST store every quote request (its inputs), the results POWWR returned, the uplifts
  applied, every PDF generated, every Outlook draft created, and every change to admin settings
  (with the old and new values).
- Each record MUST identify the signed-in person who did it (their Entra identity) and when (UTC).
- If the audit record cannot be saved, the action MUST fail rather than proceed unrecorded.
- Audit records MUST NOT be editable or deletable through the app. The only deletion allowed is
  the automated retention policy defined in the specification.

**Rationale:** we must be able to show what a customer was quoted, on what basis, with what
margin and within which uplift limits, and who did each step, for complaints, compliance reviews
and commission disputes.

### VI. One Set of Numbers

- A single pricing engine MUST do every calculation: annual costs, savings, commission (upfront
  and monthly), VAT, CCL, multi-site totals and rounding. Pages, views, the PDF template and
  reporting MUST display the engine's results and MUST NOT calculate prices themselves.
- Money and energy calculations MUST use `decimal` at full precision, never `float` or `double`.
  Rounding MUST happen only through the engine's single, documented set of rounding rules.
- The screen, the PDF and reporting MUST take their figures from the same engine result, so they
  always match to the penny.
- The engine MUST be covered by unit tests, and every change to it MUST come with tests.
- Each POWWR API MUST be reached only through an adapter interface with a mock implementation
  that returns realistic data, selected by configuration. The mocks are the default, so everything
  can be built and tested without live credentials. Tests MUST NOT call the live POWWR API.

**Rationale:** a quote whose figures differ between the screen, the PDF and reporting is a
compliance and trust failure. One tested engine and a swappable integration prevent that.

### VII. Local First, Azure-Ready

- The app MUST run, and its full test suite MUST pass, on a developer's machine with only the .NET
  SDK and SQL Server Express installed, the mock POWWR adapters, and no Azure resources.
- Everything that differs between local and Azure MUST sit behind configuration or an interface:
  secrets (configuration providers), the database (connection string, with EF Core migrations that
  work on both SQL Server Express and Azure SQL), file storage and reporting exports (a local
  folder now, Blob Storage later), logging (standard ASP.NET Core logging, ready for Application
  Insights) and the sign-in redirect URI.
- Deploying to Azure UK South later MUST need configuration and infrastructure work only, not a
  rewrite. No Azure resources are created before the dedicated deployment feature.
- Once deployed, all infrastructure MUST be defined in Bicep in this repository and changed only
  through the CI/CD pipeline, never by hand in the Azure portal. The pipeline's own credentials
  follow Principle I.

**Rationale:** building locally keeps costs and risk low while the tool is shaped. Designing the
seams now means moving to Azure is a deployment step, not a redesign, and keeps data in the UK.

### VIII. Keep It Simple

- The app and its build and tooling MUST be C# and .NET only: no Python, Node.js or npm.
  Playwright's browser is installed with its PowerShell script.
- Pages MUST be server-rendered with ASP.NET Core Razor Pages. Client-side script is limited to
  the design kit's JavaScript and small progressive enhancements; no single-page-app framework.
- Dependencies MUST be few. Every NuGet package that is not part of the stack named below MUST be
  justified in the feature's `plan.md`.
- The project MUST follow the same stack, structure and conventions as our existing Triage app.
- Build what the current specification needs, not what a future one might.

**Rationale:** a small team maintains this tool alongside Triage. One familiar stack with few
moving parts is easier to secure, review, hand over and keep running.

### IX. Compliance Wording Is Managed, Not Hard-Coded

- Customer-facing regulatory text (the commission disclosure, microbusiness protections, credit
  check wording, VAT and CCL notes, and the indicative-price notice) and the web address of our
  terms and conditions MUST come from admin-managed settings. They MUST NOT be hard-coded in code,
  views or the PDF template.
- Every customer PDF MUST state that its prices are indicative and subject to change, and MUST
  link to our terms and conditions, with the full web address printed so it also works on paper.
- Admins enter only wording approved by compliance. Only the Admin role may change it, and every
  change MUST be logged with who made it, when, and the old and new text (Principle V).
- Each generated PDF MUST record which version of each wording it used, so any past quote can
  show exactly what the customer was told.
- Until compliance supplies the wording, placeholder text MUST be visibly marked as a placeholder
  so it cannot be mistaken for approved wording.

**Rationale:** regulatory wording changes with Ofgem rules and compliance advice. Compliance must
be able to update it without a code release, and we must be able to prove what each customer saw.

## Technology and Operating Constraints

- **Stack:** ASP.NET Core on the current .NET LTS release with Razor Pages; Entity Framework Core
  with SQL Server (SQL Server Express locally, Azure SQL later); Microsoft Entra ID sign-in through
  Microsoft.Identity.Web with Agent and Admin app roles; PDFs rendered from a Razor
  template with Playwright for .NET; Outlook drafts through Microsoft Graph; xUnit for tests.
- **POWWR:** `docs/powwr/` is the reference for what the UK Pricing and Meter Lookup APIs
  support. Check it before assuming any behaviour.
- **Outlook:** "Open in Outlook" only creates a draft in the agent's own mailbox with the PDF
  attached, using delegated `Mail.ReadWrite` for the create-message call only. The app MUST NOT
  send or read email.
- **Reporting:** every quote event goes through the reporting outbox. Reporting MUST NOT slow down
  or block an agent, and failed exports are retried.
- **Data in development:** tests, fixtures, samples and the repository use made-up data only,
  never real customer data. Locally generated PDFs and reporting exports are never committed.

## Development Workflow and Quality Gates

- **Spec Kit flow:** features are built through `/speckit-specify` → `/speckit-clarify` →
  `/speckit-plan` → `/speckit-checklist` → `/speckit-tasks` → `/speckit-analyze` →
  `/speckit-implement` → `/speckit-converge`. No feature code is written outside this flow. If the
  code and the specification disagree, work stops until a person decides which is right.
- **Constitution Check:** every `plan.md` MUST check the design against all nine principles before
  research and again after design. Principles I and II can never be waived. Any other deviation
  MUST be recorded and justified in the plan's Complexity Tracking table and approved by the
  project owner before work starts.
- **Required tests:** xUnit unit tests MUST accompany every change to input validation (including
  MPAN and MPRN rules), the pricing engine, POWWR request and response mapping, uplift limits,
  spreadsheet uploads, customer-safe output and reporting records. These tests are
  required regardless of whether a template marks tests as optional.
- **Branches:** each feature has its own branch. Nothing is committed or pushed directly to
  `main`; changes reach `main` only through a merged pull request.
- **Definition of done:** `/speckit-converge` reports Converged with every task marked `[X]`;
  `dotnet build` succeeds with no errors; `dotnet test` passes; and the app starts locally and the
  feature works with the mock POWWR adapters.
- **Review:** every pull request is checked against this constitution, with particular care for
  secrets, customer-facing output, calculations outside the pricing engine and hard-coded colours
  or wording.

## Governance

- This constitution overrides `CLAUDE.md`, the playbook, specifications, plans and any other
  guidance. Where they conflict, this document wins and the other document is corrected.
- **Amendments** are made with `/speckit-constitution` on a branch, reviewed in a pull request and
  approved by the project owner before merging. Each amendment updates the version, the Last
  Amended date and the Sync Impact Report, and the specifications, plans and guidance files that
  depend on the changed rules are brought into line in the same change or listed as follow-ups.
- **Versioning** follows semantic versioning: MAJOR for removing or redefining a principle in a
  way that is not backward compatible; MINOR for adding a principle or section or materially
  expanding guidance; PATCH for clarifications and wording fixes that do not change meaning.
- **Compliance review:** the Constitution Check in each `plan.md`, the `/speckit-analyze` report
  (where a conflict with this constitution is always CRITICAL) and pull request review are the
  three points where compliance is verified.
- **Runtime guidance:** `CLAUDE.md` holds day-to-day development guidance and `DESIGN.md` holds
  the UI rules. Both MUST stay consistent with this constitution.

**Version**: 1.1.0 | **Ratified**: 2026-10-30 | **Last Amended**: 2026-10-31
