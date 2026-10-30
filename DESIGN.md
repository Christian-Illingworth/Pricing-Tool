# Consultiv Utilities web design system

The rules behind the look of Triage, written down so any Consultiv Utilities web app can match it. The values themselves live in `css/tokens.css`, and `index.html` shows every component working.

If you're building a screen, read sections 1–3 first. Most mistakes come from colour.

---

## 1. The feel

- **Calm, clean and data-first.** These are working tools people use all day, not marketing pages.
- **Navy chrome, light page, white cards.** A navy sidebar holds the white logo and the navigation. The page is a faint light-blue tint, and content sits on white cards with thin borders and soft shadows.
- **Teal is for actions. Cyan marks the current place.** A teal button commits something. A cyan edge marks the page you're on in the sidebar.
- **Numbers are big, bold and navy.** An uppercase micro-label sits above them, and the words around them say whether they're good or bad.
- **Motion is short and quiet.** Fades and slides of a few pixels, all switched off under `prefers-reduced-motion`.

---

## 2. Colour

### The palette

Use these exact hexes. They're in `tokens.css` as variables, so write `var(--teal)`, never the hex.

| Token | Hex | What it's for |
|---|---|---|
| `--navy` | `#2B3F47` | All text. The sidebar. A selected filter. |
| `--teal` | `#02708B` | Actions, links, the focus ring, the active state of a control. |
| `--cyan` | `#4AC0E5` | The active marker in the sidebar. Marks only. |
| `--lightblue` | `#B7E1F4` | Page, hover and press tints. Marks only. |
| `--coral` | `#EB5F5E` | Bad, urgent, overdue. Also the **SME** segment. Marks only. |
| `--green` | `#31AF87` | Good, done, on target. Also the **Corporate** segment. Marks only. |
| `--gray` | `#C6C6C5` | Neutral marks. Never text. |
| `--white` | `#FFFFFF` | Cards and panels. |

Portal shades, for when a set of statuses or chart series needs more hues: `--midteal` `#3D9BB8`, `--sage` `#8BC9AD`, `--deepteal` `#318487` and `--darkgray` `#7B7B7A`.

**The one exception is amber, `--amber` `#F3943E`.** The brand has no warning colour, so amber was agreed for warnings only: "Watch", High priority, Awaiting reply and internal notes. Don't use it for anything else.

The official brand guidelines also list lime `#C7D300` as a secondary colour. Triage has never used it, so the kit leaves it out. Ask before adding it, or any other new hue.

### The rules

1. **Never make a darker or lighter shade of a brand colour.** If cyan text fails contrast, don't invent a "dark cyan". Put the colour on a mark beside navy text instead.
2. **Text is navy.** Secondary text is navy at lower opacity: `--txt2` is 86% and `--txt3` is 75%. That's the same hue, not a new grey. `--txt3` is the lightest text anyone has to read, and it passes WCAG AA on every surface in the kit.
3. **Teal is the only other colour text can be,** and only for links and the active state of a control. Never use cyan, light blue, grey, green, coral or amber as text on white.
4. **Colour goes on marks:** dots, bars, borders, donut slices, and tints behind chips. A tint is a see-through version of the exact hex (`--coral-bg` is `rgba(235,95,94,0.14)`), not a new colour.
5. **Navy means selected. Teal means action.** A filter that's on is navy with white text. A button that saves, sends or claims is teal.
6. **White on coral fails contrast,** so errors keep a navy or white body and carry a coral mark (a dot, an underline or a border).

### Tones

The kit applies rule 4 through **tones**. A `.tone-*` class tells a component which brand colour its marks use. It never changes the text colour.

```html
<span class="tag tone-green">Resolved</span>       <!-- green border, green tint, navy text -->
<span class="status tone-teal">Waiting</span>       <!-- teal dot, navy text -->
<div class="notice tone-amber">3 tickets have no supplier.</div>
```

There's one tone for each colour: `tone-navy`, `tone-teal`, `tone-cyan`, `tone-coral`, `tone-green`, `tone-gray`, `tone-lightblue`, `tone-midteal`, `tone-sage`, `tone-deepteal`, `tone-darkgray` and `tone-amber`.

### Status colours (as used in Triage)

When an app has statuses, give each one a tone and keep it the same everywhere: pills, dots, chart slices and list headings. This is Triage's mapping:

| Status | Tone | Why |
|---|---|---|
| Open | cyan | |
| Waiting on supplier | teal | |
| Waiting on customer | sage | |
| Waiting for us | **coral** | The one waiting state where the ball is in our court, so it must stand out from the teals |
| Pending (other) | midteal | |
| Future date | gray | |
| AI agent | lightblue | |
| Resolved | green | |
| Closed | darkgray | |

