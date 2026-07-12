# Archive Report: Dashboard System Status Card

**Change**: `dashboard-system-status-card`
**Archived**: 2026-07-12
**Source**: `openspec/changes/dashboard-system-status-card/` → `openspec/changes/archive/2026-07-12-dashboard-system-status-card/`
**Verdict**: PASS WITH WARNINGS — archived

**Stale-checkbox reconciliation**: All 22 implementation tasks were complete per `verify-report.md` (build ✅ 0 errors, 34/34 new tests pass, 11/11 integration tests pass), but the persisted `tasks.md` had stale `[ ]` checkboxes. These were reconciled at archive time — marked to `[x]` — with proof from `apply-progress` (files created/modified) and `verify-report.md`. This is an exceptional archive-time mechanical repair permitted by the SDD archive policy.

---

## 1. Change Summary

Added a compact system status greeting card to the Priority Dashboard (`/dashboard`) that shows a time-of-day greeting and live health indicators (green/red/grey dots) for five system components: Overall, API, Database, Qdrant, and LLM.

**Why**: Users landed on `/dashboard` with no system health sense. The card reduces uncertainty without distracting from priority cards below.

**Key decisions**:
- **Overall** and **Greeting** computed client-side in Blazor (no DTO coupling to display rules)
- LLM endpoint config reuses `LlmAdvisorOptions.Endpoint` (no dedicated health-check options)
- Readiness adapters filter by `HealthCheckRegistration.Name` (established pattern)
- `LlmHealthCheck` uses dual-constructor pattern (prod `IHttpClientFactory`, test delegate)

---

## 2. Delta Spec Sync

| Domain | Action | Details |
|--------|--------|---------|
| `dashboard-system-status` | Modified | `Status API Endpoint` — expanded from 3 to 5 indicator fields (added db, llm) |
| `dashboard-system-status` | Added | `Database and LLM Readiness Indicators` — new requirement with 4 scenarios |
| `dashboard-system-status` | Added | `Overall Status Aggregation` — new requirement with 3 scenarios |
| `dashboard-system-status` | Added | `Greeting Computation` — new requirement with 3 scenarios |
| `dashboard-system-status` | Added | `Status Greeting Card` — new requirement with 4 scenarios |
| `dashboard-system-status` | Added | `Data Freshness` — new requirement with 2 scenarios |

**Source of truth**: `openspec/specs/dashboard-system-status/spec.md` now reflects all changes.

---

## 3. Files Created (10)

| File | Description |
|------|-------------|
| `src/Aura.Application/Ports/IDbReadinessProvider.cs` | Port interface for database readiness |
| `src/Aura.Application/Ports/ILlmReadinessProvider.cs` | Port interface for LLM readiness |
| `src/Aura.Infrastructure/HealthChecks/LlmHealthCheck.cs` | Health check probing Ollama `/api/tags` with ≤3s timeout |
| `src/Aura.Infrastructure/Adapters/Dashboard/DbReadinessAdapter.cs` | Adapter: HealthCheckService → filter "database" → ReadinessSignal |
| `src/Aura.Infrastructure/Adapters/Dashboard/LlmReadinessAdapter.cs` | Adapter: HealthCheckService → filter "llm" → ReadinessSignal |
| `src/Aura.UI/Components/Dashboard/StatusGreetingCard.razor` | Blazor component: greeting + 5 status dots, ≤60px, EventBus + polling |
| `tests/Aura.UnitTests/Health/LlmHealthCheckTests.cs` | Unit tests for LlmHealthCheck (healthy, unhealthy, throws) |
| `tests/Aura.UnitTests/Dashboard/DbReadinessAdapterTests.cs` | Unit tests for DbReadinessAdapter |
| `tests/Aura.UnitTests/Dashboard/LlmReadinessAdapterTests.cs` | Unit tests for LlmReadinessAdapter |
| `tests/Aura.UnitTests/UI/StatusGreetingCardTests.cs` | Unit tests for greeting, Overall, API fallback, aria-labels |

---

## 4. Files Modified (11)

| File | Description |
|------|-------------|
| `src/Aura.Application/Models/SystemStatusDto.cs` | Added `Database` + `Llm` fields to positional record |
| `src/Aura.UI/Models/SystemStatusResponse.cs` | Mirror DTO — added `Database` + `Llm` fields |
| `src/Aura.Application/Services/SystemStatusReader.cs` | Inject 2 providers, added `DeriveDatabaseIndicator()` + `DeriveLlmIndicator()` |
| `src/Aura.Infrastructure/Adapters/Dashboard/DependencyInjection.cs` | `AddScoped<IDbReadinessProvider, DbReadinessAdapter>()` + LLM mirror |
| `src/Aura.Infrastructure/DependencyInjection.cs` | `.AddCheck<LlmHealthCheck>("llm")` |
| `src/Aura.Api/Endpoints/DashboardEndpoints.cs` | Added activity tags + log params for Database, Llm |
| `src/Aura.UI/Components/Dashboard/PriorityDashboard.razor` | Inserted `<StatusGreetingCard />` before `<PrioritySummaryCards />` |
| `src/Aura.UI/wwwroot/css/stitch-dashboard.css` | Added `.status-greeting-card`, `.status-dot--*` classes |
| 3 test files (mechanical DTO updates) | Updated `SystemStatusDto`/`SystemStatusResponse` call sites |

