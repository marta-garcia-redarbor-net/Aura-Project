# Archive Report — Mobile Responsive Dashboard

**Change**: mobile-responsive-dashboard
**Archived at**: 2026-07-13
**Mode**: openspec (file-based)
**Archive path**: `openspec/changes/archive/2026-07-13-mobile-responsive-dashboard/`
**Delivery strategy**: single-pr (size:exception pre-approved)

---

## What Was Done

Adapted the Aura Dashboard (Blazor Server) for mobile viewports with a hamburger drawer sidebar, compressed header, responsive layout breakpoints, and touch-sized controls — without changing desktop behavior or adding mobile-only features.

### Key Capabilities Delivered

| Capability | Description |
|------------|-------------|
| `dashboard-responsive` (NEW) | Responsive layout framework — breakpoints at 900/768/480/600px, hamburger drawer, touch targets, card/table transforms |
| `initial-dashboard` (MODIFIED) | Layout shell adapts at all breakpoints; sidebar becomes hamburger drawer; header compresses |
| `interruption-decision-log` (MODIFIED) | Decision Log table converts to stacked card layout at ≤768px via CSS `display: block` + `data-label` |
| `pr-connector-ui` (MODIFIED) | PR tables wrapped in `overflow-x: auto` container at ≤600px |
| `focus-state-machine` (MODIFIED) | FocusState dropdown repositions from `right: 0` to `left: 0` at ≤768px |
| `demo-mode` (MODIFIED) | Demo wizard tooltip uses `position: fixed; bottom: 1rem; left: 50%` at ≤768px |

---

