# Feature Specification: CU Pricing Tool

**Feature Branch**: `001-pricing-tool`

**Created**: 2026-10-30

**Status**: Draft

**Input**: User description: "Build a pricing tool for Consultiv Utilities' sales agents. We are a UK energy broker. An agent finds a business customer's meter, gets live electricity or gas prices from suppliers through POWWR's UK Pricing API … compares them across suppliers and contract terms with our broker uplift added, selects the options to offer, downloads a branded PDF quote, and can open it in a new Outlook email to send to the customer themselves." (Full description: Step 2 of `cu-pricing-tool-speckit-playbook.md`.)

## Clarifications

### Session 2026-10-30

- Q: When the meter lookup doesn't return the DUoS tariff ID or MHHS status, how should the tool fill
  them in? → A: Derive them from a 9-character (post-MHHS) top line when one is given, treating that
  format as MHHS-migrated (POWWR to confirm). Otherwise the agent may enter them, from a bill for
  example, but they are optional; suppliers that need them show POWWR's no-quote reason.
- Q: Until POWWR confirms, what should the tool treat POWWR's commission figure as? → A: An admin
  setting says whether it is per year or for the whole contract, starting as per year. Each pricing
  records the basis it used, so earlier quotes stay correct if the setting changes.
- Q: How should the tool decide that a gas site is too large to price and needs a manual quote? → A:
  Annual quantity (AQ), meter capacity and daily metered quantity each have an optional admin-set
  limit. A site is flagged if it exceeds any of them, and a blank limit is not checked. Values are set
  once POWWR advises.
- Q: How long should a quote's prices count as valid, given POWWR gives no expiry? → A: Until the end
  of the day they were fetched (UK time). After that, a re-price is required before any new PDF or
  re-sending in Outlook.
- Q: Should POWWR's free-text supplier notes appear on the customer PDF? → A: No. They are shown to
  the agent on the results only, until compliance has reviewed what suppliers send.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Quote electricity and compare prices for one site (Priority: P1)

An agent finds a business customer's electricity meter from the site postcode (or types the
details in), enters the customer, supply and contract details, and gets live prices from every
supplier POWWR can price, with our uplift added. They compare suppliers and terms, see their
commission, adjust uplifts and re-price, then select the options to offer and mark one as
recommended.

**Why this priority**: this is the core job of the tool. Without it nothing else has value, and on
its own it already replaces looking prices up by hand.

**Independent Test**: sign in as an agent, price a single electricity site from a postcode, and
check the results, agent-only columns, sorting, filtering, re-pricing and selection, without any
PDF, Outlook, approval or admin features.

**Acceptance Scenarios**:

1. **Given** a signed-in agent starting a new electricity quote, **When** they enter the site
   postcode, pick the address and then the meter, **Then** the meter details are filled in from the
   meter lookup for the agent to review.
2. **Given** the meter lookup finds nothing or is unavailable, **When** the agent types the meter
   details in by hand, **Then** they can complete and price the quote.
3. **Given** an MPAN core with a wrong check digit, **When** the agent asks for prices, **Then** the
   field shows a clear error and no price request is sent.
4. **Given** valid details, **When** the agent asks for prices, **Then** every supplier product and
   term is shown with its rates including uplift, estimated annual cost, saving or extra cost against
   current, pricing-window flag and supplier notes, and every supplier that could not price the meter
   is listed with POWWR's reason.
5. **Given** results on screen, **When** the agent views them, **Then** agent-only columns show the
   base rates before uplift and the commission split into estimated upfront and monthly amounts, and
   the agent can sort by annual cost or commission and filter by term and supplier.
6. **Given** results on screen, **When** the agent changes the uplift for one supplier and term and
   re-prices, **Then** new prices appear without re-entering anything, and the earlier pricing is
   kept with the quote.
7. **Given** a site using 90,000 kWh a year, **When** it is priced, **Then** the customer is flagged
   as a microbusiness on the results and the flag is saved with the quote.
8. **Given** a site using 30 kWh a day on average, **When** "include VAT and CCL" is switched on,
   **Then** the low-use treatment is applied: 5% VAT and no CCL.
9. **Given** a meter the lookup reports as domestic, de-energised, already switching supplier or
   with an active Green Deal plan, **When** it is chosen, **Then** the agent sees a warning, which is
   saved with the quote; for a domestic meter the agent must confirm before pricing.
10. **Given** results on screen, **When** the agent selects several options and marks one as
    recommended, **Then** the quote is saved with those choices, the time it was priced and POWWR's
    pricebook version.

---

### User Story 2 - Quote gas (Priority: P2)

The same flow as electricity, for a gas meter identified by its MPRN.

**Why this priority**: gas is a large share of our business, but the electricity flow proves the
approach first, and most of it is reused.

**Independent Test**: price a single gas site from a postcode and from a typed MPRN, and check
the large-site and IGT flags.

**Acceptance Scenarios**:

1. **Given** a new gas quote, **When** the agent picks a meter from the postcode lookup, **Then** the
   MPRN, annual quantity (AQ), gas region (LDZ), current supplier and smart meter suggestion are
   filled in.
2. **Given** an MPRN that is not 6–10 digits, **When** the agent asks for prices, **Then** the field
   shows an error and no price request is sent.
3. **Given** a gas site whose AQ, meter capacity or daily metered quantity is above its admin-set
   limit, **When** it is chosen, **Then** the tool flags it for a manual quote and does not price it.
4. **Given** a site on an independent gas transporter (IGT) network, **When** it is chosen, **Then**
   the tool flags that some suppliers may not price it, and still prices it.
5. **Given** a new gas quote, **When** the contract section opens, **Then** the uplifts are pre-filled
   with the gas defaults, not the electricity ones.

