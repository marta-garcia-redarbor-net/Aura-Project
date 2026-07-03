# Tasks: PR Connector UI — Stitch Alignment

## Review Workload Forecast

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: single-pr (size:exception — 835 lines, accepted by user)
400-line budget risk: High (resolved via size:exception)

| Field | Value |
|-------|-------|
| Estimated changed lines | ~835 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Delivery strategy | ask-on-risk → resolved as single PR (size:exception) |
| Chain strategy | single-pr — user explicitly accepted |

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Model foundation — PullRequestResponse fields + PrPreviewItemResponse + PrioritySummaryCard | PR 1 | ~75 lines. Base: `feature/pr-ui-stitch`. Pure data, no UI. |
| 2 | Dashboard card — PrioritySummaryService + card rendering | PR 2 | ~170 lines. Base: PR 1 branch. Visual-only mini-table. |
| 3 | PR page rebuild — PullRequests.razor new columns + filter + pagination + right panel | PR 3 | ~200 lines. Base: PR 2 branch. Preserve legacy testids. |
| 4 | CSS — all new UI styles | PR 4 | ~280 lines. Base: PR 3 branch. No new logic, only styling. |
| 5 | Tests — unit + E2E test updates | PR 5 | ~110 lines. Base: PR 4 branch. Compile and pass. |

## Phase 1: Model Changes

- [x] 1.1 Add 6 fields to `PullRequestResponse`: BranchName, SourceBranchName, BuildStatus, ReviewApprovals, ReviewRequired, ReviewChangesRequested — `src/Aura.UI/Models/PullRequestResponse.cs`
- [x] 1.2 Update all 6 mock PRs in `AzureDevOpsPrClient.cs` with realistic BranchName, SourceBranchName, BuildStatus, approval/request values
- [x] 1.3 Create `PrPreviewItemResponse` sealed record with Title, PrDisplayName, BranchName, BuildStatus, ReviewApprovals, ReviewRequired, Author, UpdatedAt, RelativeTimestamp, SourceLink, IsDraft, Priority — `src/Aura.UI/Models/PrPreviewItemResponse.cs`
- [x] 1.4 Add `IsPrCard` + `List<PrPreviewItemResponse>? PrItems` to `PrioritySummaryCard`; update `TotalCount` to prefer `PrItems?.Count` — `src/Aura.UI/Services/IPrioritySummaryService.cs`

## Phase 2: Dashboard Card

- [x] 2.1 In `PrioritySummaryService.BuildCards`, replace `InboxItemPreviewResponse` mapping with `PrPreviewItemResponse` for PR card (index 3); set `IsPrCard = true` — `src/Aura.UI/Services/PrioritySummaryService.cs`
- [x] 2.2 Add `else if (card.IsPrCard)` branch in `PrioritySummaryCards.razor` before generic else; render mini-table: PR NAME, BRANCH, CI STATUS, REVIEWS, LAST ACTIVITY, ACTION; cap items at 3; footer "View All Repositories"

## Phase 3: PR Page Rebuild

- [x] 3.1 Rebuild `PullRequests.razor` table with columns: Priority, PR Name (branch pair), CI Status (badge), Reviews (X/Y + changes), Last Activity, Action (open_in_new)
- [x] 3.2 Add visual-only filter bar: "Filter: Open" pill + "New PR" link button — no `@bind`, no `@onchange`
- [x] 3.3 Add visual-only pagination: "Showing X of Y pull requests" + page 1 — no state handlers
- [x] 3.4 Add right panel `<aside>` placeholder with "Aura Intelligence — Rule Engine (coming soon)"
- [x] 3.5 Preserve testids: `pr-loading`, `pr-empty`, `pr-error`, `pr-row`, `pr-open-link`; add `pr-filter-bar`, `pr-pagination`, `pr-ci-status`, `pr-review-status`

## Phase 4: CSS

- [x] 4.1 Add CI status badges: `.ci-status--passing` (#10B981), `--running` (#F59E0B), `--failed` (#FF4B4B), `--pending` (#64748B) — capsule style
- [x] 4.2 Add PR mini-table styles: `.pr-mini-table`, header, rows, cells for name/branch/ci/reviews/action
- [x] 4.3 Add `.pr-branch--source` / `--target` branch pair display
- [x] 4.4 Add `.priority-card--pr` width override (`min-width: 28rem`)
- [x] 4.5 Add filter bar layout: `.pr-filter-bar`, pill, button
- [x] 4.6 Add pagination: `.pr-pagination`, info text, controls
- [x] 4.7 Add right panel: `.pr-detail-layout`, `.pr-detail-sidebar`, placeholder with icon
- [x] 4.8 Add reviewer initials display: `.reviewer-initials`, avatar; `.pr-mini-table__reviews-approved` green, `--changes` warning

## Phase 5: Tests

- [x] 5.1 Update `PullRequestsPageTests.cs`: add 6 new positional args to all `PullRequestResponse` constructors; verify CI status badges + new testids; add PrPreviewItemResponse construction test
- [x] 5.2 Update `PrioritySummaryCardsRenderingTests.cs`: add PR card with PrItems + IsPrCard; verify mini-table renders with review approval text
- [x] 5.3 Update `PullRequestsPageSmokeTests.cs`: add 6 new positional args; assert new testids `pr-filter-bar`, `pr-pagination`, `pr-ci-status` exist

## Dependency Graph

```
1.1 ──→ 1.2        (model + mock data)
1.1 ──→ 1.3        (model + new record)
1.3 ──→ 1.4        (PrPreviewItemResponse + PrioritySummaryCard)
1.4 ──→ 2.1        (model → service)
2.1 ──→ 2.2        (service → card rendering)
1.1 ──→ 3.1        (model → page rebuild)
3.1 ──→ 3.2, 3.3, 3.4, 3.5
3.x ──→ 4.x        (all UI → CSS)
3.x ──→ 5.1, 5.3   (page → page tests + E2E)
1.4 ──→ 5.2        (model → card tests)
```
