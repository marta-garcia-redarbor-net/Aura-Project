# Tasks: Dashboard Estado Óptimo (Positive Empty States)

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~180 (5 files, mostly CSS + Razor template + small C# additions) |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | single-pr-default |
| Chain strategy | N/A |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: N/A
400-line budget risk: Low

## Phase 1: Model Extension

- [x] 1.1 Add 4 init-only properties to `PrioritySummaryCard` record in `src/Aura.UI/Services/IPrioritySummaryService.cs` (after line 20): `EmptyIcon`, `EmptyTitle`, `EmptySubtitle`, `EmptyLinkLabel` — all `string?` with `{ get; init; }`
- [x] 1.2 Verify existing 13 constructor call sites (tests + service) compile without changes — properties are optional with null defaults

## Phase 2: Data Population

- [x] 2.1 In `src/Aura.UI/Services/PrioritySummaryService.cs`, add init-only property assignments to the Teams card (L128-138): `EmptyIcon = "check_circle"`, `EmptyTitle = "Inbox Zero"`, `EmptySubtitle = "Everything is optimal. Your cognitive load is clear."`, `EmptyLinkLabel = "View All Mentions"`
- [x] 2.2 Add empty-state properties to the Outlook card (L139-149): `EmptyIcon = "mark_email_read"`, `EmptyTitle = "All Caught Up"`, `EmptySubtitle = "Take a deep breath."`, `EmptyLinkLabel = "See All Emails"`
- [x] 2.3 Add empty-state properties to the Schedule card (L150-160): `EmptyIcon = "event_available"`, `EmptyTitle = "Schedule Clear"`, `EmptySubtitle = "No meetings for today. Enjoy your focused time."`, `EmptyLinkLabel = "View Full Schedule"`
- [x] 2.4 Add empty-state properties to the PR card (L161-175): `EmptyIcon = "verified"`, `EmptyTitle = "Queue Empty"`, `EmptySubtitle = "No pending reviews. Your workspace is clear."`, `EmptyLinkLabel = "View All Repositories"`

## Phase 3: Empty State Rendering

- [x] 3.1 In `src/Aura.UI/Components/Dashboard/PrioritySummaryCards.razor`, replace lines 55-61 (the "No items" `<ul>` block) with a `<div class="priority-card__empty-state" data-testid="priority-card-empty-state">` containing: Material Symbol icon (`@card.EmptyIcon`), title `<p>` (`@card.EmptyTitle`), subtitle `<p>` (`@card.EmptySubtitle`)
- [x] 3.2 Remove the `@if (!isEmpty)` guard on line 190 so the footer always renders. Inside the footer, conditionally render: when empty → `@card.EmptyLinkLabel` linking to `@card.ViewAllUrl`; when not empty → existing `"View all {count} {itemsLabel}"` text
- [x] 3.3 Keep the `priority-card--empty` CSS class on the `<article>` (line 26) for potential future use, but the opacity will be removed in Phase 4

## Phase 4: CSS Styles

- [x] 4.1 In `src/Aura.UI/wwwroot/css/stitch-dashboard.css`, replace the `.priority-card--empty` block (L1629-1631) and `.priority-card--empty .priority-card__items` block (L1633-1639) with new empty-state styles: `.priority-card__empty-state` (flex column, centered, gap 0.5rem), `.priority-card__empty-icon` (48px, healthy green), `.priority-card__empty-title` (15px, semibold), `.priority-card__empty-subtitle` (12px, muted, max-width 20ch, centered)

## Phase 5: Testing

- [x] 5.1 In `tests/Aura.UnitTests/Dashboard/PrioritySummaryCardsRenderingTests.cs`, add test `RendersEmptyState_WhenTeamsCardHasZeroItems`: construct Teams card with empty items + empty-state properties, render, assert icon text `check_circle`, title "Inbox Zero", subtitle "Everything is optimal..."
- [x] 5.2 Add test `RendersEmptyState_WhenOutlookCardHasZeroItems`: assert icon `mark_email_read`, title "All Caught Up", subtitle "Take a deep breath."
- [x] 5.3 Add test `RendersEmptyState_WhenScheduleCardHasZeroItems`: assert icon `event_available`, title "Schedule Clear", subtitle "No meetings for today..."
- [x] 5.4 Add test `RendersEmptyState_WhenPrCardHasZeroItems`: assert icon `verified`, title "Queue Empty", subtitle "No pending reviews..."
- [x] 5.5 Add test `RendersFooterWithEmptyLinkLabel_WhenCardIsEmpty`: assert footer is visible with `EmptyLinkLabel` text and links to `ViewAllUrl`
- [x] 5.6 Add test `DoesNotRenderEmptyState_WhenCardHasItems`: construct card with items, assert `QuerySelector("[data-testid='priority-card-empty-state']")` is null (regression)
- [x] 5.7 Run `dotnet test Aura.sln` — all existing + new tests pass