---

### User Story 3 - Customer PDF quote (Priority: P2)

From the selected options, the agent generates a branded PDF quote to give the customer.

**Why this priority**: the PDF is what the customer receives. It turns a price comparison into a
quote we can send.

**Independent Test**: from a priced quote with selected options, generate the PDF and check its
content, branding, file name and that it contains no agent-only figures.

**Acceptance Scenarios**:

1. **Given** a priced quote with options selected and one recommended, **When** the agent generates
   the PDF, **Then** an A4 PDF in Consultiv Utilities branding is produced with all the content
   listed in FR-047, the recommended option highlighted, and the file name
   `{CustomerName}_{Electricity|Gas}Quote_{YYYY-MM-DD}.pdf`.
2. **Given** a generated PDF, **When** its content is checked, **Then** it contains no base rates,
   uplift amounts or commission figures, except where the approved commission disclosure wording
   includes them.
3. **Given** a microbusiness customer, **When** the PDF is generated, **Then** it includes the
   microbusiness wording; for a customer that is not a microbusiness, it does not.
4. **Given** the compliance wording has not been approved yet, **When** the PDF is generated,
   **Then** the placeholder text is visibly marked as a placeholder.
5. **Given** a generated PDF, **When** it is opened, **Then** it states that the prices are
   indicative and subject to change, and its footer has a clickable link to our terms and conditions
   with the full web address printed.
6. **Given** a generated PDF, **When** its figures are compared with the screen, **Then** every
   figure matches to the penny.
7. **Given** a supplier has published new prices since the quote was priced, **When** the agent
   tries to generate the PDF, **Then** the tool tells them which suppliers have changed and asks them
   to re-price first.
8. **Given** a quote priced yesterday, **When** the agent tries to generate a PDF, **Then** the tool
   says the prices have expired and requires a re-price first.
9. **Given** a generated PDF, **When** it is opened, **Then** its validity statement shows the date
   the prices were fetched and says they are valid until the end of that day.

---

### User Story 4 - Open the quote in Outlook (Priority: P2)

Once the PDF exists, the agent opens it in a new, blank Outlook email to address and send
themselves.

**Why this priority**: it saves the agent downloading and attaching the PDF by hand, and keeps
sending in the agent's own mailbox.

**Independent Test**: generate a PDF, choose "Open in Outlook", and check the agent's Drafts folder
in both classic and new Outlook.

**Acceptance Scenarios**:

1. **Given** a generated PDF, **When** the agent chooses "Open in Outlook", **Then** a new email with
   only the PDF attached, and no recipient, subject or body, is saved in the agent's own Drafts folder
   and opened for them, and the tool confirms it was saved.
2. **Given** the draft has been created, **When** the agent looks in Outlook, **Then** the draft
   appears in Drafts in both classic and new Outlook, and nothing has been sent.
3. **Given** Outlook cannot be reached, **When** the agent chooses "Open in Outlook", **Then** they see
   a clear message and can still download the PDF.
4. **Given** a draft was created, **When** the quote is viewed later, **Then** it shows who created
   the draft and when.

---

### User Story 5 - Uplift limits and manager approval (Priority: P2)

Admins set maximum uplifts. An agent can see prices above their limit, but needs a manager's
approval before giving those prices to a customer.

**Why this priority**: it protects customers from excessive margins and protects our reputation,
which matters before quotes go to customers in volume.

**Independent Test**: set a low limit for the Agent role, price above it, request approval, and
approve and decline it as a manager.

**Acceptance Scenarios**:

1. **Given** an agent prices with an uplift above their limit, **When** the results show, **Then** the
   affected options are marked as needing approval, and the PDF and "Open in Outlook" are unavailable
   for them.
2. **Given** options needing approval, **When** the agent requests approval with a reason, **Then** the
   request appears on the managers' Approvals page.
3. **Given** a pending request, **When** a manager approves it with a comment, **Then** the agent sees
   the outcome in the tool and can generate the PDF.
4. **Given** a pending request, **When** a manager declines it with a comment, **Then** the agent sees
   the outcome and the comment, and must lower the uplift to continue.
5. **Given** an approved request, **When** the agent raises the uplift above the approved value,
   **Then** a new approval is needed.
6. **Given** a manager's own quote needs approval, **When** they view the Approvals page, **Then** they
   cannot approve their own request.
7. **Given** an uplift above a supplier's maximum, **When** the results show, **Then** the affected
   options are marked as above the supplier's maximum, cannot be approved by anyone, and can only be
   offered once the agent lowers the uplift to the maximum or below.

---

### User Story 6 - Multi-site quotes (Priority: P3)

One quote covers several sites or meters for the same customer and fuel, added one at a time or
uploaded from a spreadsheet.

**Why this priority**: multi-site customers are valuable but less frequent, and the single-site
flow must work first.

**Independent Test**: upload the template with good and bad rows, fix the errors, price the quote,
and check the totals, per-site breakdown and PDF.

**Acceptance Scenarios**:

1. **Given** a quote, **When** the agent adds sites one at a time, **Then** each site uses the same
   postcode lookup and has its own meter, consumption and current supplier details.
2. **Given** the downloadable template, **When** the agent uploads it with some invalid rows, **Then**
   every invalid row is listed with its errors, and nothing is priced until those rows are fixed or
   removed.
3. **Given** a valid multi-site quote, **When** it is priced, **Then** results show, for each supplier
   and term, the total across all sites and a per-site breakdown.
4. **Given** a supplier that cannot price some of the sites, **When** results show, **Then** those
   sites are flagged with POWWR's reason and that supplier's total is marked as partial.
