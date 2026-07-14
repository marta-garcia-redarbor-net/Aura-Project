# Design: Mobile Responsive Dashboard

## Technical Approach

Add a dedicated `stitch-dashboard-responsive.css` file loaded after the main stylesheet, containing all responsive rules scoped under `@media` breakpoints (900px, 768px, 480px). The existing 900px block in `stitch-dashboard.css` (lines 1135–1154) is removed and replaced by the new file. A small JS interop module (`sidebarDrawer.js`) handles hamburger toggle, backdrop dismiss, Escape key, and nav-link auto-close. No Blazor state changes — sidebar open/close is a pure CSS class toggle on the DOM element.

## Architecture Decisions

### Decision: Sidebar drawer — CSS class toggle via JS interop

| Option | Tradeoff | Decision |
|--------|----------|----------|
| JS classList.toggle on `.dashboard-sidebar` | Zero Blazor re-renders, instant, no SignalR round-trip | **Chosen** |
| Blazor `@bind` bool + conditional CSS class | Requires SignalR round-trip per toggle, adds state to MainLayoutAuthenticated | Rejected |
| CSS-only checkbox hack | No backdrop dismiss, no Escape key, inaccessible | Rejected |

**Rationale**: Blazor Server's SignalR latency makes toggle feel sluggish. Pure DOM manipulation is instant and the sidebar state doesn't need to be in Blazor's render tree.

### Decision: Decision Log card layout — CSS `display: block` on table elements

| Option | Tradeoff | Decision |
|--------|----------|----------|
| CSS `display: block` on `<table>/<tr>/<td>` with `::before` labels using `data-label` | Keeps single markup, no duplicate rendering, `<details>` still works | **Chosen** |
| Separate card `<div>` alongside table, shown/hidden via CSS | Duplicate markup, Blazor renders both, wasted bytes | Rejected |
| JS-driven DOM transformation on resize | Complex, fragile, resize race conditions | Rejected |

**Rationale**: The `<details>` expand/collapse inside each `<tr>` is preserved because we only change display mode, not DOM structure. Each `<td>` gets a `data-label` attribute for the CSS `::before` content.

### Decision: Header overflow — CSS visibility, not Blazor conditional rendering

| Option | Tradeoff | Decision |
|--------|----------|----------|
| CSS `display: none` on `.dashboard-header__demo-group` at ≤768px, replaced by overflow trigger | Simple, no Blazor re-render, desktop untouched | **Chosen** |
| Blazor `@if` based on viewport detection via JS interop | Requires resize listener → JS interop → Blazor state → re-render cascade | Rejected |

**Rationale**: Demo controls and FocusStateBadge are hidden via CSS at ≤768px. An overflow button (`.dashboard-header__overflow-btn`) is always in the DOM but `display: none` at desktop. At ≤768px it becomes visible and opens a dropdown containing the same controls (duplicated markup, but only 2–3 elements — acceptable cost for zero-JS-resize behavior).

### Decision: Separate responsive CSS file

| Option | Tradeoff | Decision |
|--------|----------|----------|
| New `stitch-dashboard-responsive.css` | Clean separation, easy rollback (remove one `<link>`), main file untouched | **Chosen** |
| Append `@media` blocks to existing `stitch-dashboard.css` | Single file, but 3777 lines already hard to navigate | Rejected |

**Rationale**: Rollback plan is literally removing one `<link>` tag. The main CSS file's existing 900px block is removed to avoid duplicate/conflicting rules.

## Data Flow

