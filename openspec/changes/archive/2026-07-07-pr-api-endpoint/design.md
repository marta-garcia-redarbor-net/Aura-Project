# Design: PR API Endpoint

## Technical Approach

Dedicated `GET /api/pull-requests` endpoint reusing `IWorkItemReader.ReadBySourceAsync(PrReview)`, mapping `WorkItem` → `PullRequestDto` via a static mapper aligned with **real** `pr.*` metadata keys from `PrReviewWorkItemMapper`. UI migrates from mock `IAzureDevOpsPrClient` to `IPullRequestsApiClient`. This is the proposal's Option C.

## Architecture Decisions

### Decision: Metadata key alignment with real ingestion data

| Option | Tradeoff | Decision |
|--------|----------|----------|
| Use keys from spec (`pr.buildStatus`, `pr.reviewApprovals`) | Spec scenario passes but real data returns defaults for everything | **Rejected** |
| Use REAL keys from `PrReviewWorkItemMapper` (`pr.status`, `pr.reviewerCount`, `pr.author`, `pr.repo`) | Fields without real data get safe defaults; mapper reflects actual persisted data | **Chosen** |

**Real metadata keys** (from `PrReviewWorkItemMapper.BuildMetadata`):
- `pr.pullRequestId`, `pr.status`, `pr.repo`, `pr.author`, `pr.sourceLink`, `pr.updatedAt`
- `pr.reviewers`, `pr.reviewerCount`, `pr.commentCount`, `pr.fileCount`, `pr.isDraft`
- `pr.priority.raw`, `pr.priority.resolution`

**Fields with no real data** (safe defaults): `BranchName=""`, `SourceBranchName=""`, `BuildStatus="pending"`, `ReviewApprovals=0`, `ReviewRequired=0`, `ReviewChangesRequested=0`.

### Decision: Id type — parse numeric suffix from ExternalId

| Option | Tradeoff | Decision |
|--------|----------|----------|
| A: Change `PullRequestResponse.Id` to `string` | Breaks UI backward compat, cascading changes | **Rejected** |
| B: Parse "pr-142" → 142 | Preserves existing `int Id` contract, zero UI breakage | **Chosen** |
| C: Replace `PullRequestResponse` with `PullRequestDto` in UI | Larger migration surface, unnecessary coupling | **Rejected** |

**Rationale**: `ExternalId` format `"pr-{id}"` is stable (set by `PrReviewWorkItemMapper`). Parsing is trivial and isolated to the mapper.

### Decision: DTO naming aligned with spec AND PullRequestResponse

| Old design (wrong) | Corrected (spec + PullRequestResponse) | Source |
|--------------------|----------------------------------------|--------|
| `RepositoryName` | `RepoName` | Metadata["pr.repo"] |
| `AuthorName` | `Author` | Metadata["pr.author"] |
| `PriorityLabel` | `Priority` | WorkItem.Priority.ToString() |
| `pr.buildStatus` | `pr.status` → `Status` | Real key |
| `pr.reviewApprovals` | `pr.reviewerCount` → `ReviewerCount` | Real key |

**Rationale**: `PullRequestResponse` already uses `RepoName`, `Author`, `Priority`, `Status`, `ReviewerCount`. The DTO mirrors these names so the UI deserializes JSON directly with zero mapping.

### Decision: PullRequestResponse — keep as-is, DTO is a superset

`PullRequestResponse` already has all fields the UI needs. `PullRequestDto` (Application) includes the same fields plus `PriorityScore` (for ordering). `System.Text.Json` silently ignores extra JSON properties during deserialization, so the UI model doesn't need changes.

### Decision: OwnerUserId filtering at endpoint layer

In-memory filter after `ReadBySourceAsync`: include items where `OwnerUserId == null || OwnerUserId == ownerUserId`. Preserves backward compat with seed data. No store changes.

### Decision: Ordering at API layer

`PriorityScore DESC`, then `CapturedAtUtc DESC`. UI removes client-side ordering.

## Data Flow