5. **Given** several small sites that add up to more than the microbusiness threshold, **When** the
   quote is priced, **Then** the microbusiness test uses the combined consumption.
6. **Given** a priced multi-site quote, **When** the PDF is generated, **Then** it shows the totals
   first, then a per-site breakdown.

---

### User Story 7 - Quote history (Priority: P3)

Agents find past quotes, reopen them, re-price with current prices, re-download PDFs, and open a
PDF in Outlook again while its prices are still valid.

**Why this priority**: valuable for follow-ups and renewals, but the tool is usable without it.

**Independent Test**: create quotes, find one by customer name and by meter number, reopen it,
re-price it, and re-download its PDF.

**Acceptance Scenarios**:

1. **Given** saved quotes, **When** the agent searches by customer name, quote reference, MPAN or
   MPRN, postcode or date range, **Then** matching quotes are listed.
2. **Given** a quote priced last month, **When** the agent reopens it, **Then** the tool shows whether
   any supplier's prices have changed since it was priced.
3. **Given** a reopened quote, **When** the agent re-prices it, **Then** new prices are fetched with
   the same details, and the earlier pricing and PDFs stay with the quote.
4. **Given** a quote with a PDF, **When** the agent re-downloads it, **Then** they get the same PDF
   that was generated before, showing the date its prices were valid.
5. **Given** a PDF whose prices were fetched on an earlier day, **When** the agent chooses "Open in
   Outlook", **Then** the tool requires a re-price and a new PDF first.
6. **Given** any quote, **When** it is opened, **Then** its history shows every request, result,
   uplift, approval, PDF and Outlook draft with who did it and when.
7. **Given** a quote older than the retention period, **When** the daily clean-up runs, **Then** the
   quote, its PDFs and its reporting records are deleted.

---

### User Story 8 - Admin settings (Priority: P3)

Admins manage the defaults, limits, supplier list, tax rates and customer-facing wording the tool
uses.

**Why this priority**: until it exists, the tool runs on starting values and placeholder wording,
which is enough to build and test the other stories.

**Independent Test**: as an admin, change a default uplift, a CCL rate with an effective date and
the commission disclosure, then check new quotes and PDFs use them and the change log shows each
change.

**Acceptance Scenarios**:

1. **Given** an admin changes the default electricity unit-rate uplift, **When** an agent starts a
   new electricity quote, **Then** the new default is pre-filled, and the change log shows who changed
   it, when, and the old and new values.
2. **Given** an admin updates the commission disclosure, **When** a new PDF is generated, **Then** it
   uses the new wording, while PDFs generated earlier are unchanged and record the version they used.
3. **Given** a new CCL rate with a future effective-from date, **When** a quote with a start date on
   or after that date is priced, **Then** the new rate is used; earlier start dates use the old rate.
4. **Given** the meter lookup returns a supplier name that differs from POWWR's pricing name,
   **When** the admin maps it in the supplier list, **Then** the current supplier is recognised on new
   quotes.
5. **Given** an agent or manager without the Admin role, **When** they try to open the settings,
   **Then** they are refused.

---

### User Story 9 - Management reporting (Priority: P3)

Every quote event is recorded for reporting. Admins view, filter and export it, and the same
records feed a reporting store that Power BI can read.

**Why this priority**: important for managing the sales team and commission, but it does not stop
agents quoting.

**Independent Test**: create, price, approve, generate and open several quotes, then check the
Reports page totals, filters and CSV export, and the records in the reporting store.

**Acceptance Scenarios**:

1. **Given** an agent prices a quote, **When** an admin opens the Reports page, **Then** the event
   appears with all the details listed in FR-084.
2. **Given** reporting data, **When** an admin filters by agent, date range, fuel, supplier, region
   and quote type, **Then** the list and totals update, and the admin can export the result to CSV.
3. **Given** an agent or manager without the Admin role, **When** they try to open reports, **Then**
   they are refused.
4. **Given** the reporting store is unavailable, **When** an agent prices and generates a PDF,
   **Then** the agent is not slowed down or blocked, and the records reach the store once it is
   available again.

---

### Edge Cases

- **Lookup finds no address or meter, or is unavailable:** the agent is told and can enter
  everything by hand; quoting is never blocked by the lookup.
- **The address has both gas and electricity meters:** only meters for the quote's fuel are offered.
- **Domestic or de-energised meter:** warning shown and saved; a domestic meter needs the agent's
  confirmation before pricing.
- **Meter already switching to another supplier, or an active Green Deal plan:** warning shown and
  saved with the quote.
- **MHHS-migrated meter with no SSC:** the agent picks the rate structure, and the tool notes that
  only some suppliers can price it.
- **Invalid MPAN or MPRN:** field-level error; no price request is sent.
- **MPAN top line and individual fields disagree** (for example a different profile class): error
  until they match.
- **No prices returned:** an empty-results message listing each supplier's reason where POWWR gives
  one.
- **A supplier declines:** listed with POWWR's no-quote reason.
- **Start date outside a supplier's pricing window:** the affected options are flagged; the agent
  can still select them, and the flag stays visible to the agent.
- **A supplier prices only some sites of a multi-site quote:** missing sites flagged with the reason;
  the supplier's total is marked as partial, and a PDF with that option lists the sites it does not
  cover.
- **POWWR errors, timeouts and rate limits:** a clear message, everything entered is kept, and the
  agent can retry; for a multi-site quote, only the failed sites are retried.
- **Supplier prices change before the PDF is made:** the agent is told which suppliers changed and
  must re-price before generating the PDF.
- **Prices fetched on an earlier day:** the prices have expired. A re-price is needed before a new PDF
  or an Outlook draft; stored PDFs can still be downloaded for reference.