```
User taps hamburger
       │
       ▼
sidebarDrawer.toggleDrawer()  ──→  classList.toggle('dashboard-sidebar--open')
       │                                    │
       ▼                                    ▼
backdrop.classList.toggle('visible')    CSS transition: translateX(-100%) → translateX(0)
                                            │
                                            ▼
                                    Sidebar visible over content
                                    
User taps backdrop / nav link / Escape
       │
       ▼
sidebarDrawer.closeDrawer()  ──→  removes class, hides backdrop
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.UI/wwwroot/css/stitch-dashboard-responsive.css` | Create | All responsive `@media` rules for 900/768/480px breakpoints |
| `src/Aura.UI/wwwroot/js/sidebarDrawer.js` | Create | JS interop: toggleDrawer, closeDrawer, Escape listener, nav-link click listener |
| `src/Aura.UI/Components/App.razor` | Modify | Add `<link>` for responsive CSS (after main), add `<script>` for sidebarDrawer.js |
| `src/Aura.UI/wwwroot/css/stitch-dashboard.css` | Modify | Remove existing `@media (max-width: 900px)` block (lines 1135–1154) and `@media (max-width: 900px)` for priority-summary-cards (lines 1744–1748) |
| `src/Aura.UI/Components/Layout/MainLayoutAuthenticated.razor` | Modify | Add backdrop `<div>`, inject JS interop for sidebar init/teardown |
| `src/Aura.UI/Components/Layout/Header.razor` | Modify | Add hamburger button (left side), add overflow button + overflow dropdown containing Demo controls + FocusStateBadge |
| `src/Aura.UI/Components/Layout/Sidebar.razor` | Modify | No structural changes needed — CSS handles drawer behavior via class on `<aside>` |
| `src/Aura.UI/Pages/DecisionLog.razor` | Modify | Add `data-label` attributes to each `<td>` in the decision trace grid |
| `src/Aura.UI/Pages/PullRequests.razor` | Modify | Wrap `.pr-table` in `<div class="table-scroll-wrapper">` |
| `src/Aura.UI/Components/Dashboard/PrioritySummaryCards.razor` | Modify | Wrap `.pr-mini-table` in `<div class="table-scroll-wrapper">` (already has `.pr-mini-table-container` — rename or reuse) |

## Interfaces / Contracts

### JS Interop Module: `sidebarDrawer.js`

```javascript
window.sidebarDrawer = {
  init() {
    // Attach Escape key listener
    // Attach nav-link click listeners (close on navigate)
    // Attach backdrop click listener
  },
  toggleDrawer() {
    // Toggle 'dashboard-sidebar--open' on .dashboard-sidebar
    // Toggle 'backdrop--visible' on .backdrop
  },
  closeDrawer() {
    // Remove 'dashboard-sidebar--open'
    // Remove 'backdrop--visible'
  },
  dispose() {
    // Remove all event listeners
  }
};
```

### CSS Classes Contract

| Class | Element | Behavior |
|-------|---------|----------|
| `.dashboard-sidebar--open` | `.dashboard-sidebar` | `transform: translateX(0)` — drawer visible |
| `.backdrop` | `<div>` in MainLayout | `display: none` by default |
| `.backdrop--visible` | `.backdrop` | `display: block; position: fixed; inset: 0; background: rgba(0,0,0,0.5); z-index: 35` |
| `.dashboard-header__hamburger` | `<button>` in Header | `display: none` at ≥901px, `display: inline-flex` at ≤900px |
| `.dashboard-header__overflow-btn` | `<button>` in Header | `display: none` at ≥769px, `display: inline-flex` at ≤768px |
| `.dashboard-header__overflow-dropdown` | `<div>` in Header | Hidden at desktop, visible when `.overflow-dropdown--open` |
| `.table-scroll-wrapper` | `<div>` wrapping tables | `overflow-x: auto` at ≤600px |

### Decision Log `data-label` Attributes

Each `<td>` in `decision-trace__grid` gets a `data-label`:

```html
<span data-label="Timestamp">...</span>
<span data-label="Title">...</span>
<span data-label="Source">...</span>
<span data-label="Score">...</span>
<span data-label="Decision">...</span>
<span data-label="Focus">...</span>
<span data-label="Explanation">...</span>
<span data-label="Guardrail">...</span>
```

## Responsive Behavior Matrix