---

## 5. Verification Results

| Check | Result |
|-------|--------|
| **Build** | ✅ 0 errors, 0 warnings |
| **Unit tests (new)** | ✅ 34/34 pass |
| **Unit tests (regression)** | ✅ 0 regressions (23 pre-existing failures in PullRequestsPageTests — unrelated) |
| **Integration tests** | ✅ 11/11 pass |
| **Architecture tests** | ✅ Pass (aura-clean-arch-guard confirmed layer isolation) |

**Compliance**: 13/18 spec scenarios covered with passing tests; 5 partial (greeting time periods, Warning overall, EventBus refresh, polling timer — covered by source inspection).

**Issues**: No CRITICAL. 3 WARNINGs documented:
1. Greeting period test coverage limited (DateTime.Now not mockable without external libs)
2. Data freshness tests absent (EventBus + polling verified by source inspection only)
3. Pre-existing parameter order bug in `PriorityDashboardRenderOrderTests` (not part of this change)

---

## 6. Architecture Impact

**Permanent changes to the architecture:**

| Aspect | Impact |
|--------|--------|
| **New ports** | `IDbReadinessProvider`, `ILlmReadinessProvider` added to `Application/Ports/` |
| **New adapters** | `DbReadinessAdapter`, `LlmReadinessAdapter` in `Infrastructure/Adapters/Dashboard/` |
| **New health check** | `LlmHealthCheck` in `Infrastructure/HealthChecks/` (dual-constructor pattern) |
| **DTO shape** | `SystemStatusDto` and `SystemStatusResponse` now have 5 fields (was 3) |
| **DI wiring** | 2 scoped services + 1 health check registration added |
| **CSS** | New `.status-greeting-card` and `.status-dot--*` classes added to design system |

All new code follows existing architectural patterns (ports in Application, adapters in Infrastructure, health checks with dual constructors, components in UI). No layer violations introduced.

---

## 7. Open Items

| Item | Priority | Notes |
|------|----------|-------|
| Extract greeting logic to static helper for testability | Low | `GetGreeting(int hour)` would enable afternoon/evening period tests without mocking DateTime.Now |
| Add data freshness unit tests | Low | EventBus subscription + polling timer tests would increase confidence |
| Fix `PriorityDashboardRenderOrderTests` parameter order | Low | Pre-existing issue; wrong positional order but no assertion failure because all values are "Ok" |
| Timezone-aware greeting | Deferred | Server local time for v1; user-configurable timezone explicitly out of scope |
| Ollama retry/fallback for LLM health check | Deferred | Currently shows Error on timeout; retry logic not needed for v1 |

---

## 8. Rollback Instructions

To revert this change completely:

```bash
# 1. Revert UI integration
#    Remove `<StatusGreetingCard />` from PriorityDashboard.razor
#    Delete StatusGreetingCard.razor
#    Remove .status-greeting-card / .status-dot--* CSS classes

# 2. Revert DI registrations
#    Remove AddCheck<LlmHealthCheck>("llm") from Infrastructure/DependencyInjection.cs
#    Remove IDbReadinessProvider + ILlmReadinessProvider scoped registrations

# 3. Revert DTO/reader
#    Remove Database + Llm fields from SystemStatusDto positional record
#    Remove Database + Llm fields from SystemStatusResponse
#    Remove IDbReadinessProvider + ILlmReadinessProvider injections from SystemStatusReader
#    Remove DeriveDatabaseIndicator() + DeriveLlmIndicator() methods

# 4. Delete new files
Remove-Item src/Aura.Application/Ports/IDbReadinessProvider.cs
Remove-Item src/Aura.Application/Ports/ILlmReadinessProvider.cs
Remove-Item src/Aura.Infrastructure/HealthChecks/LlmHealthCheck.cs
Remove-Item src/Aura.Infrastructure/Adapters/Dashboard/DbReadinessAdapter.cs
Remove-Item src/Aura.Infrastructure/Adapters/Dashboard/LlmReadinessAdapter.cs
Remove-Item src/Aura.UI/Components/Dashboard/StatusGreetingCard.razor

# 5. Revert endpoint changes
#    Remove Database/Llm activity tags from DashboardEndpoints.cs

# 6. Revert test mechanical updates
#    Remove Database/Llm params from all SystemStatusDto/SystemStatusResponse call sites
#    Delete new test files

# 7. Build + verify
dotnet build Aura.sln && dotnet test Aura.sln
```

---

## Archive Audit

| Artifact | Status |
|----------|--------|
| `proposal.md` | ✅ |
| `specs/dashboard-system-status/spec.md` | ✅ |
| `design.md` | ✅ |
| `tasks.md` | ✅ (22/22 tasks complete) |
| `verify-report.md` | ✅ (PASS WITH WARNINGS) |
| `archive-report.md` | ✅ (this file) |