- **Approval given on a later day than pricing:** the approval still covers the uplift values, but the
  agent must re-price before the PDF because the prices have expired.
- **Supplier prices change while approval is pending:** the approval still covers the uplift values;
  the agent re-prices before the PDF, and needs a new approval only if the uplift goes above the
  approved values.
- **Uplift above the agent's, role's or supplier's limit:** see User Story 5 and FR-058 to FR-066.
- **Consumption exactly on a threshold:** low use applies at exactly 33 kWh a day of electricity or
  145 kWh a day of gas; a microbusiness is flagged at exactly 100,000 kWh of electricity or
  293,000 kWh of gas (FR-027).
- **Spreadsheet upload with bad rows:** every bad row listed with its errors; nothing priced until
  fixed or removed. A meter number repeated in the same quote, or a row for the other fuel, is an
  error.
- **Outlook not available to open the draft:** a clear message; the PDF can still be downloaded.
- **Customer name with characters not allowed in file names:** those characters are removed or
  replaced in the PDF file name.
- **Compliance wording not yet approved:** placeholders are visibly marked on the PDF.

## Requirements *(mandatory)*

### Functional Requirements

**Access and roles**

- **FR-001**: Only signed-in staff in the approved staff group MUST be able to use the tool. Anyone
  else MUST see a no-access page and no data.
- **FR-002**: The tool MUST support three roles. **Agent**: create, price and manage quotes.
  **Manager**: everything an Agent can do, plus approve or decline uplift requests. **Admin**: manage
  settings and view reports. A person MAY hold more than one role.
- **FR-003**: Agents MUST see only their own quotes. Managers and Admins MUST be able to see all
  quotes.
- **FR-004**: Base rates and commission MUST be visible to every signed-in role inside the tool, and
  MUST NOT appear on anything a customer sees (see FR-049).

**Customer and meter**

- **FR-005**: The agent MUST enter the business name, which is required, and choose the business type
  from: Sole trader, Partnership, Limited company, LLP, PLC, Charity, Public sector.
- **FR-006**: The tool MUST accept an optional company registration number only for Limited company,
  LLP and PLC. When given, it MUST be 8 characters: 8 digits, or 2 letters followed by 6 digits.
- **FR-007**: The tool MUST capture the contact name, email address and phone number, and the site
  address and postcode, and MUST check that the email address, UK phone number and UK postcode are
  in a valid format.
- **FR-008**: The agent MUST be able to find a meter by entering the site postcode, picking the
  address, then picking the meter. Postcodes MUST be checked and normalised to the full format with a
  space before the lookup.
- **FR-009**: The agent MUST also be able to search directly by MPAN (electricity) or MPRN (gas).
- **FR-010**: After a meter is picked, the tool MUST fill in the details the lookup provides. For
  electricity: MPAN, profile class, MTC, LLFC, SSC, measurement class, meter type, smart meter
  (SMETS version), region, current supplier and annual consumption. For gas: the details in FR-043.
  The tool MUST show the full meter number even when the address list shows it partly hidden.
- **FR-011**: The agent MUST be able to review and change pre-filled values, or enter everything by
  hand when the lookup finds nothing or is unavailable. The tool MUST record which values came from
  the lookup and which the agent entered or changed.
- **FR-012**: The tool MUST warn the agent, and save the warning with the quote, when the meter is
  domestic, de-energised, already switching to another supplier, or has an active Green Deal plan.
  For a domestic meter, the agent MUST confirm before pricing.

**Quote type**

- **FR-013**: The agent MUST choose the quote type: renewal with the current supplier, new supplier,
  or change of tenancy. Change of tenancy MUST require the date the customer moved in.
- **FR-014**: The agent MUST record whether the customer is currently out of contract.

**Electricity supply**

- **FR-015**: The tool MUST accept the MPAN top line in either the legacy 8-digit format (profile
  class 2 digits, MTC 3, LLFC 3) or the 9-digit format used after market-wide half-hourly settlement
  (MHHS).
- **FR-016**: The tool MUST capture the individual supply fields some suppliers need: profile class,
  MTC, LLFC, SSC, DUoS tariff ID and whether the meter has migrated to MHHS. Values that appear in both
  the top line and the individual fields MUST match. When the top line is in the 9-digit (post-MHHS)
  format, the tool MUST take the SSC and DUoS tariff ID from it and record the meter as
  MHHS-migrated. Otherwise the DUoS tariff ID and MHHS status MUST be optional: the agent MAY enter
  them, and a supplier that needs them and cannot price without them is shown with POWWR's no-quote
  reason.
- **FR-017**: The MPAN core MUST be exactly 13 digits with a valid check digit. The tool MUST show the
  distribution region from its first two digits.
- **FR-018**: For half-hourly meters (profile class 00, or a half-hourly measurement class), the tool
  MUST require the agreed supply capacity (kVA) and MUST allow reactive power (kVArh).
- **FR-019**: The tool MUST record whether the meter is a smart meter.
- **FR-020**: The tool MUST take the rate structure (for example Day and Night) from POWWR's rate
  structure lookup for the meter and show only the matching consumption fields.
- **FR-021**: For an MHHS-migrated meter with no SSC, the agent MUST pick the rate structure from:
  Standard, Economy 7, Evening and Weekend, Evening Weekend and Night, or Off-peak. The tool MUST note
  that only some suppliers can price these.
- **FR-022**: The tool MUST capture annual consumption in kWh for each register (day, night and
  evening/weekend as needed), or a total plus a percentage split that adds up to 100%, pre-filled
  from the lookup. Consumption MUST be greater than zero.
- **FR-023**: The tool MUST capture the current supplier and, optionally, the current unit rate(s),
  standing charge and contract end date.

**Contract**