### Good, watch, bad

| State | Colour | How it shows |
|---|---|---|
| Good | green | A green dot and words such as "On target". The tile stays quiet. |
| Watch | amber | An amber tile border, plus a `.rag--watch` tag that says why ("Watch · over 1h 30m"). |
| Bad | coral | A coral tile border, plus a `.rag--bad` tag ("Needs attention · over 10 days"). |

The words always say the state, so it never depends on colour alone.

### Chart series

Use the series in this order: `--series-1` to `--series-10`, which are teal, cyan, navy, green, light blue, grey, mid teal, sage, deep teal and dark grey. Coral and amber are kept back for bad and watch. Keep every bar on a page on one scale.

---

## 3. Logo

All files are in `logo/`. The PNGs have transparent backgrounds.

| File | Use |
|---|---|
| `cu-logo-150.png`, `-175`, `-250` | **Colour stacked logo**, on white or light surfaces (sign-in page, reports) |
| `cu-logo-150-white.png`, `-175-white`, `-250-white` | **White stacked logo**, on navy and on dark images (the sidebar) |
| `cu-mark.png`, `cu-mark-white.png` | **Brandmark alone**, for the collapsed sidebar and internal tools |
| `favicon-32.png` | Browser tab icon |

- **On dark backgrounds, use the white version.** That's from the official brand guidelines. The colour logo's teal disappears on navy.
- The **stacked** logo is the default. The official guidelines also have a **long** version for when space is short. It isn't in this kit yet, so ask marketing for the files.
- The brandmark on its own is allowed "occasionally on internal communications" (official guidelines), so it's fine for the collapsed sidebar and the favicon.
- Use `srcset` so high-DPI screens get the 250px file: `src="cu-logo-150.png" srcset="cu-logo-150.png 1x, cu-logo-250.png 2x"`.
- Don't recolour, stretch, rotate or add effects to the logo. Don't rebuild it in text or SVG.
- **The white files were made from the colour PNGs** by turning every pixel white while keeping its transparency. If marketing has official white files, swap them in under the same names.
- Photos should be colour-graded to a blue hue so they feel consistent (official guidelines).

---

## 4. Type

**Poppins**, self-hosted in `fonts/` (400, 500, 600, 700 and 800, latin subset, OFL licence). There's no Google Fonts request, so it works offline and behind firewalls. Preload the 700 weight.

| Role | Class / token | Size / weight |
|---|---|---|
| Page title | `.title-xl` | 22 / 700, -0.3px |
| Top bar title | `.topbar-title` | 18 / 700 |
| Panel or drawer title | `.title-lg` | 17 / 700 |
| Section or dialog title | `.title-md` | 15 / 700 |
| Body | `body` | 14 / 400 |
| Running text, messages | `.body-sm`, `.prose` | 13 / 400, line-height 1.6 |
| Captions, meta | `.caption` | 12 / 400, `--txt3` |
| Micro-label | `.eyebrow`, `.kpi-label`, `.section-heading` | 11 / 700, UPPERCASE, letter-spaced |
| KPI number | `.kpi-value` | 36 / 700, tabular |
| Hero number | `.stat-value` | 44 / 800, -2.5px, tabular |

- **The minimum size is 11px, and only for uppercase micro-labels.** Anything someone reads properly is 13px or more.
- Use tabular numbers (`.num`, or `font-feature-settings: 'tnum'`) wherever figures line up.

---

## 5. Space, shape, depth, motion

- **Spacing:** a 4px grid (`--sp-1` 4px up to `--sp-8` 40px). Content padding is 24px top, 32px sides and 40px bottom. Card gaps are 12–14px.
- **Corners:** 7px (`--rsm`) for inputs, buttons and menu items. 10px (`--r`) for cards and tiles. 16px (`--r-lg`) for dialogs. Pills are fully round. Never use square corners.
- **Shadows:** navy-tinted and layered: `--sh-sm` for cards, `--sh-md` for dropdowns and hover, `--sh-lg` for dialogs. Never use a heavy black drop shadow.
- **Motion:** `0.18s ease` for hovers. Panels, tabs and dialogs use `--ease-premium` (`cubic-bezier(0.16, 1, 0.3, 1)`) at 0.28s. Keyframes never end on a `transform`, because a transform left in place traps dropdowns under overlays (see the note in `base.css`).

---

## 6. Layout

