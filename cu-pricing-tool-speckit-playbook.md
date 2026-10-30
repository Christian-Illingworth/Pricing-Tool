# CU Pricing Tool: build playbook

Follow this from top to bottom. Every step tells you **where** to type, **what** to paste, **what you should see**, and **how you know it's done**.

## Read this first

1. **There are two places to type, and they're not interchangeable.**
   - **Terminal:** in VS Code, **Terminal → New Terminal**. Only paste blocks labelled `powershell` here.
   - **Claude Code chat:** the Claude Code panel in VS Code. Everything starting with `/speckit-`, and every plain-English instruction, goes here.
2. **One step at a time.** Paste one block, wait until Claude has completely finished, do the check, then move on.
3. **Copy blocks exactly.** Use the copy button on each block and paste the whole thing as one message, even the long ones.
4. **Approvals:** Claude Code asks before it runs a command or changes a file. Read what it wants to do, then allow it. If you don't understand, ask "Why do you need to do that?" first. Don't switch on any "approve everything automatically" mode.
5. **Never paste passwords, licence codes or client secrets** into the chat or into any file. Step 8 explains how to store them safely.
6. **If something looks wrong, say so in plain English** in the chat, e.g. "The spec says agents can send emails. They can't; fix it." You don't need to know the technical fix.
7. **Taking a break is safe.** Everything is saved in files. When you come back, open the Pricing Tool folder in VS Code, open Claude Code and paste:
   ```text
   Read cu-pricing-tool-speckit-playbook.md and the files in specs/. Which step of the playbook are we on, and what's next?
   ```

## Progress checklist

Tick each box (change `[ ]` to `[x]`) as you finish it.

- [x ] Step 0: Set up the folder
- [ ] Step 1: Constitution
- [ ] Step 2: Specify
- [ ] Step 3: Clarify
- [ ] Step 4: Plan
- [ ] Step 5: Checklist
- [ ] Step 6: Tasks
- [ ] Step 7: Analyze
- [ ] Step 8: Implement (all phases, 8.1 to 8.11)
- [ ] Step 9: Converge
- [ ] Step 9b: Merge
- [ ] Part 2: Deploy to Azure (later)

## Where things stand

**Already done:** `CLAUDE.md`, this playbook, `docs\powwr` (both POWWR API files plus `README.md`), an empty GitHub repo at https://github.com/Christian-Illingworth/Pricing-Tool, and SQL Server Express installed on your laptop.

**Already decided (built into the prompts below):**
- Business types: Sole trader, Partnership, Limited company, LLP, PLC, Charity, Public sector
- Large or daily-metered gas sites are flagged for a manual quote, not priced
- "No standing charge" shows only products with no standing charge
- Meter numbers (MPAN/MPRN) are shown in full on the PDF
- Rates show to the same number of decimal places as the supplier's price
- Microbusiness limits are checked across all of a customer's sites added together (compliance to confirm)
- Start dates outside a supplier's pricing window are flagged (POWWR tells us this per supplier)
- Meter details come from POWWR's meter lookup, with manual entry as a fallback
- Reporting is admins only, analysed in Power BI
- Quotes, PDFs and reporting data are kept for 6 years, then deleted
- The Entra group is called "Pricing Tool Users"

**Waiting on other people. None of this stops you starting:**

| What | From | Needed by |
|---|---|---|
| Sign-in app registration with the Mail.ReadWrite permission | IT | Testing real sign-in and "Open in Outlook". A test sign-in works on your laptop until then |
| Pricing API licence code, Meter Lookup access, answers to the 15 questions in `docs\powwr\README.md` | POWWR | Testing with real prices. The app uses mock POWWR data until then |
| Disclosure, microbusiness and credit check wording; VAT/CCL rules; commission models; uplift limits | Compliance and finance | Entered on the app's Settings page once it's built. Placeholders until then |
| Azure subscription, domain, number of agents | You and IT | Part 2 only |

---

# Part 1: Build the app

## Step 0: Set up the folder (terminal, once, about 10 minutes)

**You need:** Git (with your name and email already set up), the .NET SDK, SQL Server Express, your empty GitHub repo, and VS Code with the **Claude Code** extension installed and signed in.

**0.1 Open the folder.** In VS Code: **File → Open Folder**, choose `C:\Users\ChristianIllingworth\Projects\Pricing Tool`. If VS Code asks whether you trust the authors, choose **Yes**.
You should see `CLAUDE.md`, `cu-pricing-tool-speckit-playbook.md` and a `docs` folder on the left.

**0.2 Open a terminal.** **Terminal → New Terminal**. The line where you type should end in `Pricing Tool>`.

Then check SQL Server Express is running:
```powershell
Get-Service MSSQL*
```
You should see `MSSQL$SQLEXPRESS` with the status `Running`. If the name after the `$` is different, that's your database's instance name: tell Claude when you get to Step 4. If it says `Stopped`, see **If something goes wrong**.

**0.3 Install uv** (the tool that installs Spec Kit):
```powershell
powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"
```
Then **close the terminal** (bin icon on the terminal panel) and **open a new one** (Terminal → New Terminal), so Windows picks up the new tool.

**0.4 Install Spec Kit and check it worked:**
```powershell
uv tool install specify-cli
specify version
```
You should see a version number. If you see *"specify is not recognized"*, run `uv tool update-shell`, close and reopen the terminal, and run `specify version` again.