- **FR-024**: The agent MUST set the contract start date (not in the past); one or more terms from
  12, 24, 36, 48 and 60 months; and the payment method (Direct Debit, BACS or Cash/Cheque; monthly or
  quarterly; fixed or variable), defaulting to monthly Direct Debit. The agent MUST also be able to
  include or exclude specific suppliers.
- **FR-025**: The tool MUST pre-fill the unit-rate uplift (p/kWh) and standing-charge uplift (p/day)
  with the defaults for the quote's fuel, and the agent MUST be able to change them for each supplier
  and term. Uplifts MUST NOT be negative.
- **FR-026**: A "no standing charge" option MUST show only products with no standing charge.

**Microbusiness and VAT and CCL treatment**

- **FR-027**: The tool MUST flag the customer as a microbusiness when the quote's annual consumption,
  added up across all its sites, is at or below 100,000 kWh of electricity or 293,000 kWh of gas
  (exactly at the threshold counts), or when the agent confirms the business has fewer than 10 employees and a turnover
  or balance sheet of no more than €2m. The thresholds MUST be admin settings.
- **FR-028**: The microbusiness flag MUST show on the results, MUST be saved with the quote and MUST
  add the microbusiness wording to the PDF. The agent MUST NOT be able to remove a flag that comes
  from consumption.
- **FR-029**: The tool MUST support three VAT and CCL treatments. **Standard**: 20% VAT and CCL
  charged. **Low use**: at or below 33 kWh a day of electricity or 145 kWh a day of gas, worked out per
  site from annual consumption, giving 5% VAT and no CCL. **Charity or other qualifying use**: 5% VAT
  and CCL relief on the qualifying percentage from the customer's declaration, which the agent enters.
- **FR-030**: Low use MUST be detected from consumption; the agent MUST NOT be able to choose it for a
  site above the threshold.
- **FR-031**: VAT rates, CCL rates and the low-use thresholds MUST be admin settings with
  effective-from dates. A quote MUST use the rates in effect on its contract start date.

**Pricing and results**

- **FR-032**: The tool MUST NOT request prices until every input has passed validation.
- **FR-033**: For each supplier product and term, results MUST show: the unit rates POWWR returns
  (day, night and evening/weekend, or up to six seasonal time-of-day rates) and standing charge,
  including uplift; for half-hourly meters, the capacity, excess capacity and reactive power charges;
  any fixed fee; any direct debit discount or cheque surcharge; the estimated annual cost; the saving
  or extra cost against the current rates (when they were given); whether the start date is outside
  that supplier's pricing window; and any supplier notes. Supplier notes are agent-only.
- **FR-034**: The estimated annual cost excluding VAT and CCL MUST be POWWR's annual price, which
  includes every charge. When "include VAT and CCL" is on, the tool MUST add VAT and CCL using the
  customer's treatment. The setting MUST be saved with the quote and used on the PDF.
- **FR-035**: Rates MUST be shown exactly as POWWR returns them, and money to the penny.
  Calculations MUST use full precision and round only for display, with the same result on screen,
  in the PDF and in reporting.
- **FR-036**: Suppliers that cannot price the meter MUST be listed with the reason POWWR gives.
- **FR-037**: Agent-only columns MUST show the base rates (before uplift) and POWWR's commission
  figure, split into estimated upfront and monthly amounts using each supplier's commission payment
  model from the admin settings. The tool MUST read POWWR's commission figure on the basis set in the
  admin settings (per year or for the whole contract; initially per year), and each pricing MUST
  record the basis it used.
- **FR-038**: The agent MUST be able to sort by annual cost or commission, and filter by term and
  supplier, and by green products when POWWR's product details identify them.
- **FR-039**: The agent MUST be able to change uplifts and re-price without re-entering anything.
  Each pricing MUST be kept with the quote.
- **FR-040**: The agent MUST be able to select any number of options and mark one of the selected
  options as recommended.
- **FR-041**: Each pricing MUST record when it happened and POWWR's pricebook version.
- **FR-042**: When POWWR returns an error, times out or limits requests, the tool MUST show a clear
  message, keep everything entered and let the agent retry.

**Gas**

- **FR-043**: Gas quotes MUST follow the electricity flow with: MPRN (numbers only, 6–10 digits), found
  by postcode lookup or typed in; annual quantity (AQ, kWh), pre-filled from the lookup; the gas
  region (LDZ) shown from the lookup and not chosen by the agent; smart meter yes/no, suggested from
  the lookup's meter reading information; and the current gas supplier and rates. Gas is single rate.
- **FR-044**: Gas MUST have its own default uplifts.
- **FR-045**: The tool MUST flag large or daily-metered gas sites for a manual quote and MUST NOT
  price them. A site is large or daily-metered when its annual quantity (AQ), meter capacity or daily
  metered quantity is above the admin-set limit for that measure. Each limit is optional, and a blank
  limit is not checked. A hand-entered site without lookup data MUST still be checked against the AQ
  limit.
- **FR-046**: The tool MUST flag sites on an independent gas transporter (IGT) network, saying some
  suppliers may not price them, and MUST still price them.

**Customer PDF**

- **FR-047**: From the selected options, the tool MUST generate an A4 PDF in Consultiv Utilities
  branding, as defined in `DESIGN.md`, of 1–2 pages for a single site. It MUST contain:
  - our logo and contact details;
  - the customer name, contact and site;
  - the MPAN or MPRN in full;
  - the quote type, date and reference number;
  - the price validity statement, with the date the prices were fetched, saying they are valid until
    the end of that day;
  - a clear notice that the prices are indicative and subject to change;
  - a comparison table of the selected options, with the recommended one highlighted;
  - the saving against current;
  - the assumptions: consumption used, payment method, VAT and CCL treatment, and whether prices
    include VAT and CCL;
  - the credit check statement ("All prices are subject to supplier credit check and acceptance");
  - the commission disclosure;
  - for microbusinesses, the microbusiness wording (for example the commission disclosure, dispute
    resolution scheme details such as the Energy Ombudsman, and any cooling-off period);
  - the agent's name, email and phone;
  - a footer with our company registration number and a clickable link to our terms and conditions,
    with the full web address printed so it also works on a printed copy.
