# Tasks: Dashboard System Status Card

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~550–700 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 (infra + API) → PR 2 (UI + tests) |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Infrastructure ports, adapters, health checks, DI, DTO, reader, API | PR 1 | base=main; 6 files + ~4 test file mechanical updates |
| 2 | UI model, StatusGreetingCard, CSS, PriorityDashboard integration, new tests | PR 2 | depends on PR 1 (needs DTO shape); 3 files + ~6 test files |

## Phase 1: Infrastructure Foundation

- [x] 1.1 Create `src/Aura.Application/Ports/IDbReadinessProvider.cs` — `Task<ReadinessSignal> GetReadinessAsync(CancellationToken)` (same contract as IQdrantReadinessProvider)
- [x] 1.2 Create `src/Aura.Application/Ports/ILlmReadinessProvider.cs` — same contract
- [x] 1.3 Create `src/Aura.Infrastructure/HealthChecks/LlmHealthCheck.cs` — dual-constructor: prod uses `IHttpClientFactory` → `GET /api/tags` ≤3s; test uses delegate. Follow QdrantHealthCheck pattern
- [x] 1.4 Create `src/Aura.Infrastructure/Adapters/Dashboard/DbReadinessAdapter.cs` — filter `HealthCheckRegistration.Name == "database"`, map to ReadinessSignal
- [x] 1.5 Create `src/Aura.Infrastructure/Adapters/Dashboard/LlmReadinessAdapter.cs` — filter `"llm"`, same pattern
- [x] 1.6 Register services: `src/Aura.Infrastructure/Adapters/Dashboard/DependencyInjection.cs` (+`IDbReadinessProvider`/`ILlmReadinessProvider` scoped); `src/Aura.Infrastructure/DependencyInjection.cs` (+`.AddCheck<LlmHealthCheck>("llm")`)

## Phase 2: Application & API

- [x] 2.1 Extend `src/Aura.Application/Models/SystemStatusDto.cs` — add `SystemIndicatorDto Database`, `SystemIndicatorDto Llm` fields to positional record
- [x] 2.2 Extend `src/Aura.Application/Services/SystemStatusReader.cs` — inject `IDbReadinessProvider` + `ILlmReadinessProvider`; add `DeriveDatabaseIndicator()` + `DeriveLlmIndicator()`; return 5-field DTO
- [x] 2.3 Update `src/Aura.Api/Endpoints/DashboardEndpoints.cs` — add `dashboard.system_status.database` + `.llm` activity tags; add Db/Llm params to log message

## Phase 3: UI Layer

- [x] 3.1 Extend `src/Aura.UI/Models/SystemStatusResponse.cs` — add `SystemIndicatorResponse Database`, `SystemIndicatorResponse Llm` fields
- [x] 3.2 Create `src/Aura.UI/Components/Dashboard/StatusGreetingCard.razor` — greeting (morning/afternoon/evening via `DateTime.Now.Hour`), Overall computed from all 5 indicators, 5 dots with `aria-label`, ≤60px, EventBus refresh + 60s polling
- [x] 3.3 Add `.status-greeting-card` + `.status-dot--*` styles to `src/Aura.UI/wwwroot/css/stitch-dashboard.css`
- [x] 3.4 Insert `<StatusGreetingCard />` in `src/Aura.UI/Components/Dashboard/PriorityDashboard.razor` before `<PrioritySummaryCards />`

## Phase 4: Testing

- [x] 4.1 Update all `SystemStatusDto` call sites in tests (3 sites across 2 files) to add Db + Llm params
- [x] 4.2 Update all `SystemStatusResponse` call sites in tests (11 sites across 7 files) to add Db + Llm params
- [x] 4.3 Add `tests/Aura.UnitTests/Health/LlmHealthCheckTests.cs` — delegate-constructor tests: healthy, unhealthy, throws (follow QdrantHealthCheckTests pattern)
- [x] 4.4 Add `tests/Aura.UnitTests/Dashboard/DbReadinessAdapterTests.cs` + `LlmReadinessAdapterTests.cs` — GIVEN stubbed HealthCheckService → THEN correct ReadinessSignal
- [x] 4.5 Extend `tests/Aura.UnitTests/Dashboard/SystemStatusReaderTests.cs` — add Db/Llm healthy, unhealthy, degraded scenarios
- [x] 4.6 Add `tests/Aura.UnitTests/UI/StatusGreetingCardTests.cs` — greeting periods, Overall aggregation, API failure graceful fallback, aria-labels, rendering
