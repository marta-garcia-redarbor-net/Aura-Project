# Design: PR Connector UI — Stitch Alignment

## Technical Approach

Extend `PullRequestResponse` with 6 new fields, introduce `PrPreviewItemResponse` to replace `InboxItemPreviewResponse` for PR dashboard cards, co-locate PR card rendering as a branch in `PrioritySummaryCards.razor`, rebuild `PullRequests.razor` table layout per Stitch wireframes, and add CSS for CI badges, mini-table, filter bar, pagination, and right panel placeholder. All new UI controls are visual-only v1 — no state, no API calls.

## Architecture Decisions

| Option | Tradeoff | Decision |
|--------|----------|----------|
| `IsPrCard` in same `PrioritySummaryCards.razor` vs separate component | Separate = cleaner but adds file ceremony for 1 branch condition | **Same file** — minimizes file count; branch sits before generic `else` |
| `PrPreviewItemResponse` as interface vs record | Interface = extensibility overhead for v1 with zero consumers | **Simple record** — sealed, positional, no interface |
| Dashboard card width for mini-table | Default grid 20rem causes wrapping; dedicated modifier needed | **`.priority-card--pr`** CSS class sets `min-width: 24rem` |
| Filter bar / pagination state | Query params + backend = accurate but out of scope for v1 | **Visual-only** — static HTML, no `@bind`, no `@onchange` handlers |
| Right panel | Separate component vs inline div | **Inline `<aside>`** in `PullRequests.razor`, subtle styling, no interactivity |

## Architecture Overview

```
[Data Model]
  PullRequestResponse (+6 fields)
  PrPreviewItemResponse (new record)

[Services]
  AzureDevOpsPrClient ──┬──→ PullRequests.razor (direct injection, detail page)
                        │
                        └──→ PrioritySummaryService ──→ PrioritySummaryCards.razor (dashboard)

[Components]
  PriorityDashboard.razor
    └── PrioritySummaryCards.razor
          ├── card.IsCalendarCard → schedule template
          ├── card.IsPrCard → PR mini-table (NEW)
          └── else → generic inbox list

  PullRequests.razor (self-contained, no sub-components for v1)
    ├── Page header (existing)
    ├── Filter bar (NEW — visual-only)
    ├── Table (rebuilt columns)
    ├── Pagination (NEW — visual-only)
    └── Right panel placeholder (NEW — visual-only)
```

## Component Tree

```
PriorityDashboard
  └── PrioritySummaryCards
        ├── [error state]
        ├── [loading state]
        ├── [empty per-card → "No items"]
        ├── card.IsCalendarCard → schedule-item template
        ├── card.IsPrCard → .pr-mini-table (NEW)
        └── else → .priority-card__item generic list

PullRequests (page)
  ├── dashboard-page-header
  ├── [loading → pr-loading]
  ├── [empty → pr-empty]
  ├── [error → pr-error + retry]
  └── [populated]
        ├── .pr-filter-bar (NEW)
        ├── table.pr-list__table
        │     ├── thead: Priority | PR Name | CI Status | Reviews | Last Activity | Action
        │     └── tbody: pr-row
        ├── .pr-pagination (NEW)
        └── aside.pr-right-panel (NEW)
```

## Data Flow

```
AzureDevOpsPrClient                PrioritySummaryService
─────────────────────              ─────────────────────
GetPendingPullRequestsAsync()      GetCardsAsync()
  │                                  ├─ _previewApiClient.GetPreviewAsync()
  │                                  ├─ _calendarApiClient.GetUpcomingMeetingsAsync()
  │                                  └─ _prClient.GetPendingPullRequestsAsync()
  │                                       │
  │                                  BuildCards() maps PRs → PrPreviewItemResponse
  │                                       │
  │                                  PrioritySummaryCard {
  │                                    IsPrCard = true,
  │                                    PrItems = List<PrPreviewItemResponse>
  │                                  }
  │                                       │
  ▼                                       ▼
PullRequests.razor                  PrioritySummaryCards.razor
(direct injection)                  (receives via IPrioritySummaryService)
  │                                    │
  └── table rows                     └── if (card.IsPrCard) → .pr-mini-table
```

## Model Changes

### PullRequestResponse — 6 new fields
```csharp
public sealed record PullRequestResponse(
    int Id, string Title, string RepoName, string Author,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    string Status, int ReviewerCount, int CommentCount, int FileCount,
    string SourceLink, bool IsDraft, string Priority,
    string BranchName,               // NEW
    string SourceBranchName,          // NEW
    string BuildStatus,               // NEW — "passing"|"running"|"failed"|"pending"|"unknown"
    int ReviewApprovals,              // NEW
    int ReviewRequired,               // NEW
    int ReviewChangesRequested        // NEW
);
```
`BuildStatus` default `"pending"` enforced by `AzureDevOpsPrClient` mock data.

### New — PrPreviewItemResponse
```csharp
public sealed record PrPreviewItemResponse(
    string Title,
    string PrDisplayName,    // "#{Id} {Title}"
    string BranchName,
    string BuildStatus,
    int ReviewApprovals,
    int ReviewRequired,
    string Author,
    DateTimeOffset UpdatedAt,
    string RelativeTimestamp,
    string SourceLink,
    bool IsDraft,
    string Priority
);
```