- **FR-048**: The file name MUST be `{CustomerName}_{Electricity|Gas}Quote_{YYYY-MM-DD}.pdf`, with
  characters that are not allowed in file names removed or replaced.
- **FR-049**: The PDF MUST NOT contain base rates, uplift amounts or commission figures, except where
  the approved commission disclosure wording includes them. The disclosure wording MAY include named
  placeholders for an option's commission, which the tool fills in. The PDF MUST NOT contain POWWR's
  supplier notes (footer messages, extra information or footer notes).
- **FR-050**: All customer-facing wording MUST come from the admin settings. Until compliance
  provides the approved wording, the settings MUST hold placeholder text that is visibly marked as a
  placeholder on the PDF.
- **FR-051**: Every PDF MUST be kept with the quote, together with the version of each piece of
  wording it used. Generating it again MUST create a new PDF and keep the earlier ones.
- **FR-052**: Prices MUST count as valid only until the end of the day they were fetched (UK time).
  The tool MUST NOT generate a PDF, or create an Outlook draft, from prices fetched on an earlier day;
  it MUST tell the agent the prices have expired and require a re-price. On the same day, before
  generating the PDF, the tool MUST also check whether any selected supplier has published new prices
  since the quote was priced. If so, it MUST tell the agent which suppliers changed and require a
  re-price.
- **FR-053**: The PDF MUST NOT be generated while any selected option is above a supplier's maximum
  uplift or needs an approval that has not been given.

**Open in Outlook**

- **FR-054**: Once a PDF exists, the agent MUST be able to choose "Open in Outlook". This MUST save a
  new email in the agent's own Drafts folder, with only the PDF attached and no recipient, subject or
  body, and open it for them.
- **FR-055**: It MUST work for agents on both classic and new Outlook.
- **FR-056**: The tool MUST NOT send or read email.
- **FR-057**: Creating the draft MUST be recorded with the quote. If Outlook cannot be reached, the
  agent MUST see a clear message and still be able to download the PDF.

**Uplift limits and approvals**

- **FR-058**: Admins MUST be able to set maximum unit-rate and standing-charge uplifts per fuel for
  each role and, optionally, for an individual agent. An individual agent's limit MUST replace their
  role's limit.
- **FR-059**: Admins MUST be able to set a maximum unit-rate and standing-charge uplift per supplier.
- **FR-060**: An agent MUST be able to see prices with uplifts above their limit. Those options MUST
  be marked as needing approval.
- **FR-061**: Before generating the PDF or opening it in Outlook for those options, the agent MUST
  request manager approval with a reason.
- **FR-062**: Managers MUST see pending requests on an Approvals page and approve or decline them.
  A comment MUST be required when declining.
- **FR-063**: A manager MUST NOT be able to approve or decline their own request.
- **FR-064**: The agent MUST see the outcome and any comment in the tool, on the quote.
- **FR-065**: An approval MUST cover the specific uplift values approved for each supplier and term.
  Re-pricing with the same or lower uplifts MUST keep the approval; raising them MUST need a new one.
- **FR-066**: A supplier's maximum uplift MUST be a hard limit. Options above it MUST be marked as
  above the supplier's maximum, with the maximum shown, and MUST NOT be approvable by anyone. The
  agent MUST lower the uplift to the maximum or below before offering those options.
- **FR-067**: Every request and decision MUST be recorded with who made it and when.

**Multi-site quotes**

- **FR-068**: One quote MUST be able to cover several sites or meters for the same customer and fuel.
- **FR-069**: The agent MUST be able to add sites one at a time, with the same postcode lookup, or
  upload a spreadsheet (CSV or Excel) using a template downloaded from the tool.
- **FR-070**: Every uploaded row MUST be validated with the same rules as a single site, and all
  errors MUST be shown per row before anything is priced. A meter number repeated in the quote, or a
  row for the other fuel, MUST be an error.
- **FR-071**: Each site MUST have its own address, meter number, meter details, consumption, and
  current supplier and rates. Terms, payment method, uplifts and VAT and CCL treatment MUST apply to
  the whole quote, with an optional start date per site.
- **FR-072**: Each site MUST be priced separately, and the results combined.
- **FR-073**: Results MUST show, for each supplier and term, the total across all sites and a
  per-site breakdown. Any site that supplier cannot price MUST be flagged with POWWR's reason, and the
  total MUST be marked as partial.
- **FR-074**: The PDF for a multi-site quote MUST show the totals first, then a per-site breakdown,
  and MUST list any sites an offered option does not cover.

**Quote history**

- **FR-075**: Agents MUST be able to find past quotes by customer name, quote reference, MPAN or
  MPRN, postcode and date range.
- **FR-076**: Agents MUST be able to reopen a quote, re-price it with current prices and re-download
  its PDFs. They MUST be able to open a PDF in Outlook again only on the day its prices were fetched;
  after that, FR-052 applies.
- **FR-077**: Reopening a quote MUST show whether any supplier's prices have changed since it was
  priced, using POWWR's price change history.
- **FR-078**: Every quote MUST have a unique reference number.
- **FR-079**: Every request, result, uplift, approval, PDF and Outlook draft MUST be kept with who
  did it and when, and MUST NOT be editable or deletable through the tool.
