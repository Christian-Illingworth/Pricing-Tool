# CLAUDE.md: CU Pricing Tool

## What this is
An internal web app for Consultiv Utilities' sales agents (we are a UK energy broker). An agent enters a business customer's electricity or gas supply details, gets live prices from the POWWR API, compares suppliers and terms with our uplift applied, and produces a branded PDF quote, which they can open in a blank Outlook email. Roles: Agent, Manager, Admin.

## Where things are
- `.specify/memory/constitution.md`: the project's non-negotiable rules. Read it before planning or coding. It overrides anything else in this file.
- `specs/<###-feature>/`: `spec.md` (what and why), `plan.md` (how) and `tasks.md` (the work list) for each feature. The active feature is recorded in `.specify/feature.json`.
- `DESIGN.md`: the Consultiv Utilities web design rules. Follow it for all UI.
- `wwwroot/cu/`: the design kit (CSS, JS, fonts, logos). Never edit these files.
- `docs/powwr/`: the POWWR API documentation. Check it before assuming what the API supports.

## How we work: Spec Kit
Features are built with Spec Kit: `/speckit-specify` → `/speckit-clarify` → `/speckit-plan` → `/speckit-checklist` → `/speckit-tasks` → `/speckit-analyze` → `/speckit-implement` → `/speckit-converge`.
- Don't write feature code outside this flow. If a request isn't covered by the current spec, say so and suggest updating the spec or starting a new feature first.
- Implement one phase at a time, then stop and tell me how to run and test it.
- Mark tasks `[X]` in `tasks.md` as you finish them.
- If the code and the spec disagree, stop and ask. Don't quietly change either.

## Git and GitHub: one branch per feature
The code lives on GitHub at https://github.com/Christian-Illingworth/Pricing-Tool (remote `origin`). Use the GitHub CLI (`gh`) for pull requests. It's signed in with a token that only has access to this repository, on purpose: never try to reach other repositories or organisations, and never ask me for broader access. If a `gh` or `git push` command fails with a permissions or credentials error, stop and tell me; the token may need renewing.

**Never commit or push feature work directly to `main`.** Changes reach `main` only through a merged pull request.

**Starting a feature**
- Every feature gets its own branch, created when the feature starts. The Spec Kit git extension does this automatically when `/speckit-specify` runs (for example `001-pricing-tool`).
- For work outside Spec Kit, switch to an up-to-date `main` and create one yourself: `git switch -c <###-short-name>`.
- Push the new branch to GitHub straight away: `git push -u origin <branch>`.

**While building**
- Commit on the feature branch after each completed phase, with a clear message, e.g. `feat(quotes): add electricity results table`, then `git push` so the work is backed up on GitHub.
- Never commit secrets, `bin/`, `obj/`, `.vs/`, `.claude/settings.local.json`, or locally generated PDFs and reporting exports. Keep `.gitignore` covering them.

**The feature is complete when all of these are true**
1. `/speckit-converge` reports **Converged**, and every task in `tasks.md` is `[X]`.
2. `dotnet build` succeeds with no errors.
3. `dotnet test` passes.
4. The app starts locally and the feature works with the mock POWWR adapter.

**Then merge it into `main` through a pull request**
1. Push the branch, then open a pull request: `gh pr create --base main --title "<feature name>" --body "<summary>"`. The summary lists what was built, any database migrations, and anything I need to do.
2. Show me the pull request link and the summary, and ask me to confirm.
3. When I confirm, merge it with a merge commit so the feature stays visible in the history, and delete the branch: `gh pr merge --merge --delete-branch`. If GitHub shows failing checks, stop and tell me instead.
4. Switch to `main` and pull (`git switch main`, then `git pull`), then run `dotnet build` and `dotnet test` again.

If the merge has conflicts, stop and show me. Don't guess. Never force-push, rebase or rewrite history on `main`.

## Tech stack (`plan.md` has the details)
- ASP.NET Core on the current .NET LTS release, with server-rendered Razor Pages. No Bootstrap, jQuery, Tailwind or any other CSS framework.
- Entity Framework Core with SQL Server: SQL Server Express locally (`.\SQLEXPRESS`), Azure SQL later.
- Microsoft Entra ID sign-in (Microsoft.Identity.Web), with Agent, Manager and Admin app roles.
- PDFs rendered from a Razor template with Playwright for .NET.
- Outlook drafts created through Microsoft Graph (delegated Mail.ReadWrite).
- xUnit for tests.
- C# and .NET only: don't add Python, Node.js or npm to the app's build or tooling. Install Playwright's browser with its PowerShell script.
- The app is built and run locally first. Azure deployment is a separate, later feature, so don't add Azure resources until then.

## Commands
Update this section with the real project paths once the solution exists.
```powershell
dotnet build
dotnet test
dotnet run --project <web project>
dotnet ef database update --project <data project>
```

## Rules that are easy to break
- **Secrets:** never in code, config files or the repo. Use .NET user secrets locally.
- **Customer-safe:** base rates and commission never appear in the PDF, the Outlook draft or anything else a customer sees, except in the approved disclosure wording.
- **One pricing engine:** every cost, saving, commission, VAT, CCL and rounding calculation goes through the pricing engine. Never calculate prices in a page, view or PDF template.
- **Validate first:** MPANs, MPRNs and all other inputs are validated on the server before any POWWR call.
- **Compliance wording:** customer-facing regulatory text comes from admin settings, never hard-coded.
- **No sending email:** "Open in Outlook" only creates a blank draft with the PDF attached, in the agent's own Drafts folder, so it works in both classic and new Outlook. The app never sends or reads email.
- **UI:** follow `DESIGN.md`. Use the classes in `wwwroot/cu/css/components.css` and the tokens in `tokens.css`; never hard-code a colour. Text is navy; the only other text colour allowed is teal, for links and active controls.
- **POWWR:** always go through the adapter. Use the mock unless I say otherwise, and never call the live API from tests.
- **Reporting:** every quote event goes through the reporting outbox, and reporting must never slow down or block an agent.

## Testing
- Add or update unit tests with every change to validation, the pricing engine, uplift limits and approvals, spreadsheet uploads or reporting records.
- Tests use the mock POWWR adapter and made-up data, never real customer data.

## Domain terms
- **MPAN:** electricity meter number. The top line is the profile class (2 digits), meter time-switch code (3) and line loss factor class (3). The core (bottom line) is 13 digits ending in a check digit; its first two digits are the distribution region. Profile class 00 means half-hourly (HH).
- **MPRN:** gas meter number (6–10 digits). **LDZ:** gas local distribution zone.
- **EAC / AQ:** estimated annual consumption (electricity) / annual quantity (gas), in kWh.
- **kVA:** agreed supply capacity, for half-hourly electricity meters.
- **Uplift:** our broker margin added to the supplier's base rates (p/kWh and p/day). It becomes our commission.
- **CCL:** Climate Change Levy.
- **Microbusiness:** a customer type with extra Ofgem protections. The spec defines the test.
- **Quote types:** renewal, new supplier, change of tenancy.
