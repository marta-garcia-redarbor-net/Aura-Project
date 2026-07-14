# Proposal: Mobile Responsive Dashboard

## Intent

Adapt the Aura Dashboard (Blazor Server) for mobile viewports with a hamburger drawer sidebar, compressed header, responsive layout breakpoints, and touch-sized controls — without changing desktop behavior or adding mobile-only features.

## Scope

### In Scope
- Sidebar → CSS hamburger drawer overlay with backdrop, reuses existing nav items
- Header → compress demo controls + FocusState badge into overflow; keep Sign out + Settings visible
- New `stitch-dashboard-responsive.css` at breakpoints 900px, 768px, 480px
- Decision Log (9-column table) → stacked card layout on ≤768px
- PR mini-table → horizontal scroll wrapper on ≤600px
- All 10 dashboard pages adapted to viewport
- Touch targets audit (44px min hit area on all interactive elements)
- Demo wizard tooltip → fixed overlay or inline banner on mobile
- FocusState dropdown → left-aligned or centered on ≤768px

### Out of Scope
- New mobile-only nav sections, menu items, or functionality
- Bottom navigation bar
- PWA / service worker / offline / installable
- Native mobile (MAUI, Blazor Hybrid)

## Capabilities

> Contract with sdd-spec. Research `openspec/specs/` before filling.

### New Capabilities
- `dashboard-responsive`: Responsive layout framework — breakpoints, hamburger drawer, touch target sizing, card transforms for tables

### Modified Capabilities
- `initial-dashboard`: Layout shell SHALL adapt at 900/768/480px breakpoints; sidebar SHALL become hamburger drawer overlay; header SHALL compress non-critical controls
- `interruption-decision-log`: Decision Log table SHALL convert to stacked card layout ≤768px; pagination controls MUST have 44px touch targets
- `pr-connector-ui`: PR mini-table SHALL use horizontal scroll wrapper ≤600px to prevent column overflow
- `focus-state-machine`: FocusStateBadge dropdown SHALL reposition to left-aligned on ≤768px
- `demo-mode`: Demo wizard tooltip SHALL use centered fixed overlay or inline banner on ≤768px

## Approach

| Area | Solution |
|------|----------|
| Sidebar | CSS `transform: translateX` slide-left overlay + JS toggle + `.backdrop` div |
| Header | Overflow menu via Blazor `Visible` binding on demo controls + FocusState |
| CSS | New `stitch-dashboard-responsive.css` under `@media`; loaded after main stylesheet |
| Decision Log | `display: grid; grid-template-columns: 1fr` at ≤768px; each row becomes label:value card |
| PR table | `overflow-x: auto` wrapper on table container ≤600px |
| Touch targets | Global CSS: `button, a, .clickable { min-height: 44px; min-width: 44px }` |
| Demo tooltip | CSS `position: fixed; inset: auto 50%; transform: translateX(-50%)` at ≤768px |
| FocusState | CSS override: `right: auto; left: 0` at ≤768px |

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.UI/Components/Layout/Sidebar.razor` | Modified | Add CSS class toggle for drawer open/close |
| `src/Aura.UI/Components/Layout/Header.razor` | Modified | Add overflow menu wrapper |
| `src/Aura.UI/Components/Layout/MainLayoutAuthenticated.razor` | Modified | Add backdrop div |
| `src/Aura.UI/Pages/DecisionLog.razor` | Modified | Add card layout CSS classes |
| `src/Aura.UI/Pages/PullRequests.razor` | Modified | Add scroll wrapper |
| `src/Aura.UI/wwwroot/css/stitch-dashboard.css` | Modified | Remove existing 900px block |
| `src/Aura.UI/wwwroot/css/stitch-dashboard-responsive.css` | New | Responsive breakpoints |
| `src/Aura.UI/_Host.cshtml` | Modified | Import new CSS |

## Visual Reference

Stitch "Aura | Dashboard de Prioridades (Mobile)" screen shows layout patterns for sidebar drawer, header compression, and card stacking. **Constraint**: Desktop functionality prevails — do not include elements absent from desktop.

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Responsive CSS regresses desktop | Medium | All rules scoped under `@media`; test at ≥1024px |
| SignalR wizard tooltip breaks position | Medium | Fixed overlay + JS interop for safe-zone calc |
| Decision Log card layout loses density | Low | Include column headers per card as label:value |
| Touch audit misses elements | Low | CSS selector audit + DevTools overlay check |

## Rollback Plan

Revert `stitch-dashboard-responsive.css` `@import` from `_Host.cshtml`. Revert Razor markup changes per component. All changes are CSS + Razor markup — no backend rollback needed.

## Success Criteria

- [ ] Hamburger opens/closes smoothly on mobile; sidebar slides over content
- [ ] All desktop functionality works unchanged at ≥1024px
- [ ] Decision Log renders stacked cards on ≤768px; no horizontal scroll
- [ ] Touch targets pass 44px minimum audit
- [ ] Demo tooltip visible and tappable on 390px viewport
- [ ] PR table scrolls horizontally without content cutoff

## Open Questions

1. Should hamburger button be in header (right side) or standalone position?
2. Decision Log cards: show all 9 fields on each card or collapse rarely-used fields behind a "More" toggle?
3. Touch audit: exclude `svg` non-interactive parents or blanket-apply `44px` to all clickable?