### PrioritySummaryCard — extended
```csharp
public sealed record PrioritySummaryCard(
    // ... existing params unchanged ...
    List<InboxItemPreviewResponse>? PreviewItems,
    List<UpcomingMeetingResponse>? CalendarItems)
{
    public bool IsCalendarCard => CalendarItems is not null;
    public bool IsPrCard { get; init; }                    // NEW
    public List<PrPreviewItemResponse>? PrItems { get; init; }  // NEW
    public int TotalCount =>
        PrItems?.Count ?? PreviewItems?.Count ?? CalendarItems?.Count ?? 0;  // MODIFIED
}
```

### PrioritySummaryService.BuildCards — PR card now uses PrPreviewItemResponse
```csharp
// Card index 3:
new PrioritySummaryCard(
    DisplayName: "Pull Requests", // ... other fields unchanged ...
    PreviewItems: null,
    CalendarItems: null)
{
    IsPrCard = true,
    PrItems = prs.Select(p => new PrPreviewItemResponse(...)).ToList()
};
```

## Dashboard Card — Mini-Table Wireframe

```
┌──────────────────────────────────────────────────────┐
│ {icon} Pull Requests                        {N} PENDING│
├──────────────────────────────────────────────────────┤
│ PR NAME    │ BRANCH    │ CI      │ REVIEWS  │ ACTION │
│────────────┼───────────┼─────────┼──────────┼────────┤
│ Hotfix:…   │ main ← fix│ ✅ pass │ 2/3 +1ch │ 🔗     │
│ Feature:…  │ dev ← feat│ ⏳ run  │ 0/2 +0ch │ 🔗     │
│ Refactor…  │ main ← ref│ ❌ fail │ 1/1 +3ch │ 🔗     │
├──────────────────────────────────────────────────────┤
│ View all 6 PRs →                      Open Azure DevOps ↗│
└──────────────────────────────────────────────────────┘
```

### CSS class structure for mini-table
```
. pr-mini-table                          — borderless, compact table inside card
  . pr-mini-table__header                — hidden on mobile if needed
  . pr-mini-table__row                   — row for each PR item
    . pr-mini-table__cell--name          — PR display name (truncated)
    . pr-mini-table__cell--branch         — branch pair
    . pr-mini-table__cell--ci             — CI badge wrapper
    . pr-mini-table__cell--reviews       — "X/Y" + changes
    . pr-mini-table__cell--action         — open_in_new icon
. pr-branch                              — branch name text
  . pr-branch--source / pr-branch--target
. priority-card--pr                      — card width override (min-width: 24rem)
```

### CI badge CSS (also reused in PullRequests page)
```
.ci-status             — base capsule
.ci-status--passing    — green (#10B981)
.ci-status--running    — amber (#F59E0B)
.ci-status--failed     — red (#FF4B4B)
.ci-status--pending    — slate (#64748B)
```

### Reviewer progress display
```
.pr-review-status                    — inline text "X/Y +Nch"
  .pr-review-status__approvals       — green count
  .pr-review-status__changes         — muted red count
```

### Footer
- Default `priority-card__footer` reused; footer link text changes to "View All Repositories" via `card.SourceLabel`.

## PullRequests Page — Wireframe

```
┌──────────────────────────────────────────────────────────────────┐
│ Pull Requests                                          {N} pending│
│ Pending PRs requiring your review...                             │
├──────────────────────────────────────────────────────────────────┤
│ [Filter bar: visual-only controls]                                │
│ ┌──────────────────┐ ┌──────────┐ ┌──────────┐                   │
│ │ 🔍 Search...      │ │ All Repo▼│ │ Priority▼│                   │
│ └──────────────────┘ └──────────┘ └──────────┘                   │
├──────────────────────────────────────────────┬───────────────────┤
│ PRIORITY │ PR NAME     │ CI    │ REVIEWS     │                   │
│          │             │       │ / ACT.      │                   │
│──────────┼─────────────┼───────┼─────────────┤                   │
│ 🔴 crit  │ #142 Hotfix…│ ✅    │ 2/3 +1ch    │                   │
│          │ main ← fix  │ pass  │ 2h ago      │  RIGHT PANEL     │
│──────────┼─────────────┼───────┼─────────────┤                   │
│ 🟡 high  │ #145 Feat…  │ ⏳    │ 0/2 +0ch    │  (placeholder)   │
│          │ dev ← feat  │ run   │ 4h ago      │                   │
│──────────┼─────────────┼───────┼─────────────┤                   │
│ 🟢 low   │ #150 Docs…  │ ⬜    │ 1/1 +0ch    │                   │
│          │ main ← doc  │ pend  │ 1d ago      │                   │
├──────────────────────────────────────────────┴───────────────────┤
│ ◀ 1 2 3 … 12 ▶  Showing 1-6 of 72                    [visual-only]│
└──────────────────────────────────────────────────────────────────┘
```

