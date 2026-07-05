# Apply Progress: W3-H3 UI Top Priority Queue Tweak

## Completed Scope

- Moved priority counters from `PriorityDashboard` into `Header` nav area and wired click navigation to `/top-priority`.
- Added `TopPriority.razor` page at `/top-priority`.
- Added `Top Priority Queue` sidebar entry in second position (after Dashboard).
- Removed top-priority summary section from `PriorityDashboard`.
- Added subtle top-priority card badge (red dot + tooltip) on source cards (Teams, Outlook, PRs) using `PriorityPresentation` helpers.
- Added/updated bUnit tests for header navigation/counters, sidebar second-position route target, and source-card top-priority badges.
- Restored header pending/high-priority counter color styling by reusing legacy badge classes from the prior PriorityDashboard counters (`dashboard-page-header__badge--live` and `dashboard-page-header__badge--high`) without changing global color tokens.
- Reworked `/top-priority` to render one flat list of pending work items across Teams/Outlook/PR sources, sorted by `PriorityScore DESC` then `CapturedAtUtc DESC`, with visible `PriorityScore` and detail links.
- Added targeted bUnit coverage for header badge class parity and flat `/top-priority` rendering with ordering assertions and no grouping containers.
- Replaced PR body Summary section with the updated concise text in `openspec/changes/W3-H3/pr-body.md`.
- Added per-card high-priority counters beside source card `new` counts (Teams, Outlook, PRs) with accessible `aria-label` format `{N} high priority` and neutral badge styling.
- Updated `/top-priority` to use a single centered-column feed layout (no right-side panel) while preserving item links and pagination.
- Added targeted TDD tests for `GetHighPriorityCount` (`>= 75`), high-priority counter rendering/aria on summary cards, and single-column `/top-priority` layout without `right-panel`.
- Moved the global pending/high-priority counter from `Header` into `PriorityDashboard` header replacing `Live Sync`, preserving legacy badge color classes and click navigation to `/top-priority`.
- Updated `/top-priority` detail rendering to align with Stitch "Unified Priority Feed (detail)" structure using a single-column detail layout without right panel while keeping pagination, item links, and visible `PriorityScore`.
- Added targeted bUnit tests for dashboard header counter replacement/navigation and top-priority detail layout markers (single-column, no right panel, detail-item markup present).

## TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| Move counters + header navigation | `tests/Aura.UnitTests/Dashboard/HeaderSignOutTests.cs` | Unit (bUnit) | ✅ Existing targeted suite passing | ✅ Written first | ✅ Passed (`HeaderSignOutTests`) | ✅ Added render + click cases | ✅ Minor cleanup via DI setup reuse |
| Sidebar second entry + route target | `tests/Aura.UnitTests/Dashboard/SidebarNavigationTests.cs` | Unit (bUnit) | ✅ Existing targeted suite passing | ✅ Written first | ✅ Passed (`SidebarNavigationTests`) | ✅ Position + href assertions | ➖ None needed |
| Remove dashboard top-priority summary | `tests/Aura.UnitTests/Dashboard/PriorityDashboardPriorityIndicatorsTests.cs` | Unit (bUnit) | ✅ Existing targeted suite passing | ✅ Written first | ✅ Passed (`PriorityDashboardPriorityIndicatorsTests`) | ✅ Asserts both panel + counters removed | ➖ None needed |
| Source card top-priority badge | `tests/Aura.UnitTests/Dashboard/PrioritySummaryCardsRenderingTests.cs` | Unit (bUnit) | ✅ Existing targeted suite passing | ✅ Written first | ✅ Passed (`PrioritySummaryCardsRenderingTests`) | ✅ Teams + Outlook + PR scenarios | ✅ Extracted helper methods in `PriorityPresentation` |
| Restore header counter legacy color classes | `tests/Aura.UnitTests/Dashboard/HeaderSignOutTests.cs` | Unit (bUnit) | ✅ Existing targeted suite passing | ✅ Written first | ✅ Passed (`HeaderSignOutTests`) | ✅ Badge class assertions for both counters | ✅ Updated existing text assertion to independent badges |
| Flat `/top-priority` sorted list + no grouping | `tests/Aura.UnitTests/Dashboard/TopPriorityPageTests.cs`, `tests/Aura.UnitTests/Dashboard/PriorityPresentationTests.cs` | Unit (bUnit + pure logic) | ✅ Existing targeted suite passing | ✅ Written first | ✅ Passed (`TopPriorityPageTests`, `PriorityPresentationTests`) | ✅ Priority + recency ordering and no grouping checks | ✅ Added reusable sorting helper in `PriorityPresentation` |
| PR body summary refresh | `N/A (doc-only)` | Docs | N/A (new file) | ✅ Written first | ✅ Saved (`pr-body.md`) | ➖ Single output | ➖ None needed |
| Per-card high-priority counters + aria labels | `tests/Aura.UnitTests/Dashboard/PrioritySummaryCardsRenderingTests.cs` | Unit (bUnit) | ✅ Existing targeted suite passing | ✅ Written first | ✅ Passed (`PrioritySummaryCardsRenderingTests`) | ✅ Counter presence + value + aria assertions | ✅ Reused `PriorityPresentation` helpers |
| High-priority count helper (`>=75`) | `tests/Aura.UnitTests/Dashboard/PriorityPresentationTests.cs` | Unit (pure logic) | ✅ Existing targeted suite passing | ✅ Written first | ✅ Passed (`PriorityPresentationTests`) | ✅ Includes boundary and null cases | ✅ Added reusable PR priority mapping helper |
| Single-column `/top-priority` layout (no right panel) | `tests/Aura.UnitTests/Dashboard/TopPriorityPageTests.cs` | Unit (bUnit) | ✅ Existing targeted suite passing | ✅ Written first | ✅ Passed (`TopPriorityPageTests`) | ✅ Existing sort test + new layout assertion | ➖ None needed |
| Move counter into `PriorityDashboard` header replacing `Live Sync` | `tests/Aura.UnitTests/Dashboard/PriorityDashboardPriorityIndicatorsTests.cs` | Unit (bUnit) | ✅ Existing targeted suite passing | ✅ Written first | ✅ Passed (`PriorityDashboardPriorityIndicatorsTests`) | ✅ Placement + text + click-navigation assertions | ✅ Removed old header counter dependency |
| Stitch detail layout marker for `/top-priority` detail | `tests/Aura.UnitTests/Dashboard/TopPriorityPageTests.cs` | Unit (bUnit) | ✅ Existing targeted suite passing | ✅ Written first | ✅ Passed (`TopPriorityPageTests`) | ✅ Single-column + no-right-panel + detail-item markers | ✅ Reused existing flat-feed layout classes |

## Test Summary

- **Targeted command**:
  - `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~HeaderSignOutTests|FullyQualifiedName~HeaderFocusStateBadgeTests|FullyQualifiedName~SidebarNavigationTests|FullyQualifiedName~PrioritySummaryCardsRenderingTests|FullyQualifiedName~PriorityDashboardPriorityIndicatorsTests"`
  - `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~HeaderSignOutTests|FullyQualifiedName~PriorityPresentationTests|FullyQualifiedName~TopPriorityPageTests"`
  - `dotnet test tests/Aura.UnitTests --filter "HeaderSignOutTests|TopPriorityPageTests|PriorityPresentationTests|PrioritySummaryCardsRenderingTests"`
  - `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~HeaderSignOutTests|FullyQualifiedName~PriorityDashboardPriorityIndicatorsTests|FullyQualifiedName~TopPriorityPageTests" -p:RunAnalyzers=false`
- **Result**: 37 passed, 0 failed (targeted scope for this batch, including W3-H3 surgical fixes).
