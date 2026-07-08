# Delta for PR Connector UI

## ADDED Requirements

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

## MODIFIED Requirements

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