```
PullRequests.razor / PrioritySummaryService
    │ injects IPullRequestsApiClient
    ▼
PullRequestsApiClient (typed HttpClient)
    │ GET /api/pull-requests?ownerUserId=...
    ▼
PullRequestsEndpoints (Minimal API)
    │ Activity "pullrequests.read" + OwnerUserId filter
    │ PullRequestMapper.ToDto(WorkItem)
    ▼
IWorkItemReader.ReadBySourceAsync(PrReview)
    ▼
EfWorkItemStore → WorkItems table (SourceType=PrReview)
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Application/Models/PullRequestDto.cs` | Create | Sealed record: `int Id`, `string Title`, `string RepoName`, `string Author`, `DateTimeOffset CreatedAt/UpdatedAt`, `string Status`, `int ReviewerCount/CommentCount/FileCount`, `string SourceLink`, `bool IsDraft`, `string Priority`, `string BranchName/SourceBranchName/BuildStatus`, `int ReviewApprovals/ReviewRequired/ReviewChangesRequested`, `int? PriorityScore` |
| `src/Aura.Application/Mapping/PullRequestMapper.cs` | Create | Static mapper: `WorkItem` → `PullRequestDto`. Reads REAL keys (`pr.status`, `pr.author`, `pr.repo`, `pr.reviewerCount`, `pr.commentCount`, `pr.fileCount`, `pr.isDraft`, `pr.sourceLink`, `pr.updatedAt`). Parses `int` from ExternalId suffix. Safe defaults for missing keys |
| `src/Aura.Api/Endpoints/PullRequestsEndpoints.cs` | Create | `GET /api/pull-requests`. Auth required. Optional `ownerUserId` query. Activity + structured logging. Pattern: `WorkItemsEndpoints` |
| `src/Aura.UI/Services/IPullRequestsApiClient.cs` | Create | `Task<IReadOnlyList<PullRequestResponse>> GetPendingPullRequestsAsync(CancellationToken)` |
| `src/Aura.UI/Services/PullRequestsApiClient.cs` | Create | HttpClient + `JsonSerializerDefaults.Web` + `EnsureSuccessStatusCode`. Pattern: `WorkItemsApiClient` |
| `src/Aura.UI/Pages/PullRequests.razor` | Modify | Inject `IPullRequestsApiClient` instead of `IAzureDevOpsPrClient`. Remove client-side ordering. Preserve testids |
| `src/Aura.UI/Services/PrioritySummaryService.cs` | Modify | Replace `IAzureDevOpsPrClient` → `IPullRequestsApiClient` in constructor |
| `src/Aura.Api/Program.cs` | Modify | `app.MapPullRequestsEndpoints()` |
| `src/Aura.UI/Program.cs` | Modify | Register `PullRequestsApiClient` + DevAccessTokenHandler. Keep `IAzureDevOpsPrClient` until v3 |
| `tests/Aura.UnitTests/Application/PullRequestMapperTests.cs` | Create | Real-key extraction, safe defaults, Id parsing, missing keys |
| `tests/Aura.UnitTests/UI/PullRequestsPageTests.cs` | Modify | Stub `IPullRequestsApiClient` |
| `tests/Aura.UnitTests/UI/PrioritySummaryServiceTests.cs` | Modify | Stub `IPullRequestsApiClient` |
| `tests/Aura.IntegrationTests/PullRequests/PullRequestsEndpointTests.cs` | Create | Auth (401), owner filter, ordering, error (500) |

## Interfaces / Contracts

### PullRequestDto

```csharp
namespace Aura.Application.Models;

public sealed record PullRequestDto(
    int Id,                     // parsed from ExternalId "pr-142" → 142
    string Title,               // WorkItem.Title
    string RepoName,            // Metadata["pr.repo"]
    string Author,              // Metadata["pr.author"]
    DateTimeOffset CreatedAt,   // WorkItem.CapturedAtUtc
    DateTimeOffset UpdatedAt,   // Metadata["pr.updatedAt"] or CapturedAtUtc
    string Status,              // Metadata["pr.status"], default "unknown"
    int ReviewerCount,          // Metadata["pr.reviewerCount"], default 0
    int CommentCount,           // Metadata["pr.commentCount"], default 0
    int FileCount,              // Metadata["pr.fileCount"], default 0
    string SourceLink,          // Metadata["pr.sourceLink"], default ""
    bool IsDraft,               // Metadata["pr.isDraft"], default false
    string Priority,            // WorkItem.Priority.ToString()
    string BranchName,          // default "" (no real data)
    string SourceBranchName,    // default "" (no real data)
    string BuildStatus,         // default "pending" (no real data)
    int ReviewApprovals,        // default 0 (no real data)
    int ReviewRequired,         // default 0 (no real data)
    int ReviewChangesRequested, // default 0 (no real data)
    int? PriorityScore);        // WorkItem.PriorityScore
```

### PullRequestMapper — key extraction

```csharp
// Real keys read from Metadata:
//   pr.status     → Status       (NOT pr.buildStatus)
//   pr.author     → Author       (NOT pr.authorName)
//   pr.repo       → RepoName     (NOT WorkItem.Source)
//   pr.reviewerCount → ReviewerCount (NOT pr.reviewApprovals)
//   pr.commentCount  → CommentCount
//   pr.fileCount     → FileCount
//   pr.isDraft       → IsDraft
//   pr.sourceLink    → SourceLink
//   pr.updatedAt     → UpdatedAt  (DateTimeOffset.Parse, fallback CapturedAtUtc)
//
// Id: int.Parse(ExternalId.AsSpan("pr-".Length))
// Priority: WorkItem.Priority.ToString()
// BranchName, SourceBranchName, BuildStatus, ReviewApprovals,
// ReviewRequired, ReviewChangesRequested → safe defaults
```

## Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| Unit | `PullRequestMapper` | Real-key extraction, Id parsing ("pr-142"→142), missing keys → defaults, invalid numeric → 0 |
| Unit | `PullRequests.razor` | Stub `IPullRequestsApiClient`, verify testids preserved |
| Unit | `PrioritySummaryService` | Stub `IPullRequestsApiClient`, verify PR card data |
| Integration | Endpoint | `WebApplicationFactory`: 401, owner filter, ordering, 500 on reader failure |

## Migration / Rollout

No data migration. Seed data already contains PR WorkItems. `IAzureDevOpsPrClient` remains until v3 E2E migration. Phased: deploy API first, verify endpoint, then deploy UI migration.

## Open Questions

None — all previous issues resolved.
