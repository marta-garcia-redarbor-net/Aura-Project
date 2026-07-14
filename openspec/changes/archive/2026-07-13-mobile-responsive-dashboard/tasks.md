# Tasks: Mobile Responsive Dashboard

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~520 (additions + deletions) |
| 400-line budget risk | Medium |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | single-pr |
| Chain strategy | pending |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Medium

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Full responsive adaptation | PR 1 | CSS + Razor + JS; all under 800-line review budget |

## Phase 1: Infrastructure (CSS + JS scaffolding)

- [x] 1.1 Create `src/Aura.UI/wwwroot/css/stitch-dashboard-responsive.css` with empty `@media` skeleton for 900px, 768px, 480px breakpoints
- [x] 1.2 Create `src/Aura.UI/wwwroot/js/sidebarDrawer.js` with `init()`, `toggleDrawer()`, `closeDrawer()`, `dispose()` — toggle `.dashboard-sidebar--open` on `<aside>`, toggle `.backdrop--visible`, body `overflow: hidden` lock on open, Escape key listener, nav-link click auto-close
- [x] 1.3 Modify `src/Aura.UI/Components/App.razor`: add `<link rel="stylesheet" href="css/stitch-dashboard-responsive.css" />` after `stitch-dashboard.css` (line 12), add `<script src="sidebarDrawer.js"></script>` after existing scripts

## Phase 2: Layout Shell (Sidebar drawer + Header compression)

- [x] 2.1 Modify `src/Aura.UI/Components/Layout/MainLayoutAuthenticated.razor`: add `<div class="backdrop" @onclick="CloseDrawer"></div>` after `<Sidebar />`; inject `IJSObjectReference` for `sidebarDrawer`; call `sidebarDrawer.init()` from `OnAfterRenderAsync(firstRender: true)`; call `sidebarDrawer.dispose()` from existing `Dispose()` method
- [x] 2.2 Modify `src/Aura.UI/Components/Layout/Header.razor`: add hamburger `<button class="dashboard-header__hamburger">` before `__identity` div; add overflow `<button class="dashboard-header__overflow-btn">` + `<div class="dashboard-header__overflow-dropdown">` containing Demo controls + FocusStateBadge duplicate; add `@onclick` for overflow toggle
- [x] 2.3 Populate `stitch-dashboard-responsive.css` ≤900px block: sidebar `position: fixed; transform: translateX(-100%); transition: transform 300ms`, `.dashboard-sidebar--open` → `translateX(0)`, `.backdrop--visible` → `display: block; position: fixed; inset: 0; background: rgba(0,0,0,0.5); z-index: 35`, hamburger `display: inline-flex`, content `padding-left: 0`
- [x] 2.4 Populate ≤768px block: header `__demo-group` + `FocusStateBadge` → `display: none`, overflow-btn `display: inline-flex`, overflow-dropdown visible when `.overflow-dropdown--open`, user name hidden (icon only), `.priority-card--pr { min-width: 0 }`, FocusState dropdown `right: auto; left: 0`, demo wizard `position: fixed; bottom: 1rem; left: 50%; transform: translateX(-50%); z-index: 30`

## Phase 3: Page-Level Responsive (Decision Log + PR tables + Touch targets)

- [x] 3.1 Modify `src/Aura.UI/Pages/DecisionLog.razor`: add `data-label` attributes to each `<span>` inside `.decision-trace__grid` — `data-label="Timestamp"`, `data-label="Title"`, `data-label="Source"`, `data-label="Score"`, `data-label="Decision"`, `data-label="Focus"`, `data-label="Explanation"`, `data-label="Guardrail"` (chevron span gets no label)
- [x] 3.2 Populate ≤768px CSS for Decision Log: `.decision-log-table, .decision-log-table thead, .decision-log-table tbody, .decision-log-table th, .decision-log-table tr, .decision-trace__grid` → `display: block`, each `<span[data-label]>::before { content: attr(data-label); font-weight: 600 }`, pagination buttons `min-height: 44px; min-width: 44px`
- [x] 3.3 Modify `src/Aura.UI/Pages/PullRequests.razor`: add class `table-scroll-wrapper` to existing `.pr-list__table-wrapper` div (line 57)
- [x] 3.4 Modify `src/Aura.UI/Components/Dashboard/PrioritySummaryCards.razor`: add class `table-scroll-wrapper` to existing `.pr-mini-table-container` div (line 89)
- [x] 3.5 Populate ≤600px CSS: `.table-scroll-wrapper { overflow-x: auto; -webkit-overflow-scrolling: touch }`, ensure tables inside retain `min-width` to force scroll
- [x] 3.6 Populate ≤768px touch-target rules: `button, a, .clickable, [role="button"], .dashboard-sidebar__nav-item, .decision-log__page { min-height: 44px; min-width: 44px }`
- [x] 3.7 Populate ≤480px block: page title `font-size: 18px`, subtitle `text-overflow: ellipsis`, content padding `12px`, status badges `flex-wrap: wrap`, footer `flex-direction: column`

## Phase 4: Cleanup + Regression Prevention

- [x] 4.1 Remove existing `@media (max-width: 900px)` block (lines 1135–1154) from `src/Aura.UI/wwwroot/css/stitch-dashboard.css`
- [x] 4.2 Remove existing `@media (max-width: 900px)` block for `.priority-summary-cards` (lines 1744–1748) from `stitch-dashboard.css` — relocated to responsive file
- [x] 4.3 Populate ≤768px toast rules: `.toast-container { max-width: calc(100vw - 32px); min-width: 280px }`, ensure no overlap with hamburger
- [x] 4.4 Verify: desktop at ≥1024px is visually identical (no responsive rules leak), sidebar always visible, hamburger hidden
- [x] 4.5 Verify: rotation stability — no JS errors on resize, sidebar state resets correctly crossing 900px threshold

## Rollback Plan (Gate Finding #6)

To rollback: (1) remove `<link>` for `stitch-dashboard-responsive.css` from `App.razor`, (2) remove `<script src="sidebarDrawer.js">` from `App.razor`, (3) revert Razor markup changes per component, (4) **restore the two deleted `@media (max-width: 900px)` blocks** in `stitch-dashboard.css` (lines 1135–1154 and 1744–1748). Keep the original blocks documented in git history for exact restoration.

## Gate Findings Integration

| # | Finding | Addressed In |
|---|---------|-------------|
| 1 | Decision Log uses `<span>` not `<td>` for `data-label` | Task 3.1 — `data-label` on `<span>` inside `.decision-trace__grid` |
| 2 | JS lifecycle: `init()` from `OnAfterRenderAsync(firstRender)`, `dispose()` from `IDisposable.Dispose()` | Task 2.1 — explicit lifecycle hooks in MainLayoutAuthenticated |
| 3 | Body scroll lock: `overflow: hidden` on `<body>` when drawer open | Task 1.2 — `sidebarDrawer.js` manages body overflow |
| 4 | `.priority-card--pr` min-width: 0 at ≤768px | Task 2.4 — explicit rule in ≤768px block |
| 5 | `.pr-mini-table-container` vs `.table-scroll-wrapper` | Tasks 3.3/3.4 — add `.table-scroll-wrapper` alongside existing class; style `.table-scroll-wrapper` in responsive CSS |
| 6 | Rollback must restore deleted `@media` blocks | Task 4.1/4.2 + Rollback Plan section above |
