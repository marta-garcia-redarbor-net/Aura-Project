# Tasks: W3-H3 UI Top Priority Queue Tweak

## UI Follow-up (Top Priority Queue)

- [x] Move priority counters to `Header.razor` replacing static header text and navigate to `/top-priority` on click.
- [x] Add `TopPriority.razor` at `/top-priority` and add `Top Priority Queue` sidebar entry in 2nd position.
- [x] Remove top-priority summary section from `PriorityDashboard.razor`.
- [x] Add subtle top-priority badge (red dot + tooltip) on Teams/Outlook/PR source cards when top-priority items are present.
- [x] Add bUnit tests for header navigation, sidebar ordering/route target, and source-card top-priority badges.
- [x] Restore header pending/high-priority counter color classes using legacy PriorityDashboard badge classes (no global token changes).
- [x] Update `/top-priority` to render a flat sorted work-item list (PriorityScore DESC, CapturedAtUtc DESC), remove source grouping, and show priority score + detail link per item.
- [x] Add targeted bUnit tests for header counter class parity and flat sorted `/top-priority` rendering without grouping containers.

## UI Follow-up (High-Priority Counters + Single-Column Refinement)

- [x] Replace PR body Summary section text in `openspec/changes/W3-H3/pr-body.md`.
- [x] Add per-card high-priority counter next to existing `new` count for Teams/Outlook/PR cards with accessible aria-label.
- [x] Update `/top-priority` page layout to the Stitch single centered column feed with no right panel.
- [x] Add targeted tests for high-priority counter rendering/aria, single-column layout (no `right-panel`), and high-priority count logic (`>= 75`).

## UI Follow-up (W3-H3 Surgical Fixes)

- [x] Move global header counter into `PriorityDashboard` header replacing static `Live Sync`; keep existing badge classes/aria and clickable navigation to `/top-priority`.
- [x] Update `/top-priority` detail view to Stitch `Unified Priority Feed (detail)` style using a single-column detail layout with no right-side panel, while preserving pagination, item links, and prominent `PriorityScore`.
- [x] Add bUnit tests for dashboard-header counter placement/navigation and top-priority detail layout markers (single-column, no right panel, detail item markup present).