- **FR-080**: Quotes, PDFs and reporting records MUST be deleted automatically once they are older
  than the retention period. The period MUST be an admin setting, initially 6 years.

**Admin settings**

- **FR-081**: Admins MUST be able to manage:
  - default uplifts per fuel;
  - maximum uplifts per role, per agent and per supplier;
  - the supplier list: each supplier's name as POWWR's pricing expects it, matched to the names and
    IDs the meter lookup returns, and each supplier's commission payment model;
  - the basis of POWWR's commission figure (per year or whole contract);
  - the default payment method;
  - VAT rates, CCL rates and the low-use thresholds, with effective-from dates;
  - the microbusiness thresholds;
  - the large gas site limits for AQ, meter capacity and daily metered quantity (each optional);
  - the retention period;
  - all customer-facing wording: validity statement, indicative-price notice, credit check,
    commission disclosure, microbusiness wording and terms;
  - the web address of our terms and conditions.
- **FR-082**: Only Admins MUST be able to change settings. Every change MUST be logged with who, when,
  and the old and new values, and Admins MUST be able to view that log.
- **FR-083**: Changing a setting MUST NOT change PDFs that have already been generated.

**Management reporting**

- **FR-084**: Every quote event MUST be recorded for reporting: quote requested, prices returned,
  re-priced, approval requested, approval decided, PDF generated, opened in Outlook, and failed. Each
  record MUST include:
  - who (agent name and email) and when;
  - our quote reference and POWWR's reference;
  - the quote type and out-of-contract flag;
  - the customer name, business type, company number and postcode;
  - the microbusiness flag and the fuel;
  - every site and meter: the full MPAN with profile class, MTC, LLFC, SSC, DUoS tariff ID, MHHS
    status and region, or the MPRN with LDZ;
  - half-hourly, kVA and smart meter;
  - consumption (per register, or AQ);
  - the current supplier and rates;
  - the start date, terms requested, payment method, uplifts, no-standing-charge choice, and VAT and
    CCL treatment;
  - every price returned: supplier, product, plan type, term, base and uplifted rates, estimated
    annual cost, POWWR's commission and our upfront and monthly split, and pricebook version;
  - every no-quote reason;
  - which options were selected and recommended;
  - whether a PDF was produced and opened in Outlook.
- **FR-085**: Admins MUST be able to view these records on a Reports page, filter by agent, date
  range, fuel, supplier, region and quote type, see totals (quotes per agent, PDFs produced and opened
  in Outlook, and estimated commission), and export the filtered list to CSV.
- **FR-086**: Every export MUST be recorded with who made it and when.
- **FR-087**: The same records MUST be exported to a reporting store that Power BI can read.
- **FR-088**: Reporting data includes commission and customer details, so only Admins MUST be able
  to see it.
- **FR-089**: Recording and exporting MUST NOT slow down or block an agent. Failed exports MUST be
  retried until they succeed.

**Look and accessibility**

- **FR-090**: Every screen and the PDF MUST follow the Consultiv Utilities design rules in
  `DESIGN.md`.
- **FR-091**: All text and controls MUST meet WCAG AA contrast and be usable by keyboard.
- **FR-092**: Screens MUST work on desktop, tablet and phone widths.

### Key Entities *(include if feature involves data)*

- **Quote**: one customer, one fuel and one or more sites. It holds the reference number, owner
  (agent), quote type, out-of-contract flag, contract settings, microbusiness flag, VAT and CCL
  treatment, "include VAT and CCL" choice, and status.
- **Customer details**: the business name, type, company number and contact details, held on the
  quote.
- **Site**: an address with exactly one meter, consumption, current supplier and rates, and an
  optional start date.
- **Meter**: electricity (MPAN with top-line fields, region, half-hourly details, rate structure) or
  gas (MPRN, LDZ, IGT flag, large-site flag), with lookup warnings and a record of which values came
  from the lookup.
- **Pricing run**: one request for prices at a point in time, with the uplifts used, the pricebook
  version, the commission basis used and the results.
- **Price option**: one supplier product and term from a pricing run. It holds the rates as returned,
  base rates, charges, estimated annual cost, VAT and CCL, saving, commission split, pricing-window
  flag, notes, and whether it is selected, recommended or needs approval.
- **No-quote result**: a supplier that could not price a site, with POWWR's reason.
- **Approval request**: options needing approval, the uplift values, the agent's reason, and the
  manager's decision and comment.
- **Customer PDF**: a generated file linked to the quote, the pricing run and the wording versions
  used.
- **Outlook draft record**: when and by whom a draft was created for a PDF.
- **Supplier**: the pricing name, lookup names and IDs, commission payment model and maximum
  uplifts.
- **Setting**: a managed value (defaults, limits, rates with effective-from dates, thresholds,
  wording, terms address) with its change history.
- **Audit entry**: who did what to which quote or setting, and when.
- **Reporting record**: one quote event with the details in FR-084.
- **Staff member**: a signed-in person with one or more roles and, optionally, their own uplift
  limits.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An agent goes from a postcode to a downloaded PDF, or an Outlook draft with it
  attached, in under 2 minutes for a single site in 9 out of 10 attempts, when POWWR responds
  normally.
- **SC-002**: In testing, 100% of invalid MPANs and MPRNs are caught before any price request is sent.
- **SC-003**: In testing, no customer PDF or Outlook draft contains a base rate, uplift amount or
  commission figure outside the approved disclosure wording.
- **SC-004**: For every test quote, every figure on screen, in the PDF and in reporting matches to
  the penny.
- **SC-005**: When the meter lookup is unavailable, agents can still complete 100% of quotes by
  entering details by hand.
