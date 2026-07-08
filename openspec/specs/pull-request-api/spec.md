# Pull Request API Specification

## Purpose

Defines the `GET /api/pull-requests` endpoint that serves PR-specific data from persisted WorkItems (SourceType=PrReview). Reuses `IWorkItemReader`, maps to a dedicated `PullRequestDto`, and enforces ownership visibility.

## Requirements

### Requirement: Endpoint Route and Auth

The system MUST expose `GET /api/pull-requests` under authorization. The endpoint SHALL accept an optional `ownerUserId` query parameter.

#### Scenario: Authenticated request succeeds

- GIVEN an authenticated user calls `GET /api/pull-requests`
- WHEN the request is processed
- THEN the endpoint SHALL return 200 OK with `List<PullRequestDto>`

#### Scenario: Unauthenticated request rejected

- GIVEN an unauthenticated caller
- WHEN `GET /api/pull-requests` is invoked
- THEN the endpoint SHALL return 401 Unauthorized

### Requirement: Data Source and Filtering

The endpoint MUST retrieve data via `IWorkItemReader.ReadBySourceAsync(PrReview, null, ct)`. When `ownerUserId` is provided, the result MUST exclude items whose `OwnerUserId` is non-null and does not match. Items with `OwnerUserId = null` are visible to all callers.

#### Scenario: All PRs returned when no owner filter

- GIVEN 6 PR WorkItems in store with mixed OwnerUserId values
- WHEN `GET /api/pull-requests` is called without `ownerUserId`
- THEN all 6 items SHALL be returned

#### Scenario: Owner filter applied

- GIVEN 6 PR WorkItems: 3 with `OwnerUserId="user-A"`, 2 with `OwnerUserId="user-B"`, 1 with `OwnerUserId=null`
- WHEN `GET /api/pull-requests?ownerUserId=user-A` is called
- THEN 4 items SHALL be returned (3 owned by user-A + 1 with null owner)

### Requirement: PullRequestDto Mapping

The system MUST map each WorkItem to a `PullRequestDto` extracting PR-specific fields from `Metadata` using `pr.*` keys. The DTO MUST include: `Id` (from ExternalId parsed as int), `Title`, `RepoName` (from `Metadata["pr.repo"]`), `Author` (from `Metadata["pr.author"]`), `Status` (from `Metadata["pr.status"]` — "passing", "running", "failed", "pending"), `ReviewerCount` (from `Metadata["pr.reviewerCount"]`), `CommentCount` (from `Metadata["pr.commentCount"]`), `FileCount` (from `Metadata["pr.fileCount"]`), `SourceLink`, `IsDraft`, `Priority`, `PriorityScore`, `CreatedAt`, `UpdatedAt`.

Fields without real metadata keys (`BranchName`, `SourceBranchName`, `BuildStatus` as separate field, `ReviewRequired`, `ReviewChangesRequested`) SHALL use documented safe defaults (empty string, "pending", 0).

NOTE: The endpoint uses the metadata keys that the ingestion pipeline (`PrReviewWorkItemMapper`) actually writes. See `design.md` for the authoritative key mapping.

#### Scenario: Metadata fields extracted correctly

- GIVEN a WorkItem with `Metadata["pr.status"]="passing"`, `Metadata["pr.reviewerCount"]="2"`, `Metadata["pr.commentCount"]="5"`
- WHEN mapped to PullRequestDto
- THEN `Status` SHALL be "passing", `ReviewerCount` SHALL be 2, `CommentCount` SHALL be 5

#### Scenario: Missing metadata keys default safely

- GIVEN a WorkItem with no `pr.status` key in Metadata
- WHEN mapped to PullRequestDto
- THEN `Status` SHALL default to "pending"

#### Scenario: Numeric metadata parsed safely

- GIVEN a WorkItem with `Metadata["pr.reviewerCount"]="invalid"`
- WHEN mapped to PullRequestDto
- THEN `ReviewerCount` SHALL default to 0

### Requirement: Ordering

The response list MUST be ordered by `PriorityScore` DESC. Items with equal PriorityScore SHOULD be ordered by `CapturedAtUtc` DESC.

#### Scenario: Priority ordering

- GIVEN PRs with PriorityScore 90, 50, 90
- WHEN the endpoint returns results
- THEN the order SHALL be: 90, 90, 50

### Requirement: Observability

The endpoint MUST emit an Activity span named `pullrequests.read` with tags for count and owner filter. The endpoint MUST log success (event with count) and failure (with exception).

#### Scenario: Trace span emitted

- GIVEN a successful request returning 4 PRs
- WHEN the response is sent
- THEN an Activity `pullrequests.read` SHALL exist with tag `pullrequests.count=4`

#### Scenario: Error logged on failure

- GIVEN the underlying store throws
- WHEN the endpoint catches the exception
- THEN a structured error log SHALL be emitted and a 500 Problem response returned

### Requirement: Performance

The endpoint SHOULD respond within 500ms for up to 200 PR WorkItems. The endpoint MUST NOT perform N+1 queries — all data SHALL come from a single store call.

#### Scenario: Response time within budget

- GIVEN 100 PR WorkItems in store
- WHEN `GET /api/pull-requests` is called
- THEN the response SHALL complete within 500ms
