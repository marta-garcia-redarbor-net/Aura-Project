# PR Connector UI — Stitch Alignment Spec

Canonical full spec at `openspec/specs/pr-connector-ui/spec.md`.

## ADDED Requirements

### Requirement: PullRequestResponse — Extended Data Model

The record MUST add 6 fields: `BranchName`, `SourceBranchName`, `BuildStatus`, `ReviewApprovals`, `ReviewRequired`, `ReviewChangesRequested`. `BuildStatus` SHALL be one of "passing", "running", "failed", "pending", "unknown" and MUST default to `"pending"`.

#### Scenario: New fields populated from mock data

- GIVEN `AzureDevOpsPrClient` returns PRs
- WHEN constructing each `PullRequestResponse`
- THEN all 6 new fields SHALL contain non-null, non-default values

#### Scenario: BuildStatus defaults safely

- GIVEN a `PullRequestResponse` without explicit `BuildStatus`
- THEN `BuildStatus` SHALL be `"pending"`

### Requirement: PrPreviewItemResponse — New Preview Record

New record with fields: `Title`, `PrDisplayName` (`"#{Id} {Title}"`), `BranchName`, `BuildStatus`, `ReviewApprovals`, `ReviewRequired`, `Author`, `UpdatedAt`, `RelativeTimestamp`, `SourceLink`, `IsDraft`, `Priority`. Replaces `InboxItemPreviewResponse` for PR card data.

#### Scenario: PR card uses PrPreviewItemResponse

- GIVEN `PrioritySummaryService.BuildCards` creates the PR card
- WHEN the PR card is built
- THEN items SHALL be `PrPreviewItemResponse` instances

### Requirement: AzureDevOpsPrClient — Mock Data Updated

All 6 existing PR instances MUST receive new field values. Existing fields unchanged.

#### Scenario: Realistic branch names and review counts

- GIVEN the mock PR list
- WHEN iterating each PR
- THEN `ReviewApprovals` ≤ `ReviewRequired`, `ReviewChangesRequested` ≥ 0

## MODIFIED Requirements

### Requirement: PrioritySummaryCard — IsPrCard and PrItems

Add `bool IsPrCard` and `List<PrPreviewItemResponse>? PrItems`. `TotalCount` SHALL use `PrItems.Count` when `IsPrCard` is true. PR card SHALL set `IsPrCard = true`.
(Previously: only PreviewItems and CalendarItems, no PR-specific flag)

#### Scenario: PR card TotalCount uses PrItems

- GIVEN a PR card with 6 PrItems and `IsPrCard = true`
- WHEN `TotalCount` is read
- THEN it SHALL return 6

### Requirement: PrioritySummaryCards.razor — PR Card Rendering

Add `else if (card.IsPrCard)` branch before generic `else`. Mini-table: PR NAME, BRANCH, CI STATUS, REVIEWS (X/Y + changes), LAST ACTIVITY, ACTION (open_in_new). No tabs for v1. Footer: "View All Repositories". Keep `else` fallback.
(Previously: all non-calendar cards used generic inbox item template)

#### Scenario: PR card renders mini-table

- GIVEN a card with `IsPrCard = true` and 3 items
- WHEN rendered
- THEN output has the 6-column mini-table with open_in_new icons

### Requirement: PullRequests.razor — Stitch-Aligned Detail Page

Full rebuild: header with PR + repo counts, filter bar (visual-only), table columns (Priority, PR Name, CI Status, Reviews, Last Activity), pagination (visual-only), right panel placeholder. Preserve testids: `pr-loading`, `pr-empty`, `pr-error`, `pr-row`, `pr-status`, `pr-open-link`. Add: `pr-filter-bar`, `pr-pagination`, `pr-ci-status`, `pr-review-status`.
(Previously: columns were Title, Repository, Author, Updated, Status, Reviews, Comments)

#### Scenario: Stitch columns render

- GIVEN 5 populated PRs
- WHEN page renders in `Populated` state
- THEN all spec columns and legacy testids are present

#### Scenario: Filter bar is visual-only

- GIVEN populated state
- WHEN filter controls are clicked
- THEN no API call is triggered

### Requirement: stitch-dashboard.css — New CSS Rules

Add `.ci-status--passing` (green), `--running` (amber), `--failed` (red), `--pending` (slate). Add PR mini-table (borderless, compact), filter bar (flex), pagination row, reviewer initials, PR section header, right panel placeholder. MUST NOT remove existing dashboard-card styles.
(Previously: no CI badges, no PR mini-table, no filter/pagination styles)

#### Scenario: CI badges render with correct colors

- GIVEN a PR with `BuildStatus = "running"`
- WHEN rendered
- THEN badge has `.ci-status--running` with amber color

### Requirement: Tests — Updated for New Model

Unit tests: add new fields to `PullRequestResponse` constructors, adapt column assertions. E2E: verify new testids exist. Cards rendering tests: work with updated `PrioritySummaryCard`.
(Previously: tests used the 11-field constructor; no CI or review-status assertions)

#### Scenario: Existing tests pass

- GIVEN extended `PullRequestResponse`
- WHEN tests construct PRs
- THEN they compile and pass

#### Scenario: E2E verifies new testids

- GIVEN deployed app
- WHEN `GET /pull-requests` returns HTML
- THEN `pr-filter-bar`, `pr-pagination`, `pr-ci-status`, `pr-review-status` exist
