# Tasks: W2-H6 Dashboard Inbox-by-Source and Morning Summary Preview

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 520-760 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 → PR 2 → PR 3 |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Application + API preview contract and endpoint | PR 1 | Includes integration tests + telemetry. If feature-branch-chain is chosen: base = feature/tracker branch. |
| 2 | UI client + dashboard panels with all states | PR 2 | Includes UI smoke tests. If feature-branch-chain: base = PR 1 branch. |
| 3 | Architecture boundary checks + full verification pass | PR 3 | Includes architecture tests and suite run. If feature-branch-chain: base = PR 2 branch. |

## Phase 1: Foundation / Contracts

- [x] 1.1 Create `src/Aura.Application/Models/DashboardPreviewDto.cs` with `DashboardPreviewDto`, `InboxSourceGroupDto`, `InboxItemPreviewDto`, and `SummaryPreviewEntryDto` (dashboard-specific DTOs only).
- [x] 1.2 Create `src/Aura.Application/Ports/IDashboardPreviewReader.cs` with `Task<DashboardPreviewDto> GetAsync(CancellationToken)`.
- [x] 1.3 Update `src/Aura.Application/DependencyInjection.cs` to register `IDashboardPreviewReader` as scoped and keep `WorkItem` internal to Domain/Application.

## Phase 2: Core API Slice (TDD)

- [x] 2.1 RED: Create `tests/Aura.IntegrationTests/Dashboard/DashboardPreviewEndpointTests.cs` for `GET /api/dashboard/preview` scenarios: 200 populated shape, 200 empty shape, 401 unauthenticated, and 500 reader failure.
- [x] 2.2 GREEN: Create `src/Aura.Application/Services/DashboardPreviewReader.cs` using `IWorkItemReader`, `IMorningSummaryRankingPolicy`, and `ICurrentUserService` to project title/source/relative timestamp/score/suggested action and summary rank entries.
- [x] 2.3 GREEN: Modify `src/Aura.Api/Endpoints/DashboardEndpoints.cs` to map `GET /api/dashboard/preview`, call `IDashboardPreviewReader`, and emit dashboard preview activity/log tags in the same slice.
- [x] 2.4 REFACTOR: Align endpoint/test helpers and DTO serialization so no `WorkItem` or domain aggregate appears in response types.

## Phase 3: UI Integration Slice (TDD)

- [x] 3.1 RED: Modify `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` (WebApplicationFactory-based) with stub `IDashboardPreviewApiClient` and assertions for loading, empty, error, and populated markers.
- [x] 3.2 GREEN: Create `src/Aura.UI/Models/DashboardPreviewResponse.cs`, `src/Aura.UI/Services/IDashboardPreviewApiClient.cs`, and `src/Aura.UI/Services/DashboardPreviewApiClient.cs` consuming `GET /api/dashboard/preview`.
- [x] 3.3 GREEN: Modify `src/Aura.UI/Program.cs` to register `IDashboardPreviewApiClient`/`DashboardPreviewApiClient` with `DevAccessTokenHandler` in development.
- [x] 3.4 GREEN: Create `src/Aura.UI/Components/Dashboard/InboxPreviewPanel.razor` and `src/Aura.UI/Components/Dashboard/MorningSummaryPreviewPanel.razor` with loading/empty/error/populated states and stable `data-testid` attributes.
- [x] 3.5 REFACTOR: Modify `src/Aura.UI/Pages/Index.razor` to mount both panels after existing dashboard panels while keeping components presentation-only.

## Phase 4: Verification / Boundary Enforcement

- [x] 4.1 Modify `tests/Aura.ArchitectureTests/DashboardArchitectureTests.cs` to assert `Aura.UI.Models` and dashboard endpoint types do not depend on `Aura.Domain`.
- [x] 4.2 Extend `tests/Aura.IntegrationTests/Dashboard/DashboardPreviewEndpointTests.cs` to verify deserialized payload shape contains only dashboard preview DTO fields.
- [x] 4.3 Run `dotnet test Aura.sln` and `dotnet test Aura.sln --collect:"XPlat Code Coverage"`; fix regressions before `sdd-verify`.
