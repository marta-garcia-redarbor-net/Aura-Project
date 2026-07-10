# PR Connector UI Specification

## Purpose

Defines the PR connector UI layer: extended data model, dashboard card with PR-specific rendering, and detail page with Stitch-aligned columns, filter bar, and pagination. Data sources include the mock `IAzureDevOpsPrClient` (v1, preserved for backward compat) and the real `IPullRequestsApiClient` (v2, consuming `GET /api/pull-requests` from persisted WorkItems).

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

Unit tests MUST update constructor calls and column assertions. E2E must verify new testids. Cards tests must work with updated `PrioritySummaryCard`. Tests for `PullRequests.razor` and `PrioritySummaryService` MUST stub `IPullRequestsApiClient` instead of `IAzureDevOpsPrClient`. The existing `StubPrClient` and `ThrowingPrClient` in E2E tests MUST be preserved until v3 mock removal.
(Previously: Tests used `IAzureDevOpsPrClient` stubs and mock data from `AzureDevOpsPrClient`.)

#### Scenario: Existing tests pass

- GIVEN extended `PullRequestResponse`
- WHEN tests construct PRs
- THEN they compile and pass

#### Scenario: E2E verifies new testids

- GIVEN deployed app
- WHEN `GET /pull-requests` returns HTML
- THEN `pr-filter-bar`, `pr-pagination`, `pr-ci-status`, `pr-review-status` exist

#### Scenario: Unit tests use new API client stub

- GIVEN `PullRequestsPageTests` constructs the page
- WHEN the test provides a stub `IPullRequestsApiClient`
- THEN the page renders correctly with stubbed data

### Requirement: IPullRequestsApiClient — New HTTP Client Port

The system MUST introduce `IPullRequestsApiClient` in `Aura.UI.Services` with method `Task<IReadOnlyList<PullRequestResponse>> GetPendingPullRequestsAsync(CancellationToken ct)`. The implementation `PullRequestsApiClient` MUST call `GET /api/pull-requests` on the Aura Api base address and deserialize the JSON response. The pattern MUST follow `WorkItemsApiClient` (HttpClient injection, JsonSerializerDefaults.Web, EnsureSuccessStatusCode).

#### Scenario: Client returns deserialized PRs

- GIVEN the API returns 4 PRs as JSON
- WHEN `GetPendingPullRequestsAsync` is called
- THEN the client SHALL return a list of 4 `PullRequestResponse` instances

#### Scenario: API error propagated

- GIVEN the API returns 500
- WHEN `GetPendingPullRequestsAsync` is called
- THEN the client SHALL throw `HttpRequestException`

### Requirement: DI Registration for PullRequestsApiClient

`PullRequestsApiClient` MUST be registered as Scoped in `Aura.UI/Program.cs` with a named or typed `HttpClient` pointing to the Api base address. The existing `IAzureDevOpsPrClient` registration MUST remain until v3 migration.

#### Scenario: New client resolvable from DI

- GIVEN the application starts
- WHEN `IPullRequestsApiClient` is resolved from the service provider
- THEN a `PullRequestsApiClient` instance SHALL be returned

### Requirement: PullRequests.razor — Migrate to IPullRequestsApiClient

`PullRequests.razor` MUST inject `IPullRequestsApiClient` instead of `IAzureDevOpsPrClient`. The page MUST call `GetPendingPullRequestsAsync` on the new client. All existing testids (`pr-loading`, `pr-empty`, `pr-error`, `pr-row`, `pr-open-link`, `pr-ci-status`, `pr-review-status`, `pr-pagination`, `pr-filter-bar`) MUST be preserved. Client-side ordering by PriorityScore from the API response SHOULD be removed (API already orders).

#### Scenario: Page renders with real API data

- GIVEN `IPullRequestsApiClient` returns 5 PRs from the store
- WHEN `PullRequests.razor` loads
- THEN the page SHALL render 5 rows with testid `pr-row` and correct CI/review data

#### Scenario: Existing testids preserved after migration

- GIVEN the page is in Populated state
- WHEN the DOM is inspected
- THEN all v1 testids (`pr-loading`, `pr-empty`, `pr-error`, `pr-row`, `pr-open-link`) SHALL be present

### Requirement: PrioritySummaryService — Migrate to IPullRequestsApiClient

`PrioritySummaryService` MUST replace its `IAzureDevOpsPrClient` dependency with `IPullRequestsApiClient`. The `BuildCards` PR section MUST use the new client's response. The `PrPreviewItemResponse` mapping MUST remain unchanged.

#### Scenario: Dashboard cards use API data

- GIVEN `IPullRequestsApiClient` returns 3 PRs
- WHEN `GetCardsAsync` builds cards
- THEN the PR card SHALL contain 3 `PrPreviewItemResponse` items from real data

#### Scenario: Service compiles with new dependency

- GIVEN the constructor takes `IPullRequestsApiClient` instead of `IAzureDevOpsPrClient`
- WHEN the solution builds
- THEN it SHALL compile without errors

---

### Requirement: PrioritySummaryCard — Empty State Model Properties

`PrioritySummaryCard` MUST expose five new properties: `EmptyIcon` (string), `EmptyTitle` (string), `EmptySubtitle` (string), `EmptyFooterLabel` (string), `EmptyFooterUrl` (string). `PrioritySummaryService.BuildCards()` MUST populate these per card. Cards with items still populate the properties but they are not rendered.

#### Scenario: Model carries empty-state properties

- GIVEN a `PrioritySummaryCard` is constructed
- WHEN the record is created
- THEN `EmptyIcon`, `EmptyTitle`, `EmptySubtitle`, `EmptyFooterLabel`, and `EmptyFooterUrl` are set to non-null values

#### Scenario: BuildCards populates per-card values

- GIVEN `PrioritySummaryService.BuildCards()` executes
- WHEN cards are built for all four sources
- THEN each card has distinct empty-state values matching the proposal table

### Requirement: PR Card Empty State Rendering

When the Pull Requests card has zero items, it MUST render its card-specific positive empty state. The card MUST display icon `verified`, title "Queue Empty", subtitle "No pending reviews. Your workspace is clear." A footer link "View All Repositories" pointing to the card's `ViewAllUrl` MUST be visible.

#### Scenario: PR card empty state

- GIVEN the PR card has 0 items and `IsPrCard = true`
- WHEN `PrioritySummaryCards.razor` renders
- THEN the card displays icon `verified`, title "Queue Empty", subtitle "No pending reviews. Your workspace is clear."
- AND a footer link "View All Repositories" is visible
