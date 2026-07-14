# Mobile Responsive Dashboard — Delta Specs

## Domain: dashboard-responsive (NEW)

### Purpose

Responsive layout framework providing breakpoint system, hamburger drawer sidebar, touch-target sizing, and card/ table transforms for viewports ≤900px.

### Requirements

#### Requirement: Breakpoint System

The system MUST define three CSS breakpoints in `stitch-dashboard-responsive.css`: 900px (tablet), 768px (small tablet/large phone), 480px (phone). All responsive rules MUST be scoped under `@media (max-width: Xpx)` to prevent desktop regression.

| Breakpoint | Trigger | Primary effect |
|------------|---------|----------------|
| ≤900px | Tablet | Sidebar hidden; hamburger visible; content full-width |
| ≤768px | Small tablet | Header compresses; card grids → single column; dropdowns reposition |
| ≤480px | Phone | Status badges wrap; page titles truncate; padding reduces further |

##### Scenario: Desktop unaffected at ≥1024px

- GIVEN viewport width is 1024px or greater
- WHEN the stylesheet loads
- THEN no `@media` rules from `stitch-dashboard-responsive.css` SHALL apply
- AND the layout SHALL be identical to pre-change behavior

##### Scenario: Breakpoint cascade at 768px

- GIVEN viewport width is exactly 768px
- WHEN the page renders
- THEN both 900px AND 768px breakpoint rules SHALL apply

---

#### Requirement: Hamburger Drawer Sidebar

The sidebar MUST become a hidden off-canvas drawer at ≤900px. A hamburger button MUST toggle the drawer via CSS class `.dashboard-sidebar--open`. The drawer MUST use `transform: translateX(-100%)` (closed) → `translateX(0)` (open) with a transition ≤300ms. A `.backdrop` div MUST overlay the main content when the drawer is open, with `background: rgba(0,0,0,0.5)` and dismiss-on-tap behavior.

| Element | Desktop (≥901px) | Mobile (≤900px) |
|---------|-------------------|------------------|
| Sidebar | Fixed left, always visible | Off-canvas, hidden by default |
| Hamburger button | Hidden (`display: none`) | Visible in header left |
| Backdrop | Not rendered / hidden | Visible when drawer open |
| Main content | `padding-left: var(--aura-sidebar-width)` | `padding-left: 0` |

##### Scenario: Tap hamburger opens drawer

- GIVEN viewport ≤900px and sidebar is closed
- WHEN the user taps the hamburger button
- THEN `.dashboard-sidebar--open` class is added to the sidebar
- AND the sidebar slides into view from the left
- AND the backdrop overlay appears

##### Scenario: Tap backdrop closes drawer

- GIVEN the sidebar drawer is open
- WHEN the user taps the backdrop area
- THEN the sidebar slides out of view
- AND the backdrop disappears

##### Scenario: Nav item tap closes drawer

- GIVEN the sidebar drawer is open
- WHEN the user taps any nav link inside the sidebar
- THEN the drawer SHALL close after navigation

##### Scenario: Desktop sidebar always visible

- GIVEN viewport ≥901px
- WHEN the page renders
- THEN the hamburger button is hidden
- AND the sidebar is visible in its fixed position

---

#### Requirement: Touch Target Sizing

All interactive elements (buttons, links, nav items, pagination controls, dropdown options) MUST have a minimum hit area of 44×44px at ≤768px. This MUST be achieved via CSS `min-height: 44px; min-width: 44px` or equivalent padding. Non-interactive elements MUST NOT be affected.

##### Scenario: Pagination buttons pass touch audit

- GIVEN viewport ≤768px and Decision Log pagination is visible
- WHEN each pagination button is measured
- THEN its hit area is at least 44×44px

##### Scenario: Sidebar nav items pass touch audit

- GIVEN viewport ≤768px
- WHEN each sidebar nav item is measured
- THEN its hit area is at least 44px in height

---

#### Requirement: Responsive CSS Loading

`stitch-dashboard-responsive.css` MUST be loaded after `stitch-dashboard.css` in `App.razor`. The new file MUST NOT contain any rules outside `@media` blocks (except CSS custom property overrides scoped to breakpoints).

##### Scenario: CSS load order correct

- GIVEN the page HTML
- WHEN `<link>` elements are inspected
- THEN `stitch-dashboard-responsive.css` appears after `stitch-dashboard.css`

---

## Domain: initial-dashboard (MODIFIED)

#### Requirement: Layout Shell Responsive Adaptation

The dashboard shell (header + sidebar + main content) MUST adapt at 900/768/480px breakpoints. At ≤900px the sidebar becomes a hamburger drawer overlay. At ≤768px the header compresses non-critical controls into an overflow area. The Sign out button and Settings icon MUST remain visible at all breakpoints.

(Previously: Shell had a single 900px breakpoint that collapsed sidebar to full-width inline and hid header nav.)

##### Scenario: Header at ≤768px shows hamburger + essential controls

- GIVEN viewport ≤768px
- WHEN the header renders
- THEN the hamburger icon is visible on the left
- AND Sign out + Settings remain visible on the right
- AND Demo Mode / FocusState badge are accessible via overflow or remain visible if space permits