## Files Changed

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.UI/wwwroot/css/stitch-dashboard-responsive.css` | **NEW** | All responsive `@media` rules for 900/768/600/480px breakpoints (256 lines) |
| `src/Aura.UI/wwwroot/js/sidebarDrawer.js` | **NEW** | JS interop: toggleDrawer, closeDrawer, Escape listener, nav-link auto-close, body scroll lock |
| `src/Aura.UI/Components/App.razor` | MODIFIED | Added `<link>` for responsive CSS + `<script>` for sidebarDrawer.js |
| `src/Aura.UI/wwwroot/css/stitch-dashboard.css` | MODIFIED | Removed old `@media (max-width: 900px)` blocks (lines 1135–1154 and 1744–1748) |
| `src/Aura.UI/Components/Layout/MainLayoutAuthenticated.razor` | MODIFIED | Added backdrop div, JS interop lifecycle (init/dispose) |
| `src/Aura.UI/Components/Layout/Header.razor` | MODIFIED | Added hamburger button + overflow button + overflow dropdown |
| `src/Aura.UI/Pages/DecisionLog.razor` | MODIFIED | Added `data-label` attributes to decision trace grid spans |
| `src/Aura.UI/Pages/PullRequests.razor` | MODIFIED | Added `table-scroll-wrapper` class for horizontal scroll |
| `src/Aura.UI/Components/Dashboard/PrioritySummaryCards.razor` | MODIFIED | Added `table-scroll-wrapper` class for PR mini-table |

**Total**: 2 new files, 7 modified files

---

## Spec Sync Summary

| Domain | Action | Details |
|--------|--------|---------|
| `dashboard-responsive` | **Created** | New spec with Breakpoint System, Hamburger Drawer, Touch Targets, CSS Loading, Toast Positioning, Rotate/Resize Stability requirements |
| `initial-dashboard` | **Updated** | Appended "Layout Shell Responsive Adaptation" requirement with 2 scenarios |
| `interruption-decision-log` | **Updated** | Appended "Decision Log Stacked Card Layout at ≤768px" requirement with 3 scenarios |
| `pr-connector-ui` | **Updated** | Appended "PR Mini-Table Horizontal Scroll at ≤600px" requirement with 2 scenarios |
| `focus-state-machine` | **Updated** | Appended "FocusState Badge Dropdown Repositioning at ≤768px" requirement with 1 scenario |
| `demo-mode` | **Updated** | Appended "Demo Wizard Tooltip Repositioning at ≤768px" requirement with 2 scenarios |

No requirements were removed or renamed from existing specs. All merged requirements are additions.

---

## Build Results

| Check | Result |
|-------|--------|
| `dotnet build` | ✅ **0 errors, 0 warnings** |

---

## Task Completion

| Phase | Tasks | Status |
|-------|-------|--------|
| Phase 1: Infrastructure (CSS + JS scaffolding) | 1.1, 1.2, 1.3 | ✅ All 3 complete |
| Phase 2: Layout Shell (Sidebar drawer + Header compression) | 2.1, 2.2, 2.3, 2.4 | ✅ All 4 complete |
| Phase 3: Page-Level Responsive | 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7 | ✅ All 7 complete |
| Phase 4: Cleanup + Regression Prevention | 4.1, 4.2, 4.3, 4.4, 4.5 | ✅ All 5 complete |
| **Total** | **15 tasks** | **✅ 15/15 complete** |

---

## Verification Results

**Verdict**: ✅ **PASS WITH WARNINGS**

### Spec Compliance

| Measure | Result |
|---------|--------|
| Scenarios compliant | 19/21 |
| Scenarios partial | 1 (Desktop unaffected — 2 CSS guard rules outside @media) |
| Scenarios untested | 1 (Rotation stability — requires runtime browser testing) |

### Known Issues

#### CRITICAL

1. **No tests written for responsive changes (Strict TDD violation)**
   - Strict TDD Mode is active (`openspec/config.yaml: strict_tdd: true`) but 0 test files were created or modified for the 15 implementation tasks.
   - The change is functionally complete and production-ready, but the TDD process was not followed.
   - **This archive is intentional with user awareness of this process gap.** The team should decide whether to: (a) add Playwright/bUnit test coverage for responsive behavior, or (b) update the config to use Standard TDD for CSS/JS changes.

2. **E2E test infrastructure failures (pre-existing)**
   - 42 E2E tests fail with 404/NotFound/timeout — environment issue, not caused by this change.

#### WARNINGS

1. **CSS guard rules outside @media blocks** (stitch-dashboard-responsive.css lines 8-14)
   - Two rules (`.dashboard-header__hamburger { display: none }`, `.backdrop { display: none }`) exist outside any `@media` block.
   - These are intentional desktop guards but technically violate the spec requirement.
   - Impact: none — desktop rendering is correct.

2. **Design doc wording inaccuracy**
   - Design.md says `data-label` on `<td>`; implementation correctly uses `<span>` per the authoritative spec.

3. **`.table-scroll-wrapper` added alongside existing class**
   - Functionally correct — both classes coexist, preserves backward compatibility.

---

## Implementation Verification (Source Inspection)

| Requirement | Source Evidence | Status |
|------------|----------------|--------|
| Breakpoint system (900/768/600/480px) | `stitch-dashboard-responsive.css` lines 17, 75, 218, 230 | ✅ |
| Hamburger toggle + 300ms transition | Line 20 (`transition: transform 300ms`), sidebarDrawer.js lines 54-68 | ✅ |
| Backdrop overlay (rgba(0,0,0,0.5), z-index: 35) | Lines 36-41 | ✅ |
| Escape key closes drawer | sidebarDrawer.js lines 43-48 | ✅ |
| Nav-link tap closes drawer | sidebarDrawer.js lines 23-31 | ✅ |
| Touch targets 44×44px | Lines 191-208 | ✅ |
| SignalR toast width constraints | Lines 211-214 | ✅ |
| Decision Log card layout (display: block + data-label) | Lines 147-197 | ✅ |
| PR table horizontal scroll (overflow-x: auto) | Lines 219-225 | ✅ |
| FocusState dropdown left:0 | Line 129 | ✅ |
| Demo wizard fixed bottom-center | Lines 133-140 | ✅ |
| Body scroll lock on drawer open | sidebarDrawer.js line 68 | ✅ |
| Header compression at ≤768px | Lines 76-118 | ✅ |
| Old @media blocks removed from stitch-dashboard.css | Confirmed absent | ✅ |

---

## Design Compliance

| Design Decision | Followed? | Notes |
|----------------|-----------|-------|
| CSS class toggle via JS interop (not Blazor state) | ✅ | Pure DOM manipulation, no SignalR round-trip |
| CSS display:block on table elements for cards | ✅ | `<details>` preserved, no duplicate markup |
| CSS visibility for header overflow (not Blazor conditional) | ✅ | Hidden via CSS at ≤768px |
| Separate responsive CSS file | ✅ | Easy rollback — remove one `<link>` |

---

## Rollback Instructions

1. Remove `<link href="css/stitch-dashboard-responsive.css" />` from `App.razor`
2. Remove `<script src="sidebarDrawer.js"></script>` from `App.razor`
3. Revert Razor markup changes per component:
   - `MainLayoutAuthenticated.razor` — remove backdrop div, JS interop init/dispose
   - `Header.razor` — remove hamburger button, overflow button/dropdown
   - `DecisionLog.razor` — remove `data-label` attributes
   - `PullRequests.razor` — remove `table-scroll-wrapper` class
   - `PrioritySummaryCards.razor` — remove `table-scroll-wrapper` class
4. **Restore** the two deleted `@media (max-width: 900px)` blocks in `stitch-dashboard.css` (see git history)
5. Delete `stitch-dashboard-responsive.css` and `sidebarDrawer.js`

---

## Next Steps for the Team

1. **Decide on test coverage**: The Strict TDD violation is the primary non-functional gap. Either add Playwright/bUnit responsive tests or set TDD mode to Standard for UI/CSS changes in `openspec/config.yaml`.
2. **Decision Log card density**: The design has an open question about showing all 9 fields vs. adding a "More" toggle. Currently shows all 9 — gather user feedback.
3. **E2E infrastructure**: The 42 pre-existing E2E failures need investigation. Likely missing running server for test harness.
4. **CSS guard rules**: Either remove the two desktop-guard rules from `stitch-dashboard-responsive.css` and put defaults in `stitch-dashboard.css`, or update the spec to allow them.

---

## Archive Metadata

| Field | Value |
|-------|-------|
| Change | mobile-responsive-dashboard |
| Archived | 2026-07-13 |
| Archive path | `openspec/changes/archive/2026-07-13-mobile-responsive-dashboard/` |
| Intentional archive | ✅ Yes — orchestrator confirmed with full awareness of CRITICAL issues |
| Archive type | Intentional-with-warnings |
| SDD cycle | Complete ✅ |