- **SC-006**: Every quote action and settings change can be traced to the person who made it and
  when, with no gaps.
- **SC-007**: A reporting outage never stops an agent completing a quote, and every event reaches
  the reporting store within 15 minutes of the store becoming available.
- **SC-008**: A 50-site spreadsheet can be uploaded, have every invalid row reported, and be priced
  in a single sitting without re-entering any site.
- **SC-009**: No PDF can be generated or opened in Outlook for an option above a supplier's maximum
  uplift, or one that needs an approval that has not been given.
- **SC-010**: A single-site PDF with up to 6 selected options fits on 1–2 A4 pages.
- **SC-011**: No new PDF or Outlook draft is ever produced from prices fetched on an earlier day.

## Assumptions

- **Users:** internal staff only, signing in with their work accounts. The number of agents is
  confirmed later; normal office use of tens of agents is assumed.
- **POWWR:** POWWR's UK Pricing API and Meter Lookup Service are the only price and meter sources.
  Until POWWR provides credentials and answers its open questions (`docs/powwr/README.md`), the tool
  is built and demonstrated with realistic sample data.
- **Commission:** POWWR's docs do not say whether its commission figure is per year, for the whole
  contract or the upfront payment. The basis is an admin setting, initially per year because the
  figure sits beside POWWR's annual price. Each supplier's commission payment model then splits it
  into upfront and monthly amounts. POWWR to confirm.
- **Price validity:** POWWR gives no expiry for prices, so the tool treats them as valid until the
  end of the day they were fetched (UK time). The validity statement is admin wording with that date
  filled in. Same-day changes are detected through POWWR's price change history. POWWR to confirm how
  long prices actually stay valid.
- **Missing lookup fields:** the lookup does not return the DUoS tariff ID or MHHS status. They come
  from a 9-digit top line when there is one (FR-016); otherwise they are optional for the agent.
  POWWR's example 9-digit top line, `040151661`, carries the SSC (`0151`) and DUoS tariff ID (`661`).
  POWWR to confirm that layout, and that this format means the meter is MHHS-migrated.
- **Large gas sites:** POWWR's docs give no thresholds, and the lookup's daily metered quantity
  appears even on small meters, so it cannot identify daily-metered sites on its own. The three limits
  start blank, so nothing is flagged until they are set from POWWR's advice.
- **Settings before User Story 8:** until the admin settings pages exist, the tool starts with initial
  values (defaults, limits, VAT and CCL rates, thresholds) and placeholder wording.
- **VAT and CCL detail:** finance confirms how partial qualifying use is treated, for example any
  percentage above which the whole supply qualifies, and the rates and thresholds are set to match.
  The annual cost estimate uses the rates in effect on the contract start date for the first year.
- **Low use:** worked out per site as annual consumption divided by 365.
- **Microbusiness boundary:** consumption exactly at 100,000 kWh of electricity or 293,000 kWh of gas
  counts as a microbusiness, following our reading of the regulatory "not more than" definition.
  Open item: compliance to confirm.
- **Visibility:** agents see only their own quotes; managers and admins see all. A manager's quotes
  are subject to the Manager role's limits and need another manager's approval.
- **Re-pricing:** re-pricing adds a new pricing run to the same quote; earlier runs and PDFs are kept.
- **Retention:** the 6-year period runs from a quote's most recent activity.
- **Volume:** multi-site quotes are usually under 50 sites, and single-site quotes usually offer up
  to 6 options.
- **Out of scope:** sending email; reading the agent's mailbox; contracts, e-signature or submitting
  deals to suppliers; dual-fuel quotes (each quote is for one fuel); domestic customers; a separate
  customer database or CRM; and deploying to Azure, which is a later feature.
- **Supplier notes:** POWWR's free-text notes stay agent-only until compliance has reviewed what
  suppliers send. Showing reviewed notes on the PDF would be a later change.
- **What the POWWR docs settle:** the payment method must be Direct Debit, BACS or Cash/Cheque,
  optionally with Monthly or Quarterly and Fixed or Variable, and it restricts which prices return.
  Rates come back as text to show as returned, with separate exact figures for calculations. The
  pricing API takes the SSC and DUoS tariff ID from a 9-digit top line when one is given.
- **Open questions for POWWR** (numbered as in `docs/powwr/README.md`), and what the tool does until
  they are answered:
  - Q3, what the commission figure covers: an admin setting, initially per year (FR-037).
  - Q4, the allowed values for plan type (including any no-standing-charge plan), business type and
    meter type: "no standing charge" filters on the returned standing charge; our seven business types
    are mapped to POWWR's values once known; meter type is shown as returned.
  - Q5, the units of the uplift fields: assumed to be p/kWh and p/day.
  - Q6, how long prices stay valid: treated as valid until the end of the day fetched (FR-052).
  - Q7, whether any field identifies green products: the green filter appears only if one does.
  - Q8, what the "inclusive" annual price includes: not used; the tool works out VAT and CCL itself.
  - Q9, which date field is the new contract's start date: settled during planning, from POWWR's
    answer.
  - Q10, the DUoS tariff ID and MHHS status: taken from a 9-digit top line, otherwise optional
    (FR-016). Also confirm the 9-digit layout, and that it means the meter is MHHS-migrated.
  - Q11, the large gas site thresholds: three optional admin limits, blank until advised (FR-045).
  - Q12, the content of supplier notes: agent-only until reviewed (FR-049).
- **Dependencies:** IT provides the sign-in registration with permission to create Outlook drafts;
  POWWR provides the licence code, Meter Lookup access and answers to the open questions; compliance
  provides the approved wording and terms address; finance provides the VAT and CCL rules, commission
  payment models and uplift limits.