**0.5 Check Git knows who you are** (you've already set this up):
```powershell
git config --global user.name
git config --global user.email
```
You should see your name and email. If either line is blank, set it with `git config --global user.name "Your Name"` or `git config --global user.email "you@example.com"`, using the email on your GitHub account.

**0.6 Install the GitHub CLI and sign in with a token that only works on this repo.** This lets Claude save to GitHub and open pull requests, with no access to anything else on your account or in the ConsultivUtilities organisation.

**a. Install it:**
```powershell
winget install --id GitHub.cli -e
```
Close the terminal and open a new one.

**b. Create the token on GitHub.** Click your profile picture (top right) → **Settings** → **Developer settings** (bottom of the left-hand menu) → **Personal access tokens** → **Fine-grained tokens** → **Generate new token**. Fill in:
- **Token name:** Pricing Tool
- **Resource owner:** Christian-Illingworth (your own account, **not** ConsultivUtilities)
- **Expiration:** 90 days (GitHub emails you before it runs out; see **If something goes wrong** for renewing)
- **Repository access:** **Only select repositories**, then choose **Pricing-Tool**
- **Permissions → Repository permissions:** set **Contents** to **Read and write**, and **Pull requests** to **Read and write**. (**Metadata: Read-only** is added automatically.) Leave everything else as **No access**.

Click **Generate token** and copy it. GitHub only shows it once. Don't paste it into the Claude chat or save it in a file.

**c. Sign in with it:**
```powershell
gh auth login
```
Answer with the arrow keys and Enter: **GitHub.com**, then **HTTPS**, then **Yes** (authenticate Git with your GitHub credentials), then **Paste an authentication token**. Right-click in the terminal to paste the token (nothing shows as you paste, which is normal), then press Enter.

**d. Check it worked:**
```powershell
gh auth status
```
You should see *Logged in to github.com account Christian-Illingworth*. If it also warns about missing scopes, ignore that: it's normal for this kind of token.

**0.7 Set up Spec Kit in this folder:**
```powershell
specify init --here --force --non-interactive --integration claude --script ps
specify extension add git
```
You should see `.specify` and `.claude` folders appear on the left. `--force` is needed because the folder already has files in it; your files are kept.

**0.8 Copy in the design rules:**
```powershell
Copy-Item "C:\Users\ChristianIllingworth\Projects\Consultiv-Utilities-Brand\cu-web-design-kit\cu-web-design-kit\DESIGN.md" .
```
You should see `DESIGN.md` appear next to `CLAUDE.md`.

**0.9 Connect the folder to your GitHub repo and save the first version:**
```powershell
git init -b main
dotnet new gitignore
Add-Content .gitignore ".claude/settings.local.json"
git remote add origin https://github.com/Christian-Illingworth/Pricing-Tool.git
git add .
git commit -m "Project setup: Spec Kit, CLAUDE.md, playbook, design rules and POWWR docs"
git push -u origin main
```
The last line should finish with *branch 'main' set up to track 'origin/main'*. Refresh your repo page on GitHub: you should see `CLAUDE.md`, the playbook, `DESIGN.md`, and the `docs`, `.specify` and `.claude` folders.

**0.10 Open Claude Code** (the Claude Code panel in VS Code; if you can't find it, press **Ctrl+Shift+P** and type "Claude Code"). In its chat box, type `/speckit` without pressing Enter.
You should see a list of Spec Kit commands (`speckit-constitution`, `speckit-specify` and so on). If you don't, start a new Claude Code conversation, or press **Ctrl+Shift+P** → **Developer: Reload Window** and try again.

✅ **Step 0 is done when** `specify version` shows a number, your files show on GitHub, and typing `/speckit` in Claude Code shows the commands.

## Step 1: Constitution (Claude Code chat)

The ground rules every later step is checked against. Paste:

```text
/speckit-constitution Create the constitution for the Consultiv Utilities pricing tool, an internal web app our sales agents use to get live business energy prices from the POWWR API and produce branded PDF quotes for customers. Principles:
1. Security first. POWWR credentials and all other secrets are never in code, the repo, app settings or the browser: they live in .NET user secrets on developer machines, and in Azure Key Vault (read through managed identity) once deployed. Only signed-in staff in our approved Microsoft Entra group can use the app.
2. Customer-safe output. Base (pre-uplift) rates and commission are for agents only. They never appear on anything a customer sees, except where the approved commission disclosure wording requires them.
3. Validate before calling out. Every input, including MPANs and MPRNs, is validated on the server (and in the form) before any POWWR request.
4. On-brand and accessible. Every screen and the customer PDF follow DESIGN.md from the Consultiv Utilities web design kit: kit classes and colour tokens only, no hard-coded colours, no other CSS framework, WCAG AA contrast, and layouts that work at the kit's 1100px and 720px breakpoints.
5. Auditable. Every quote request, its results, the uplifts applied, every approval, every PDF and Outlook draft, and every change to admin settings are stored and traceable to the person who made them.
6. One set of numbers. A single, unit-tested pricing engine does every calculation (costs, savings, commission, VAT, CCL) with full precision and one set of rounding rules, and feeds the screen, the PDF and reporting, so the figures always match. The POWWR integration sits behind an adapter with a mock, so everything can be built and tested without live credentials.
7. Local first, Azure-ready. The app runs and can be fully tested on a developer's machine with no Azure resources. Anything that differs between local and Azure (secrets, database, file storage, logging, sign-in redirect) sits behind configuration or an interface, so deploying to Azure UK South later needs no rewrite. Once deployed, all infrastructure is defined in Bicep and changed only through the CI/CD pipeline, never by hand in the portal.
8. Keep it simple. C# and .NET only (no Python or Node.js in the app's build), server-rendered pages, few dependencies, and the same stack and conventions as our existing Triage app.
9. Compliance wording is managed, not hard-coded. Customer-facing regulatory text (commission disclosure, microbusiness protections, credit check, VAT and CCL notes, and the notice that prices are indicative and subject to change) and the link to our terms and conditions come from admin-managed settings approved by compliance, and changes to them are logged. Every customer PDF carries the indicative-price notice and the T&Cs link.
```

**What happens:** Claude writes `.specify\memory\constitution.md`. (The folder is already connected to GitHub from Step 0.9, so Spec Kit skips its own Git setup.)

**Check:** open `.specify\memory\constitution.md`. It should contain the 9 principles above. If anything is missing or different, tell Claude.

✅ **Done when** the constitution lists all 9 principles.

## Step 2: Specify (Claude Code chat)

What the app does and why, with no technology. Paste:

```text
/speckit-specify Build a pricing tool for Consultiv Utilities' sales agents. We are a UK energy broker. An agent finds a business customer's meter, gets live electricity or gas prices from suppliers through POWWR's UK Pricing API (meter details come from POWWR's Meter Lookup Service; both are documented in docs/powwr/), compares them across suppliers and contract terms with our broker uplift added, selects the options to offer, downloads a branded PDF quote, and can open it in a new Outlook email to send to the customer themselves. Users are internal staff with three roles: Agent, Manager and Admin. Each quote is for one fuel.

User stories, in priority order:

P1 – Quote electricity and compare prices (one site). The agent enters:
- Customer: business name (required); business type (Sole trader / Partnership / Limited company / LLP / PLC / Charity / Public sector); company registration number (optional, for Ltd/LLP/PLC); contact name, email and phone; site address and postcode.
- Find the meter: the agent enters the site postcode, picks the address, then picks the meter. The tool fills in the meter details from POWWR's meter lookup: MPAN, profile class, meter time-switch code (MTC), line loss factor class (LLFC), standard settlement configuration (SSC), measurement class, meter type, smart meter (SMETS version), region, current supplier and annual consumption. The agent can also search by MPAN, or type everything in by hand if the lookup finds nothing or is unavailable. The tool warns if the meter is domestic, de-energised, already switching to another supplier, or has an active Green Deal plan.
- Quote type: renewal with the current supplier, new supplier, or change of tenancy (with the date the customer moved in), plus whether the customer is currently out of contract.
- Supply: MPAN top line in either format POWWR accepts, the legacy 8-digit format (profile class 2, MTC 3, LLFC 3) or the newer 9-digit format used after market-wide half-hourly settlement (MHHS); plus the individual fields some suppliers need: profile class, MTC, LLFC, SSC, DUoS tariff ID, and whether the meter has migrated to MHHS. MPAN core: 13 digits, check digit validated, with the distribution region shown from the first two digits. Half-hourly meters (profile class 00, or a half-hourly measurement class) add agreed supply capacity (kVA) and, optionally, reactive power (kVArh). Smart meter yes/no. Rate structure: taken from POWWR's rate structure lookup for the meter (e.g. Day and Night), which decides which consumption fields to show; for an MHHS-migrated meter with no SSC, the agent picks the rate structure (Standard, Economy 7, Evening and Weekend, Evening Weekend and Night, or Off-peak), and the tool notes that only some suppliers can price these. Annual consumption (kWh) per register: day, night and evening/weekend as needed (or a total plus % split), pre-filled from the lookup. Current supplier and, optionally, current unit rate(s), standing charge and contract end date.
- Contract: start date (not in the past); one or more terms (12/24/36/48/60 months); payment method (Direct Debit, BACS or Cash/Cheque; monthly or quarterly; fixed or variable; default monthly Direct Debit), because it changes which prices suppliers offer; unit rate uplift (p/kWh) and standing charge uplift (p/day), pre-filled with defaults and adjustable per supplier and term; a "no standing charge" option that shows only products with no standing charge; include or exclude specific suppliers.
- Microbusiness: the tool flags the customer as a microbusiness when annual consumption is under 100,000 kWh of electricity or 293,000 kWh of gas (consumption added up across all of the customer's sites on the quote), or when the agent confirms the business has fewer than 10 employees and turnover or balance sheet of no more than €2m. The flag shows on the results, is saved with the quote and adds the microbusiness wording to the PDF.
- VAT and CCL treatment: standard (20% VAT, CCL charged), low use (up to 33 kWh a day of electricity or 145 kWh a day of gas: 5% VAT and no CCL, detected from consumption), or charity / other qualifying use (5% VAT and CCL relief on the qualifying percentage from the customer's declaration). VAT and CCL rates and thresholds are admin settings, so finance can adjust them.
Results show, for each supplier product and term: the unit rates POWWR returns (day, night and evening/weekend, or up to six seasonal time-of-day rates) and standing charge, including uplift; for half-hourly, the capacity, excess capacity and reactive power charges; any fixed fee; any direct debit discount or cheque surcharge; estimated annual cost; saving or extra cost vs current; whether the start date is outside that supplier's pricing window; and any supplier notes. The estimated annual cost excluding VAT and CCL is POWWR's annual price, which includes every charge; the tool adds VAT and CCL using the customer's treatment when "include VAT and CCL" is switched on. Rates show exactly as POWWR returns them, and money to the penny; calculations use full precision and round only for display, identically on screen, in the PDF and in reporting. Suppliers that can't price the meter are listed with the reason POWWR gives. Agent-only columns show base rates (before uplift) and POWWR's commission figure, split into estimated upfront and monthly amounts using each supplier's payment model (set in admin settings). The agent can sort by annual cost or commission, filter by term and supplier (and by green products, if POWWR's product details identify them), change uplifts and re-price without re-entering anything, select any number of rows, and mark one as recommended. Each quote records when it was priced and POWWR's pricebook version.

P2 – Quote gas. The same flow with: MPRN (6–10 digits, validated), found by postcode lookup or typed in; annual quantity (AQ, kWh), pre-filled from the lookup; the gas region (LDZ) shown from the lookup (POWWR prices gas from the site postcode, so the agent doesn't choose it); smart meter yes/no; current gas supplier and rates; separate default uplifts for gas. Gas is single rate. Large or daily-metered gas sites (identified from the lookup's daily metered quantity and meter capacity) are not priced: the tool flags them for a manual quote. Independent gas transporter (IGT) sites are flagged, as some suppliers may not price them.

P2 – Customer PDF. From the selected rows, generate a branded A4 PDF (Consultiv Utilities branding as defined in DESIGN.md; 1–2 pages for a single site) with: our logo and contact details; customer name, contact and site; MPAN or MPRN in full; quote type, date and reference number; price validity statement; a clear notice that the prices are indicative and subject to change; comparison table of the selected options with the recommended one highlighted; saving vs current; assumptions (consumption used, payment method, VAT and CCL treatment, whether prices include them); "All prices are subject to supplier credit check and acceptance"; commission disclosure; for microbusinesses, the microbusiness wording (e.g. commission disclosure, dispute resolution scheme details such as the Energy Ombudsman, any cooling-off period); agent name, email and phone; footer with our company registration number and a link to our terms and conditions (clickable, with the full web address printed so it also works on a printed copy). No base rates or commission figures except as the disclosure requires. Filename {CustomerName}_{Electricity|Gas}Quote_{YYYY-MM-DD}.pdf. The PDF is kept with the quote. All customer-facing wording comes from admin settings, which start with clearly marked placeholder text until compliance provides the approved wording.

P2 – Open the quote in Outlook. Once the PDF is generated, the agent can choose "Open in Outlook". This saves a new, blank email with the PDF attached and nothing else filled in to the agent's Outlook Drafts folder, and opens it for them. Agents use a mix of classic and new Outlook, so it must work in both. The agent addresses, writes and sends it themselves in Outlook; the tool never sends email. Creating the Outlook draft is recorded with the quote.

P2 – Uplift controls. Admins set maximum unit rate and standing charge uplifts per fuel for each role, and optionally per agent. POWWR doesn't return supplier maximum uplifts, so admins can also set a maximum per supplier. An agent can see prices above their limit, but must request manager approval, with a reason, before generating the PDF or opening it in Outlook. Managers see pending requests on an Approvals page, approve or decline with a comment, and the agent is told the outcome. Every request and decision is recorded.

P3 – Multi-site quotes. One quote can cover several sites or meters for the same customer and fuel. The agent adds sites one at a time (with the same postcode lookup), or uploads a spreadsheet using a downloadable template; every row is validated, with errors shown per row before pricing. Each site has its own address, MPAN or MPRN, meter details, consumption and current supplier and rates. Contract settings (terms, payment method, uplifts, VAT and CCL treatment) apply to the whole quote, with optional per-site start dates. POWWR prices one meter per request, so the tool prices each site separately and combines the results. Results show, for each supplier and term, the total across all sites, flag any site that supplier can't price (with POWWR's reason), and give a per-site breakdown. The PDF shows the totals first, then a per-site breakdown.

P3 – Quote history. Agents can find past quotes, reopen them, re-price with current prices, and re-download PDFs or open them in Outlook again. Reopening a quote shows whether any supplier's prices have changed since it was priced, using POWWR's price change history. Every request, result, uplift, approval, PDF and Outlook draft is kept with who did it and when. Quotes and PDFs are kept for 6 years, then deleted automatically.

P3 – Admin settings. Admins manage: default uplifts per fuel; maximum uplifts per role, per agent and per supplier; the supplier list (each supplier's name as the Pricing API expects it, matched to the names and IDs the meter lookup returns) and each supplier's commission payment model; the default payment method; CCL rates and VAT relief thresholds, with effective-from dates; and all customer-facing wording (validity statement, indicative-price notice, credit check, commission disclosure, microbusiness wording, terms), plus the web address of our terms and conditions. Every change is logged with who, when, and the old and new values.

P3 – Management reporting. Every quote event (quote requested, prices returned, re-priced, approval requested or decided, PDF generated, opened in Outlook, failed) is recorded for reporting with: who (agent name and email) and when; our quote reference and POWWR's reference; quote type and out-of-contract flag; customer name, business type, company number and postcode; microbusiness flag; fuel; every site and meter (full MPAN with profile class, MTC, LLFC, SSC, DUoS tariff ID, MHHS status and region, or MPRN with LDZ); half-hourly, kVA and smart meter; consumption (per register or AQ); current supplier and rates; start date, terms requested, payment method, uplifts, no-standing-charge choice, and VAT and CCL treatment; every price returned (supplier, product, plan type, term, base and uplifted rates, estimated annual cost, POWWR's commission and our upfront and monthly split, pricebook version) and every no-quote reason; which options were selected and recommended; and whether a PDF was produced and opened in Outlook. Admins can view these on a Reports page, filter by agent, date range, fuel, supplier, region and quote type, see totals (quotes per agent, PDFs produced and opened in Outlook, estimated commission), and export to CSV. The same records are exported to a reporting store Power BI can read. Reporting data includes commission and customer details, so only admins can see it, and it is kept for 6 years. Recording never slows down or blocks an agent's quote.

Access: only staff in the approved group can sign in; anyone else sees a no-access page.

Edge cases: the lookup finds no address or meter, or is unavailable (manual entry still works); a domestic or de-energised meter; a meter already switching to another supplier; an active Green Deal plan; an MHHS-migrated meter with no SSC; invalid MPAN/MPRN; no prices returned; a supplier declining (show POWWR's no-quote reason); a start date outside a supplier's pricing window; a supplier pricing only some sites of a multi-site quote; POWWR errors, timeouts and rate limits; supplier prices changing before the PDF is made or while approval is pending; an uplift above the agent's, role's or supplier's limit; consumption exactly on a microbusiness or low-use threshold; a spreadsheet upload with bad rows; Outlook not available to open the draft.

Success criteria: an agent goes from a postcode to a downloaded PDF, or an Outlook email with it attached, in under 2 minutes for a single site; invalid MPANs and MPRNs are caught before any price request; customers never see base rates or commission; the figures on screen, in the PDF and in reporting always match.
```

**What happens:** Claude creates a new branch for this feature (e.g. `001-pricing-tool`). You'll see the name in the bottom-left corner of VS Code, and Claude pushes the branch to GitHub. It then writes `specs\001-…\spec.md` and `specs\001-…\checklists\requirements.md`.

**Check:** open `spec.md` and skim the user stories (P1 to P3). They should match what's above. Lines marked **[NEEDS CLARIFICATION]** are fine: Step 3 deals with them.

✅ **Done when** `spec.md` exists and a branch name like `001-…` shows bottom-left.

## Step 3: Clarify (Claude Code chat)

Claude checks the spec against the POWWR files and asks about anything unclear. Paste:

```text
/speckit-clarify Check the spec against docs/powwr/ (UK Pricing API 1.2.0 and Meter Lookup Service 2.5.0, plus the open questions in docs/powwr/README.md). Focus on: what POWWR's commission figure represents (per year, whole term or upfront); the allowed values for PlanType (including any no-standing-charge plan), BusinessType, PaymentMethod and MeterType; how long returned prices stay valid; whether any field identifies green products; what POWWR's "inclusive" annual price includes; which date field is the new contract's start date; how to get the DUoS tariff ID and MHHS status when the lookup doesn't return them; and the thresholds for flagging large or daily-metered gas sites. Record anything the docs can't answer as a question for POWWR.
```

**What happens:** Claude asks **up to 5 questions, one at a time**, usually with lettered options and a recommendation. Reply with the letter, or your own answer.

- If only POWWR can answer, reply: `Unknown. Record it as an open question for POWWR.`
- If you're not sure, reply: `Use your recommendation.`

You can run the same command again if Claude says there are more open points.

✅ **Done when** Claude says the answers are recorded in the spec.

## Step 4: Plan (Claude Code chat)

The technical design. Paste:

```text
/speckit-plan Build and run everything locally for now. Deploying to Azure is a separate feature later, so add no Azure resources or deployment work here, but keep the design Azure-ready.
- Stack: ASP.NET Core on the current .NET LTS release with server-rendered Razor Pages, matching our Triage app. If you can access github.com/ConsultivUtilities/Triage, follow its structure and conventions. No Bootstrap, jQuery, Tailwind or other CSS framework.
- Design kit: copy css/, js/, fonts/ and logo/ from C:\Users\ChristianIllingworth\Projects\Consultiv-Utilities-Brand\cu-web-design-kit\cu-web-design-kit into wwwroot/cu/ unchanged, and set them up as the kit's README describes for ASP.NET Core. App styles go in a separate stylesheet loaded after the kit, using tokens only; list any component the kit lacks so it can be added to the kit.
- Screens built from kit components: the app shell (navy sidebar with the white logo; nav: New quote, Quotes, Approvals for managers, and Reports and Settings for admins); .standalone pages for sign-in, loading and no-access; a postcode → address → meter picker at the top of the quote form, with manual entry as a fallback; a .card with a .section-heading per form section; .field / .field-label / .form-input / .form-select / .field-hint / .form-error; .seg for the fuel choice and quote type; .check-chip for terms; .switch-row for no standing charge and for showing prices with VAT and CCL; .tag for the microbusiness flag; .load-steps or .skeleton while fetching prices; results in a .table inside a .card--flush with .num figures and .filter-pill filters; .tag.tone-green for Recommended; .tag.tone-amber for "Awaiting approval"; green or coral marks plus words for savings (never coloured text); .btn-cta with .btn-count for "Generate customer PDF"; CU.toast() to confirm, with an "Open in Outlook" action; .notice.tone-coral for errors, .notice.tone-amber for warnings, .empty-state when no prices come back; .table or .split for history and approvals; .drawer for settings; Lucide icons. If HTML is swapped in after page load, keep the kit's menus and tabs working.
- Sign-in and roles: Microsoft Entra ID via Microsoft.Identity.Web, restricted to our tenant and the "Pricing Tool Users" group, with Agent, Manager and Admin app roles. Locally, use a development app registration with a localhost redirect URI. Until IT provides one, use a test sign-in that only works in the Development environment.
- Outlook: agents use a mix of classic and new Outlook. "Open in Outlook" creates a draft in the agent's own mailbox through Microsoft Graph (delegated Mail.ReadWrite, using only the create-message call) with only the PDF attached (no recipient, subject or body). Confirm with a toast ("Saved to your Outlook Drafts") and an Open button that opens the draft in Outlook on the web; the draft also appears in Drafts in both classic and new Outlook. The app never sends or reads email. Fallback, only if IT won't grant Mail.ReadWrite: download a .eml file marked as unsent (X-Unsent: 1) with the PDF attached. This works in classic Outlook, but new Outlook may open it read-only, so those agents would attach the downloaded PDF themselves. Record the draft with the quote.
- Pricing engine: one service for all calculations, built on POWWR's returned figures: annual cost excluding VAT and CCL (POWWR's RawAnnualPrice), VAT and CCL by the customer's treatment, savings, commission split into upfront and monthly by supplier payment model (from RawCommission), multi-site totals, and microbusiness and low-use checks. It also recalculates the annual cost from rates × consumption as a cross-check and logs any mismatch. Decimal arithmetic and one set of rounding rules, used by the screens, the PDF and reporting.
- Multi-site upload: CSV and Excel (.xlsx) with a downloadable template; validate every row and show errors per row before pricing.
- Approvals: requests stored with the quote; managers see them on an Approvals page; the agent sees the outcome in the app.
- Audit: every admin setting change stored with who, when, and the old and new values.
- Retention: a daily background job deletes quotes, PDFs and reporting records older than 6 years (the period is an admin setting).
- Secrets: .NET user secrets, read through configuration so Azure Key Vault can be added later without code changes.
- Data: Entity Framework Core with the SQL Server provider, on the local SQL Server Express instance (.\SQLEXPRESS, Windows sign-in, with the connection string in appsettings.Development.json or user secrets), so the same migrations work on Azure SQL Database later. A quote has one or more sites, each with one meter.
- PDFs: a Razor HTML template rendered with Playwright for .NET (headless Chromium), using the kit's Poppins fonts, colour logo (logo/cu-logo-250.png) and tokens. Ignore the older Consultiv-Utilities-Brand-Guide.md. Store PDFs behind a file-storage interface (a local folder for now, Azure Blob Storage later), downloaded only through the app.
- POWWR: two typed clients, each behind an adapter interface with a mock adapter returning realistic data, switched by configuration. Use the mocks by default.
  - UK Pricing API 1.2.0: POST /electricprices, /gasprices, /prompts (rate structure lookup) and /pricechange, and GET /ping. Authentication is a LicenceCode plus mascaradeuser in the request body's SecurityDetails: the licence code is a secret, and mascaradeuser is set to the signed-in agent's name. Send our quote reference as QuoteReference, the Settings entry ShowNoQuoteReason=true, the Renewal, COT/CotDate, OutOfContract, PaymentMethod and BusinessType fields, and per-supplier, per-term uplifts through QuoteDefinitions and Plans. Each request prices one supply, so multi-site quotes send one request per site, in parallel within POWWR's rate limits. Keep rate strings exactly as returned for display, and use the Raw* decimal fields for calculations. Production server: https://udcoreapi.co.uk/Service.svc/web. For development, use our mock or SwaggerHub's auto-mock server.
  - Meter Lookup Service 2.5.0: POST /Search/addressSearch (postcode → addresses with meter counts) and /Search/meterSearch (by meterAddressId, MPAN or MPRN → meter details). OAuth2 client credentials from POWWR's auth server (the sandbox token URL is in the docs), with our organisationPowwrId in callerIdentification and the agent's ID as userId. MPANs in address results may be redacted to the last 3 digits, so always call meterSearch for full details. Handle its RFC 7807 error responses. Normalise postcodes to the full format with a space. The lookup returns profile class and MTC as strings (e.g. "03"), while the Pricing API needs integers. Supplier names differ between the two APIs (e.g. "British Gas Business" vs "British Gas"), so map them through the admin supplier list using the lookup's powwrId or MPID. For gas, use additionalInformation.amrIndicator to suggest the smart meter flag.
  - The licence code and OAuth client secret live in user secrets locally (Key Vault later).
- Reporting: record each quote event as a versioned JSON document (with a schemaVersion field) in the database, and export it through a reporting-export interface: a local folder for now, Azure Blob Storage later. Files go under quotes/yyyy/MM/dd/, one JSON file per event. Export in the background from an outbox table, so failed writes are retried and never block the agent. The Reports page reads from the database, with filters, totals and CSV export, restricted to the Admin role.
- Logging: standard ASP.NET Core structured logging, so Application Insights can be added later.
- Tests: xUnit unit tests for MPAN/MPRN validation (including legacy 8-digit and MHHS 9-digit top lines), mapping to and from POWWR's request and response fields, the pricing engine (costs, rounding, commission models, VAT and CCL, microbusiness and low-use thresholds, multi-site totals), uplift limits and approvals, spreadsheet upload validation, and the reporting record format.
- Tooling: C# and .NET only. Don't add Python, Node.js or npm to the build; install Playwright's Chromium with its PowerShell script. The only prerequisites are the .NET SDK and SQL Server Express.
- README: prerequisites, user secrets, database setup, how to run with the mocks, SwaggerHub's mock server or live POWWR, the spreadsheet template, and the reporting record format.
```

**What happens:** this can take several minutes. Claude writes `plan.md`, `research.md`, `data-model.md`, `quickstart.md` and a `contracts` folder, all inside `specs\001-…\`.

**Check:** you don't need to understand all of it. Open `plan.md` and make sure it mentions ASP.NET Core, SQL Server Express, the design kit and mock POWWR adapters. If it mentions **Python, Node.js, npm, Bootstrap, Tailwind or creating Azure resources**, tell Claude to remove them.

✅ **Done when** `plan.md` exists and passes that check.

## Step 5: Checklist (Claude Code chat)

A review list for security and compliance. Paste:

```text
/speckit-checklist Security and customer-facing compliance: secrets handling, access control and roles, uplift limits and approvals, microbusiness and credit check wording, customer and commission data in reporting exports, and making sure base rates and commission never reach a customer.
```

**What happens:** Claude writes a new file in `specs\001-…\checklists\`.

**Then you review it:** open the file and read each item.
- If the spec covers it, change its `[ ]` to `[x]`.
- If it doesn't, tell Claude: `Checklist item "<paste the item>" isn't covered. Update the spec.` Then tick it.

This matters because Step 8 stops and asks you about any unticked items.

✅ **Done when** every item is ticked, or you've deliberately left one open.

## Step 6: Tasks (Claude Code chat)

Turns the plan into a numbered to-do list. Paste:

```text
/speckit-tasks Include unit tests for validation, the pricing engine, uplift limits, approvals and spreadsheet uploads. Put sign-in, roles, the app shell and the pricing engine in the Foundational phase. No Azure resources or deployment tasks in this feature.
```

**What happens:** Claude writes `specs\001-…\tasks.md`, with tasks numbered T001, T002 and so on, grouped into phases: **Setup**, **Foundational**, one phase per user story, then **Polish**.

**Check:** the phases are there in that order, and there are no Azure tasks.

✅ **Done when** `tasks.md` exists with those phases.

## Step 7: Analyze (Claude Code chat)

A read-only consistency check. Nothing is changed. Paste:

```text
/speckit-analyze
```

**What happens:** Claude shows a table of issues, each marked **CRITICAL**, **HIGH**, **MEDIUM** or **LOW**.

- If there are any CRITICAL or HIGH issues, paste this, then run `/speckit-analyze` again:
  ```text
  Fix the CRITICAL and HIGH issues from the analysis in the spec, plan and tasks.
  ```
- MEDIUM and LOW issues are optional.

✅ **Done when** there are no CRITICAL issues left.

## Step 8: Implement, one phase at a time (Claude Code chat)

Claude writes the code, one phase at a time.

**Before each phase:** type `/clear` in the Claude Code chat to start a fresh conversation. Everything important is in the files, and a fresh start keeps Claude accurate.

**After each phase:**
1. Claude tells you how to run the app, usually a command for the terminal and then a `https://localhost:…` link to open in your browser. Try what it suggests.
2. If something doesn't work, paste the error or describe what you saw, e.g. `The results page is empty after I click Get prices.`
3. Claude saves (commits) the phase to the feature branch and pushes it to GitHub, as `CLAUDE.md` tells it to, so your work is backed up.
4. Tick the phase off below.

**Storing secrets** (when POWWR sends the licence code or login details): paste `Give me the command to store the POWWR licence code as a .NET user secret.` into the chat. Then run the command it gives you **in the terminal**, typing the real code where it shows a placeholder. Never paste the real code into the chat.

**The phases, in order:**

- [ ] **8.1 Setup and Foundational** (project, sign-in, app shell, pricing engine):
  ```text
  /speckit-implement Only the Setup and Foundational phases, then stop and tell me how to run it on my machine and what to test.
  ```
- [ ] **8.2 Electricity quotes:**
  ```text
  /speckit-implement Only the phase for the electricity quotes user story, then stop and tell me how to test it.
  ```
- [ ] **8.3 Gas quotes:**
  ```text
  /speckit-implement Only the phase for the gas quotes user story, then stop and tell me how to test it.
  ```
- [ ] **8.4 Customer PDF:**
  ```text
  /speckit-implement Only the phase for the customer PDF user story, then stop and tell me how to test it.
  ```
- [ ] **8.5 Open in Outlook:**
  ```text
  /speckit-implement Only the phase for the Open in Outlook user story, then stop and tell me how to test it.
  ```
- [ ] **8.6 Uplift controls and approvals:**
  ```text
  /speckit-implement Only the phase for the uplift controls user story, then stop and tell me how to test it.
  ```
- [ ] **8.7 Multi-site quotes:**
  ```text
  /speckit-implement Only the phase for the multi-site quotes user story, then stop and tell me how to test it.
  ```
- [ ] **8.8 Quote history:**
  ```text
  /speckit-implement Only the phase for the quote history user story, then stop and tell me how to test it.
  ```
- [ ] **8.9 Admin settings:**
  ```text
  /speckit-implement Only the phase for the admin settings user story, then stop and tell me how to test it.
  ```
- [ ] **8.10 Management reporting:**
  ```text
  /speckit-implement Only the phase for the management reporting user story, then stop and tell me how to test it.
  ```
- [ ] **8.11 Polish:**
  ```text
  /speckit-implement Only the Polish phase, then stop and tell me what changed.
  ```

✅ **Each phase is done when** you've tried it and it works with the mock POWWR data.

## Step 9: Converge (Claude Code chat)

Checks the finished code against the spec, plan and tasks. Type `/clear` first, then paste:

```text
/speckit-converge
```

**What happens:** either Claude reports **Converged** (finished), or it adds new tasks to the end of `tasks.md`.

**If it added tasks:** type `/clear`, paste the command below, then run `/speckit-converge` again. Repeat until it says **Converged**.
```text
/speckit-implement Only the tasks that converge just added, then stop and tell me how to test them.
```

✅ **Done when** Claude reports **Converged**.

## Step 9b: Merge the feature (Claude Code chat)

Paste:

```text
The feature has converged. Check it's complete as CLAUDE.md describes, then merge it into main.
```

**What happens:** Claude builds the app and runs the tests, opens a pull request on GitHub, shows you its link and a summary, and **asks you to confirm**. Reply `Yes, merge it.` Claude merges the pull request and deletes the feature branch. Afterwards, the bottom-left corner of VS Code shows `main`, and the merged pull request is listed on GitHub under **Pull requests → Closed**.

✅ **Part 1 is complete.**

---

# Part 2: Deploy to Azure (later)

**Only start when** Part 1 is merged, and IT has agreed the Azure subscription. Part 2 creates real Azure resources, which cost money. It follows the same pattern as Part 1, and the constitution stays the same. Type `/clear` before each step.

The deployment pipeline runs on **GitHub Actions**, because the code is on GitHub. Before Step 12, edit your **Pricing Tool** token on GitHub and set **Workflows** to **Read and write**, so Claude can save the pipeline file.

## Step 10: Specify the deployment (Claude Code chat)

```text
/speckit-specify Make the pricing tool available to all our sales agents by hosting it in our Azure subscription. Agents open it at [quotes.consultivutilities.com] and sign in with their Microsoft 365 accounts, and can open quotes in Outlook. POWWR credentials and other secrets are protected and never visible in the code or to staff. Quotes and PDFs are stored durably, with backups. Quote reporting data is exported to Azure Blob Storage, where Power BI can read it; only admins can access it, and it is deleted after 6 years. We can see errors, slow pages and POWWR failures, and get alerted to them. Every release goes to a staging copy first and is promoted to production only once checked, and a bad release can be rolled back. The whole environment can be rebuilt from the repository. Success: agents use the hosted app with all the features they had locally, and we know the monthly running cost for [number] agents.
```

**What happens:** a new branch is created (e.g. `002-…`) with its own `spec.md`.

## Step 10b: Clarify the deployment (Claude Code chat)

```text
/speckit-clarify Ask me for anything still in [brackets] in this spec and in the Step 11 plan: the domain, the number of agents, and whether POWWR allowlists IP addresses.
```

Answer Claude's questions as in Step 3.

## Step 11: Plan the Azure setup (Claude Code chat)

Paste this exactly as it is. Claude fills in anything still in [square brackets] from your Step 10b answers.

```text
/speckit-plan
- Hosting: a Dockerfile for the app (including Chromium for PDF rendering), hosted on Azure App Service for Linux in UK South [or Azure Container Apps, with reasons]. Not Static Web Apps.
- Secrets: Azure Key Vault through managed identity, plugged into the existing configuration.
- Data: Azure SQL Database, using the existing EF Core migrations.
- Files: a private Azure Blob Storage container behind the existing file-storage interface. A lifecycle rule deletes PDFs after 6 years.
- Reporting: a separate private `reporting` container, plugged into the existing reporting-export interface, in a storage account with hierarchical namespace (Data Lake Storage Gen2) so Power BI can read the folders directly. A lifecycle rule deletes data after 6 years. Read access for admins and Power BI through Entra role assignments: no shared keys or public links.
- Sign-in and Outlook: a production Entra app registration with the hosted redirect URI, the Agent, Manager and Admin app roles, and the Mail.ReadWrite delegated permission.
- Monitoring: Application Insights, including POWWR response times and failures, with alerts.
- Infrastructure: Bicep for every resource; a GitHub Actions pipeline in the github.com/Christian-Illingworth/Pricing-Tool repo that builds, tests and deploys to a staging slot, then swaps to production. It signs in to Azure with OpenID Connect (federated credentials), so no Azure passwords or keys are stored in GitHub.
- Networking: [if POWWR allowlists IPs, a static outbound IP via a NAT gateway; tell me the IP to register].
- Domain: [quotes.consultivutilities.com] with a managed certificate.
- README: provisioning, deployment, rollback, and the estimated monthly Azure cost.
```

## Step 12: Build and finish the deployment (Claude Code chat)

Do these in order, exactly as in Part 1:

1. `/speckit-tasks`, as in Step 6.
2. `/speckit-analyze`, as in Step 7.
3. `/speckit-implement`, one phase at a time, as in Step 8, starting with: `/speckit-implement Only the Setup and Foundational phases, then stop and tell me what to check.`
4. `/speckit-converge`, as in Step 9.
5. Merge, as in Step 9b.

---

# Adding features later

Keep the constitution. Start from **Step 2** with a new `/speckit-specify` describing the new feature, then follow Steps 3 to 9b. Each feature gets its own branch automatically, and is merged into `main` when complete.

---

# If something goes wrong

| Problem | What to do |
|---|---|
| *"specify is not recognized"* in the terminal | Run `uv tool update-shell`, close and reopen the terminal, try again. |
| `/speckit-…` commands don't appear in Claude Code | Start a new Claude Code conversation, or **Ctrl+Shift+P** → **Developer: Reload Window**. Check the `.claude` folder exists; if not, redo Step 0.7. |
| You pasted a `/speckit-…` command into the terminal | Nothing breaks. Paste it into the Claude Code chat instead. |
| Git says *"Please tell me who you are"* | Do Step 0.5. |
| `winget` isn't recognised or is blocked | Download the GitHub CLI from https://cli.github.com, or ask IT to install it. Then carry on from `gh auth login`. |
| `dotnet new gitignore` says the file already exists | Skip that line and carry on. |
| Step 0.9 says *"remote origin already exists"* | That line's already done. Carry on with the next one. |
| `git push` or `gh` asks you to sign in, or says *permission denied* or *Bad credentials* | Your token has probably expired or is missing a permission. On GitHub, go to **Settings → Developer settings → Personal access tokens → Fine-grained tokens → Pricing Tool**, click **Regenerate token**, check it still has Pricing-Tool with Contents and Pull requests set to Read and write, copy it, then redo Step 0.6c. |
| Claude asks to do something you don't understand | Ask `Why do you need to do that?` before allowing it. |
| Claude starts doing things that aren't in the current phase | Paste: `Stop. Only do what's in tasks.md for the current phase.` |
| A build or test fails | Paste the error into the chat: `This failed: <error>. Fix it.` |
| Step 8 stops halfway or the chat gets cut off | Type `/clear` and paste the same `/speckit-implement` command again. Finished tasks are ticked `[X]` in `tasks.md` and are skipped. |
| Step 8 says checklist items are unticked | Go back to Step 5 and tick the ones you're happy with. |
| Database errors, or SQL Server Express isn't running | In the terminal run `Get-Service MSSQL*`. If `MSSQL$SQLEXPRESS` says `Stopped`, open the Windows **Services** app, find **SQL Server (SQLEXPRESS)**, right-click it and choose **Start**. If that needs admin rights, ask IT. |
| You want to change something already built | Describe the change in the chat. If it's a new requirement, Claude will suggest updating the spec first. That's expected. |
| You've lost track of where you are | Type `/clear`, then paste the "taking a break" message from **Read this first**. |