### CSS class structure for PullRequests page
```
. pr-filter-bar
  . pr-filter-bar__search
  . pr-filter-bar__dropdown
. pr-list__table (existing, rethemed columns)
  . pr-list__cell--priority
  . pr-list__cell--name         — PR display name + branch pair
    . pr-list__branch
      . pr-list__branch--source
      . pr-list__branch--target
  . pr-list__cell--ci           — .ci-status capsule
  . pr-list__cell--reviews      — review approvals + changes
  . pr-list__cell--activity     — relative timestamp
  . pr-list__cell--action       — open_in_new
. pr-pagination
  . pr-pagination__info         — "Showing 1-6 of 72"
  . pr-pagination__controls     — prev/next + page buttons
. pr-right-panel                — fixed-width column (~320px)
  . pr-right-panel__placeholder — subtle dashed border, muted text
```

### Right panel placeholder
```html
<aside class="pr-right-panel" data-testid="pr-right-panel">
  <div class="pr-right-panel__placeholder">
    <span class="material-symbols-outlined">info</span>
    <p>Select a PR to view details</p>
  </div>
</aside>
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.UI/Models/PullRequestResponse.cs` | Modify | Add 6 fields: BranchName, SourceBranchName, BuildStatus, ReviewApprovals, ReviewRequired, ReviewChangesRequested |
| `src/Aura.UI/Models/PrPreviewItemResponse.cs` | Create | New record for PR dashboard preview items |
| `src/Aura.UI/Services/IPrioritySummaryService.cs` | Modify | Add IsPrCard + PrItems to PrioritySummaryCard; update TotalCount |
| `src/Aura.UI/Services/PrioritySummaryService.cs` | Modify | BuildCards maps PRs → PrPreviewItemResponse; PR card sets IsPrCard=true |
| `src/Aura.UI/Services/AzureDevOpsPrClient.cs` | Modify | Populate 6 new fields in all 6 mock PRs |
| `src/Aura.UI/Components/Dashboard/PrioritySummaryCards.razor` | Modify | Add `else if (card.IsPrCard)` branch with mini-table before generic `else` |
| `src/Aura.UI/Pages/PullRequests.razor` | Modify | Rebuild table columns, add filter bar, pagination, right panel |
| `src/Aura.UI/wwwroot/css/stitch-dashboard.css` | Modify | Add CI status badges, PR mini-table, filter bar, pagination, right panel placeholder, reviewer initials styles |
| `tests/Aura.UnitTests/Pages/PullRequestsPageTests.cs` | Modify | Update PR constructors with 6 new fields; add column assertions |
| `tests/Aura.UnitTests/Dashboard/PrioritySummaryCardsRenderingTests.cs` | Modify | Update card constructors; add test for PR mini-table rendering |
| `tests/Aura.E2E/PullRequests/PullRequestsPageSmokeTests.cs` | Modify | Update PR constructors; add testids `pr-filter-bar`, `pr-pagination`, `pr-ci-status`, `pr-review-status` |

## Interfaces / Contracts

```csharp
// No new interfaces. PrPreviewItemResponse is a concrete record.
// IAzureDevOpsPrClient and IPrioritySummaryService remain unchanged.
```

## Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| Unit — model | `PrPreviewItemResponse` construction, field defaults, `PrDisplayName` format | Direct assertions on new record |
| Unit — service | PR card in `BuildCards` sets `IsPrCard=true`, `PrItems` count matches input | Mock `IAzureDevOpsPrClient`, verify card index 3 |
| Unit — card rendering | PR mini-table renders 3 items max, `TotalCount` shows 6, footer shows "View All Repositories" | bUnit `RenderComponent<PrioritySummaryCards>` with PR card |
| Unit — page | Updated columns (Priority, PR Name, CI, Reviews), preserved testids (`pr-row`, `pr-status`, `pr-open-link`), new testids | bUnit `RenderComponent<Aura.UI.Pages.PullRequests>` |
| E2E — smoke | HTTP response contains new testids `pr-filter-bar`, `pr-pagination`, `pr-ci-status`, `pr-review-status` | `WebApplicationFactory`, scrape HTML string |

### Test constructors — all `PullRequestResponse` creations must add 6 positional args
```csharp
// Before (11 params):
new PullRequestResponse(142, "...", "Aura", "Carlos", ..., false, "critical");

// After (17 params):
new PullRequestResponse(142, "...", "Aura", "Carlos", ..., false, "critical",
    "feature/foo", "main", "passing", 2, 3, 1);
```

## State Management

No new state for v1. Existing `PrListState` (Loading / Empty / Error / Populated) unchanged. Filter bar, pagination, and right panel render static HTML only — no `@bind`, no `@onchange`, no query params.

## Performance Considerations

- Dashboard mini-table caps at 3 PRs (`.Take(3)`) — negligible render cost.
- PullRequests table shows 6 items in v1 mock — no virtualization needed.
- No new API calls or polling introduced.
- CSS additions are static — no runtime style computation.

## Migration / Rollout

No migration required. All changes are additive or modify mock data. Existing `PrioritySummaryCard` consumers (tests, services) compile-fail until updated — coordinate test updates with model changes.

## Open Questions

- None. All decisions documented and aligned with spec constraints.
