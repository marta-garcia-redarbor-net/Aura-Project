# Dashboard Responsive Specification

## Purpose

Responsive layout framework providing breakpoint system, hamburger drawer sidebar, touch-target sizing, and card/table transforms for viewports ≤900px.

## Requirements

### Requirement: Breakpoint System

The system MUST define three CSS breakpoints in `stitch-dashboard-responsive.css`: 900px (tablet), 768px (small tablet/large phone), 480px (phone). All responsive rules MUST be scoped under `@media (max-width: Xpx)` to prevent desktop regression.

| Breakpoint | Trigger | Primary effect |
|------------|---------|----------------|
| ≤900px | Tablet | Sidebar hidden; hamburger visible; content full-width |
| ≤768px | Small tablet | Header compresses; card grids → single column; dropdowns reposition |
| ≤480px | Phone | Status badges wrap; page titles truncate; padding reduces further |

#### Scenario: Desktop unaffected at ≥1024px

- GIVEN viewport width is 1024px or greater
- WHEN the stylesheet loads
- THEN no `@media` rules from `stitch-dashboard-responsive.css` SHALL apply
- AND the layout SHALL be identical to pre-change behavior

#### Scenario: Breakpoint cascade at 768px

- GIVEN viewport width is exactly 768px
- WHEN the page renders
- THEN both 900px AND 768px breakpoint rules SHALL apply

---

### Requirement: Hamburger Drawer Sidebar

The sidebar MUST become a hidden off-canvas drawer at ≤900px. A hamburger button MUST toggle the drawer via CSS class `.dashboard-sidebar--open`. The drawer MUST use `transform: translateX(-100%)` (closed) → `translateX(0)` (open) with a transition ≤300ms. A `.backdrop` div MUST overlay the main content when the drawer is open, with `background: rgba(0,0,0,0.5)` and dismiss-on-tap behavior.

| Element | Desktop (≥901px) | Mobile (≤900px) |
|---------|-------------------|------------------|
| Sidebar | Fixed left, always visible | Off-canvas, hidden by default |
| Hamburger button | Hidden (`display: none`) | Visible in header left |
| Backdrop | Not rendered / hidden | Visible when drawer open |
| Main content | `padding-left: var(--aura-sidebar-width)` | `padding-left: 0` |

#### Scenario: Tap hamburger opens drawer

- GIVEN viewport ≤900px and sidebar is closed
- WHEN the user taps the hamburger button
- THEN `.dashboard-sidebar--open` class is added to the sidebar
- AND the sidebar slides into view from the left
- AND the backdrop overlay appears

#### Scenario: Tap backdrop closes drawer

- GIVEN the sidebar drawer is open
- WHEN the user taps the backdrop area
- THEN the sidebar slides out of view
- AND the backdrop disappears

#### Scenario: Nav item tap closes drawer

- GIVEN the sidebar drawer is open
- WHEN the user taps any nav link inside the sidebar
- THEN the drawer SHALL close after navigation

#### Scenario: Desktop sidebar always visible

- GIVEN viewport ≥901px
- WHEN the page renders
- THEN the hamburger button is hidden
- AND the sidebar is visible in its fixed position

---

### Requirement: Touch Target Sizing

All interactive elements (buttons, links, nav items, pagination controls, dropdown options) MUST have a minimum hit area of 44×44px at ≤768px. This MUST be achieved via CSS `min-height: 44px; min-width: 44px` or equivalent padding. Non-interactive elements MUST NOT be affected.

#### Scenario: Pagination buttons pass touch audit

- GIVEN viewport ≤768px and Decision Log pagination is visible
- WHEN each pagination button is measured
- THEN its hit area is at least 44×44px

#### Scenario: Sidebar nav items pass touch audit

- GIVEN viewport ≤768px
- WHEN each sidebar nav item is measured
- THEN its hit area is at least 44px in height

---

### Requirement: Responsive CSS Loading

`stitch-dashboard-responsive.css` MUST be loaded after `stitch-dashboard.css` in `App.razor`. The new file MUST NOT contain any rules outside `@media` blocks (except CSS custom property overrides scoped to breakpoints).

#### Scenario: CSS load order correct

- GIVEN the page HTML
- WHEN `<link>` elements are inspected
- THEN `stitch-dashboard-responsive.css` appears after `stitch-dashboard.css`

---

### Requirement: SignalR Toast Positioning on Mobile

The `.toast-container` (fixed top-right) MUST remain visible and non-overlapping with the header at ≤768px. Toast `min-width` MUST reduce to fit within viewport (min 280px, max `calc(100vw - 32px)`).

#### Scenario: Toast fits on 390px viewport

- GIVEN viewport is 390px and a SignalR toast fires
- WHEN the toast renders
- THEN its width is ≤358px (390 - 32)
- AND it does not overlap the hamburger button

### Requirement: Rotate / Resize Stability

The layout MUST NOT break when the device rotates between portrait and landscape. CSS transitions on sidebar MUST complete within 300ms. No JavaScript errors SHALL occur on resize events.

#### Scenario: Portrait to landscape rotation

- GIVEN the sidebar drawer is closed in portrait mode
- WHEN the device rotates to landscape
- THEN the layout adapts without visual glitches
- AND if viewport crosses 900px threshold, sidebar behavior switches correctly