| Component | ≥1024px (Desktop) | ≤900px (Tablet) | ≤768px (Small tablet) | ≤480px (Phone) |
|-----------|-------------------|------------------|----------------------|-----------------|
| Sidebar | Fixed left, always visible | Off-canvas drawer, hamburger toggle | ← same | ← same |
| Header brand | Full "Aura" text | ← same | ← same | ← same |
| Hamburger button | Hidden | Visible (left) | ← same | ← same |
| Demo controls | Inline in header | ← same | Hidden → overflow dropdown | ← same |
| FocusStateBadge | Inline in header | ← same | Hidden → overflow dropdown | ← same |
| Sign out + Settings | Visible | ← same | ← same | ← same |
| User name | Visible | ← same | Hidden (icon only) | ← same |
| StatusGreetingCard badges | Single row, flex | ← same | Wrap allowed | Wrap to 2 rows |
| PrioritySummaryCards grid | 3 columns | 1 column (existing rule) | ← same | ← same |
| PR mini-table | Normal table | Normal table | `overflow-x: auto` wrapper | ← same |
| PR full table | Normal table | Normal table | `overflow-x: auto` wrapper | ← same |
| Decision Log table | 9-column table | 9-column table | Stacked cards via CSS `display: block` + `data-label` | ← same |
| Decision Log pagination | Normal buttons | ← same | 44px min touch targets | ← same |
| Dashboard cards grid | `auto-fit, minmax(13rem, 1fr)` | ← same | Single column | ← same |
| Page header title | 32px | 24px | 20px | 18px |
| Page header subtitle | Visible | ← same | ← same | Truncated with ellipsis |
| Demo wizard tooltip | `position: absolute` below button | ← same | `position: fixed; bottom: 1rem; left: 50%` | ← same |
| FocusState dropdown | `right: 0` | ← same | `left: 0` | ← same |
| Toast container | Fixed top-right, min-width 340px | ← same | `max-width: calc(100vw - 32px)`, min-width 280px | ← same |
| Footer | Flex row | ← same | Stack column | ← same |
| Touch targets | Default sizes | ← same | `min-height: 44px` on `button, a, .clickable, [role="button"]` | ← same |
| Content padding | `var(--aura-margin-desktop)` 32px | 16px | ← same | 12px |

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Manual / Visual | All breakpoints render correctly | Browser DevTools device emulation at 390px, 480px, 768px, 900px, 1024px, 1440px |
| Manual / Interaction | Hamburger open/close, backdrop dismiss, Escape key, nav-link close | Tap through all flows on real mobile device |
| Manual / Touch audit | All interactive elements ≥44×44px | Chrome DevTools → Rendering → "Emulate CSS media feature: pointer: coarse" + overlay |
| CSS regression | Desktop unchanged at ≥1024px | Screenshot comparison at 1440px before/after |
| Decision Log cards | All 9 fields visible as label:value at ≤768px | Inspect each card on mobile viewport |
| PR table scroll | Horizontal scroll works, no column clipping | Resize to 390px, scroll table horizontally |
| Demo wizard | Tooltip visible and tappable at 390px | Activate demo mode on mobile viewport |
| Rotation stability | No layout break on portrait ↔ landscape | Rotate device or DevTools, check sidebar state resets correctly |

## Migration / Rollout

No migration required. Changes are CSS + Razor markup + one JS file. Rollback: remove the `<link>` for `stitch-dashboard-responsive.css` from `App.razor` and revert Razor markup changes.

## Open Questions

- [x] ~~Hamburger position~~ → Left side of header (adjacent to brand), consistent with Material Design patterns.
- [ ] Decision Log cards: show all 9 fields or collapse rarely-used ones? → **Default: show all 9** as label:value pairs. Can add "More" toggle in future iteration if user feedback says it's too dense.
- [x] ~~Touch audit scope~~ → Apply 44px to `button, a, .clickable, [role="button"], .dashboard-sidebar__nav-item, .decision-log__page` — selective, not blanket on all elements.
