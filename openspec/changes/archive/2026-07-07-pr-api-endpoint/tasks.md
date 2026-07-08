# Tasks: PR API Endpoint

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~620 (570 additions + 50 deletions) |
| 800-line budget risk | Medium |
| Chained PRs recommended | No |
| Suggested split | single PR |
| Delivery strategy | auto-forecast |
| Chain strategy | pending |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
800-line budget risk: Medium

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Application DTO + Mapper + unit tests | PR 1 (single) | Self-contained; no external deps |
| 2 | Api endpoint + DI + integration tests | included in PR 1 | Depends on Phase 1 |
| 3 | UI client + migration + UI tests | included in PR 1 | Depends on Phase 2 |

## Phase 1: Application Layer (Foundation)

- [x] 1.1 **RED** — Create `tests/Aura.UnitTests/Application/PullRequestMapperTests.cs` with failing tests: real-key extraction (`pr.status`, `pr.author`, `pr.repo`, `pr.reviewerCount`, `pr.commentCount`, `pr.fileCount`, `pr.isDraft`, `pr.sourceLink`, `pr.updatedAt`), Id parsing (`"pr-142"` → `142`), missing keys → safe defaults, invalid numeric → 0
- [x] 1.2 **GREEN** — Create `src/Aura.Application/Models/PullRequestDto.cs` (sealed record, 22 fields per design) and `src/Aura.Application/Mapping/PullRequestMapper.cs` (static `ToDto(WorkItem)` reading real `pr.*` keys, `int.Parse` on ExternalId suffix, safe defaults). Verify mapper tests pass
- [x] 1.3 **REFACTOR** — Review mapper for null-safety and consistency; ensure no external SDK references in Application

## Phase 2: Api Layer (Endpoint + Wiring)

- [x] 2.1 **RED** — Create `tests/Aura.IntegrationTests/PullRequests/PullRequestsEndpointTests.cs` with `WebApplicationFactory`: test 401 unauthenticated, owner filter (3+1 of 6), PriorityScore DESC ordering, 500 on reader failure
- [x] 2.2 **GREEN** — Create `src/Aura.Api/Endpoints/PullRequestsEndpoints.cs`: `GET /api/pull-requests`, auth required, optional `ownerUserId` query, Activity `pullrequests.read`, structured logging, `PullRequestMapper.ToDto`, ordering by PriorityScore DESC then CapturedAtUtc DESC. Pattern: `WorkItemsEndpoints`
- [x] 2.3 Wire `app.MapPullRequestsEndpoints()` in `src/Aura.Api/Program.cs`. Verify integration tests pass

## Phase 3: UI Layer (Client + Migration)

- [x] 3.1 Create `src/Aura.UI/Services/IPullRequestsApiClient.cs` (`Task<IReadOnlyList<PullRequestResponse>> GetPendingPullRequestsAsync(CancellationToken)`) and `src/Aura.UI/Services/PullRequestsApiClient.cs` (typed HttpClient, `JsonSerializerDefaults.Web`, `EnsureSuccessStatusCode`). Pattern: `WorkItemsApiClient`
- [x] 3.2 Register `IPullRequestsApiClient` / `PullRequestsApiClient` as Scoped in `src/Aura.UI/Program.cs` with named HttpClient pointing to Api base address + DevAccessTokenHandler. Keep `IAzureDevOpsPrClient` registration
- [x] 3.3 Modify `src/Aura.UI/Pages/PullRequests.razor`: inject `IPullRequestsApiClient` instead of `IAzureDevOpsPrClient`, call `GetPendingPullRequestsAsync`, remove client-side ordering. Preserve all testids (`pr-loading`, `pr-empty`, `pr-error`, `pr-row`, `pr-open-link`)
- [x] 3.4 Modify `src/Aura.UI/Services/PrioritySummaryService.cs`: replace `IAzureDevOpsPrClient` → `IPullRequestsApiClient` in constructor and `GetCardsAsync`
- [x] 3.5 Update `tests/Aura.UnitTests/Pages/PullRequestsPageTests.cs`: stub `IPullRequestsApiClient` instead of `IAzureDevOpsPrClient`. Update `tests/Aura.UnitTests/Dashboard/PrioritySummaryCardsRenderingTests.cs` if needed. Verify `dotnet test Aura.sln` passes

## Phase 4: Architecture Verification

- [x] 4.1 Add NetArchTest in `tests/Aura.ArchitectureTests/`: verify `PullRequestMapper` (Application) does not reference Infrastructure; verify `PullRequestsEndpoints` (Api) does not reference UI. Verify `dotnet test Aura.sln` green

## Phase 5: Cleanup

- [x] 5.1 Run `dotnet build Aura.sln` and `dotnet test Aura.sln --collect:"XPlat Code Coverage"` — confirm ≥80% coverage on new code, zero warnings
