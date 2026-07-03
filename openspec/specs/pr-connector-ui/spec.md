# PR Connector UI Specification

## Purpose

Defines the PR connector UI layer: extended data model, dashboard card with PR-specific rendering, and detail page with Stitch-aligned columns, filter bar, and pagination. All data is mock-based in v1.

## Requirements

### Requirement: PullRequestResponse — Extended Data Model

Add 6 fields: `BranchName`, `SourceBranchName`, `BuildStatus` (one of: "passing", "running", "failed", "pending", "unknown"), `ReviewApprovals`, `ReviewRequired`, `ReviewChangesRequested`. `BuildStatus` MUST default to `"pending"`.

#### Scenario: New fields populated from mock data

- GIVEN `AzureDevOpsPrClient` returns PRs
- WHEN constructing each response
- THEN all 6 new fields SHALL contain non-null values

#### Scenario: BuildStatus defaults safely

- GIVEN a response without explicit `BuildStatus`
- THEN `BuildStatus` SHALL be `"pending"`

### Requirement: AzureDevOpsPrClient — Mock Data Updated

All 6 existing PR instances MUST receive values for the 6 new fields. Existing fields unchanged.

#### Scenario: Realistic mock values

- GIVEN the mock PR list
- WHEN iterating each PR
- THEN `ReviewApprovals` ≤ `ReviewRequired`, `ReviewChangesRequested` ≥ 0

### Requirement: PrPreviewItemResponse — New Preview Record

New record replaces `InboxItemPreviewResponse` for PR card data. Fields: `Title`, `PrDisplayName` (`"#{Id} {Title}"`), `BranchName`, `BuildStatus`, `ReviewApprovals`, `ReviewRequired`, `Author`, `UpdatedAt`, `RelativeTimestamp`, `SourceLink`, `IsDraft`, `Priority`.

#### Scenario: PR card uses PrPreviewItemResponse

- GIVEN `PrioritySummaryService.BuildCards` creates the PR card
- WHEN built
- THEN its items SHALL be `PrPreviewItemResponse` instances

### Requirement: PrioritySummaryCard — IsPrCard and PrItems

Add `bool IsPrCard` and `List<PrPreviewItemResponse>? PrItems`. `TotalCount` SHALL use `PrItems.Count` when `IsPrCard` is true. PR card SHALL set `IsPrCard = true`.

#### Scenario: PR card count uses PrItems

- GIVEN a PR card with 6 PrItems and `IsPrCard = true`
- WHEN `TotalCount` is read
- THEN it returns 6

### Requirement: PrioritySummaryCards.razor — PR Rendering

Add `else if (card.IsPrCard)` before generic `else`. Mini-table: PR NAME, BRANCH, CI STATUS, REVIEWS (X/Y + changes), LAST ACTIVITY, ACTION. Footer: "View All Repositories". No tabs for v1. Keep `else` fallback.

#### Scenario: PR card renders mini-table

- GIVEN a card with `IsPrCard = true` and 3 items
- WHEN rendered
- THEN output contains the 6-column mini-table with open_in_new per row

### Requirement: PullRequests.razor — Stitch Detail Page

Full rebuild: header with PR + repo counts, filter bar (visual-only), table columns (Priority, PR Name, CI Status, Reviews, Last Activity), pagination (visual-only), right panel placeholder. Preserve testids: `pr-loading`, `pr-empty`, `pr-error`, `pr-row`, `pr-open-link`. Add: `pr-filter-bar`, `pr-pagination`, `pr-ci-status`, `pr-review-status`.

#### Scenario: Stitch columns render

- GIVEN 5 populated PRs
- WHEN page renders in `Populated` state
- THEN all spec columns and legacy testids are present

#### Scenario: Filter bar is visual-only

- GIVEN populated state
- WHEN filter controls are clicked
- THEN no API call is triggered

### Requirement: stitch-dashboard.css — New Rules

Add CI badges (`.ci-status--passing` green, `--running` amber, `--failed` red, `--pending` slate), PR mini-table (borderless, compact), filter bar (flex), pagination, reviewer initials, PR section header, right panel placeholder. MUST NOT remove existing styles.

#### Scenario: CI badges use correct colors

- GIVEN a PR with `BuildStatus = "running"`
- WHEN rendered
- THEN badge has `.ci-status--running` with amber color

### Requirement: Tests — Updated

Unit tests MUST update constructor calls and column assertions. E2E must verify new testids. Cards tests must work with updated `PrioritySummaryCard`.

#### Scenario: Existing tests pass

- GIVEN extended `PullRequestResponse`
- WHEN tests construct PRs
- THEN they compile and pass

#### Scenario: E2E verifies new testids

- GIVEN deployed app
- WHEN `GET /pull-requests` returns HTML
- THEN `pr-filter-bar`, `pr-pagination`, `pr-ci-status`, `pr-review-status` exist