```
┌──────────┬──────────────────────────────────────────┐
│  white   │ Top bar: title + caption    search  CTA  │  56px, white
│  logo    ├──────────────────────────────────────────┤
│          │                                          │
│  nav     │  Content (tinted page, white cards)      │
│  (navy)  │  24 / 32 / 40 padding                    │
│          │                                          │
│  user    │                                          │
└──────────┴──────────────────────────────────────────┘
  232px, then a 64px icon rail below 1100px or when collapsed
```

- **Sidebar:** navy. Inactive items are white at 86%. The active item has a 3px cyan left edge on a cyan tint, with a cyan icon. Section labels are light blue, uppercase. Below 1100px it becomes a 64px icon rail; on narrow screens the menu button slides the full sidebar out over the page.
- **Split view** (lists of records): a 320px list column on the tinted surface, beside a white detail pane. Below 1100px one shows at a time and the detail gets a Back button.
- **Breakpoints:** 1100px (rail, single-column split, KPI grid 4→2) and 720px (phone: the top bar wraps, `.hide-phone` items go).

---

## 7. Components

Each one is in `css/components.css`, with working markup in `index.html`.

| Need | Use |
|---|---|
| Save, send, confirm | `.btn-primary` (teal) |
| Cancel, secondary choice | `.btn-secondary` |
| Small toolbar button | `.btn-outline` |
| The page's one main action | `.btn-cta` (teal pill, optional `.btn-count`) |
| Quiet action beside content | `.btn-action` |
| Destructive | `.btn-danger-link` (navy words, coral underline) |
| Text link button | `.btn-link` |
| Form field | `.field` > `.field-label` + `.form-input` / `.form-select` / `.form-textarea` |
| Static label | `.tag.tone-*` |
| Value you can change | `.pill.tone-*` inside a `.prop` with a `.prop-label`, opening a `.menu` |
| Priority or level | `.signal[data-level="1–4"]`, or `.pill[data-level]` |
| Good / watch / bad | `.rag--good` / `.rag--watch` / `.rag--bad` |
| Filters | `.filter-pill` (short sets), `.chip` with counts, `.seg` for two or three views |
| Dropdown | `.select-trigger[data-menu]` + `.menu` with `.menu-item[role=menuitemradio]` |
| Headline numbers | `.kpi-grid` > `.kpi-tile`; secondary numbers in a `.kpi-strip` > `.kpi-mini` |
| Breakdown | `CU.donut()` |
| Ranked amounts | `.bar-list` |
| Records | `.table` in a `.card--flush`, or `.list-row`s in a `.split` |
| Messages, history | `.timeline-entry.tone-*`, `.timeline-entry--note`, `.timeline-event` |
| Inline message | `.notice.tone-*` |
| Brief confirmation | `CU.toast()` (an Undo action, or `{ error: true }`) |
| Confirm or edit | `.modal-overlay` > `.modal`, or `.drawer-wrap` > `.drawer` |
| Nothing to show | `.empty-state` (icon + message), never a bare grey line |
| Loading | `.skeleton`, `.spinner`, `.load-steps`, `.load-bar` |

**Icons:** one line set (Feather / Lucide): 24×24 viewBox, `stroke="currentColor"`, 2px stroke, round caps and joins, usually 13–18px. Never emoji.

---

## 8. Accessibility

- One keyboard focus ring everywhere: a teal ring with a white gap (`--focus-ring`), cyan on the navy sidebar. Mouse clicks don't show it.
- All text meets WCAG AA. That's why light colours only ever colour marks.
- State is never shown by colour alone: a word, a shape (`.signal`) or both always go with it.
- In the icon rail, nav labels are visually hidden but still read by screen readers. Give each item a `title` so its name shows on hover.
- Toasts use `role="status"`, and errors use `role="alert"`. Dialogs move focus in, close on Escape, and give focus back.
- Everything animated stops under `prefers-reduced-motion`.

---

## 9. Don't

- Invent a shade, a grey or a hue. Use the tokens.
- Put coloured text on white, except teal for links and the active state of a control.
- Use pure black, heavy shadows, square corners, or gradients on content cards.
- Use the colour logo on navy, or put it on a white box on navy. Use the white logo.
- Mix this kit with Bootstrap. Its `.card`, `.badge`, `.btn-primary` and `.modal` classes collide with these.

---

## About the older document guide

`../Consultiv-Utilities-Brand-Guide.md` was written for documents and PDFs before Triage's colour rules were tightened on 7 October 2026. It still lists darker "ink" text shades, grey text colours, the old amber `#e07b00`, and a white tile behind the logo on navy. **For web apps, follow this file.**