##### Scenario: Status greeting card badges wrap at ≤480px

- GIVEN viewport ≤480px and StatusGreetingCard has 5 status badges
- WHEN the card renders
- THEN badges wrap to a second row without horizontal overflow

---

## Domain: interruption-decision-log (MODIFIED)

#### Requirement: Decision Log Stacked Card Layout at ≤768px

At viewport ≤768px, the 9-column decision log table MUST convert to a stacked card layout. Each row becomes a card with `label: value` pairs for all 9 fields (chevron, timestamp, title, source, score, decision, focus, explanation, guardrail). The `<details>` expand/collapse MUST still function. Pagination controls MUST have 44px touch targets.

(Previously: Decision log rendered as a fixed 9-column `<table>` at all viewports.)

##### Scenario: Decision Log renders as cards on mobile

- GIVEN viewport ≤768px and 5 decision items exist
- WHEN the page renders
- THEN 5 stacked cards are visible (not a table)
- AND each card shows all 9 fields as label:value pairs
- AND no horizontal scroll is needed

##### Scenario: Card expand/collapse works on mobile

- GIVEN viewport ≤768px
- WHEN the user taps a decision card's expand control
- THEN the detail panel (Final Verdict, Rules Fired, LLM Rationale, Semantic Context) is revealed

##### Scenario: Desktop table layout preserved

- GIVEN viewport ≥769px
- WHEN the Decision Log page renders
- THEN the 9-column table layout is used (unchanged from current behavior)

---

## Domain: pr-connector-ui (MODIFIED)

#### Requirement: PR Mini-Table Horizontal Scroll at ≤600px

The PR mini-table in PrioritySummaryCards and the full PR table on PullRequests.razor MUST be wrapped in a container with `overflow-x: auto` at ≤600px, allowing horizontal scroll to prevent column cutoff.

(Previously: PR tables had no scroll wrapper and could overflow at narrow viewports.)

##### Scenario: PR mini-table scrolls horizontally on small phones

- GIVEN viewport ≤600px and the dashboard PR card has 3 items
- WHEN the PR mini-table renders
- THEN the table is wrapped in a horizontally scrollable container
- AND no columns are clipped or hidden

##### Scenario: Full PR table scrolls on small phones

- GIVEN viewport ≤600px and PullRequests page has 5 rows
- WHEN the page renders
- THEN the table scrolls horizontally without content loss

---

## Domain: focus-state-machine (MODIFIED)

#### Requirement: FocusState Badge Dropdown Repositioning at ≤768px

The FocusState dropdown MUST reposition from `right: 0` to `left: 0` (or `left: 50%; transform: translateX(-50%)`) at ≤768px to prevent viewport overflow when the badge is near the right edge of the header.

(Previously: Dropdown was always `position: absolute; right: 0`.)

##### Scenario: Dropdown opens within viewport on mobile

- GIVEN viewport ≤768px and FocusStateBadge is in the header
- WHEN the user taps the badge to open the dropdown
- THEN the dropdown is fully visible within the viewport
- AND no horizontal clipping occurs

---

## Domain: demo-mode (MODIFIED)

#### Requirement: Demo Wizard Tooltip Repositioning at ≤768px

The demo wizard tooltip (`.demo-wizard`) MUST use `position: fixed; bottom: 1rem; left: 50%; transform: translateX(-50%)` at ≤768px instead of `position: absolute` relative to the demo button. The tooltip MUST remain tappable and fully visible on 390px viewports.

(Previously: Tooltip was `position: absolute` anchored below the demo button with `left: 50%; transform: translateX(-50%)`.)

##### Scenario: Demo wizard visible on 390px viewport

- GIVEN viewport is 390px wide and demo user is active
- WHEN the wizard tooltip is shown
- THEN it is centered at the bottom of the viewport
- AND the text is fully readable
- AND the tooltip is tappable

##### Scenario: Demo wizard does not overlap sidebar drawer

- GIVEN viewport ≤768px and sidebar drawer is open
- WHEN the demo wizard is visible
- THEN the wizard z-index is below the sidebar drawer (z-index < 40) OR the wizard is hidden while drawer is open

---

## Edge Cases

#### Requirement: SignalR Toast Positioning on Mobile

The `.toast-container` (fixed top-right) MUST remain visible and non-overlapping with the header at ≤768px. Toast `min-width` MUST reduce to fit within viewport (min 280px, max `calc(100vw - 32px)`).

##### Scenario: Toast fits on 390px viewport

- GIVEN viewport is 390px and a SignalR toast fires
- WHEN the toast renders
- THEN its width is ≤358px (390 - 32)
- AND it does not overlap the hamburger button

#### Requirement: Rotate / Resize Stability

The layout MUST NOT break when the device rotates between portrait and landscape. CSS transitions on sidebar MUST complete within 300ms. No JavaScript errors SHALL occur on resize events.

##### Scenario: Portrait to landscape rotation

- GIVEN the sidebar drawer is closed in portrait mode
- WHEN the device rotates to landscape
- THEN the layout adapts without visual glitches
- AND if viewport crosses 900px threshold, sidebar behavior switches correctly
