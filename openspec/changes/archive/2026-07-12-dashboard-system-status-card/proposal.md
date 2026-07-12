# Proposal: Dashboard System Status Card

## Intent

Users land on `/dashboard` with no system health sense. Add a compact greeting + live green/red dots for 5 indicators, reducing uncertainty without distracting from priority cards below.

## Scope

**In**: Greeting (server UTC time-of-day); 5 dots — Overall, API, DB, Qdrant, LLM; Overall = ALL(api, db, qdrant, llm, mockauth) healthy; `IDbReadinessProvider` + `DbReadinessAdapter`; `ILlmReadinessProvider` + `LlmReadinessAdapter` + `LlmHealthCheck` (HTTP GET `/api/tags` via `IHttpClientFactory`); update DTOs + UI model; new `StatusGreetingCard.razor`; DI registration; 10 test files.

**Out**: Replacing `AlwaysHealthyApiReadinessAdapter` stub; mock-auth changes; alerts/paging; timezone detection; Ollama retry/fallback; Playwright visual E2E.

## Capabilities

**New**: None. **Modified**: `dashboard-system-status` — add Database + Llm indicators, greeting, Overall aggregation, new ports/adapters/health checks.

## Approach

1. **Ports**: `IDbReadinessProvider`, `ILlmReadinessProvider` in `Application/Ports/`
2. **Adapters**: `DbReadinessAdapter` calls `HealthCheckService` for DbHealthCheck; `LlmReadinessAdapter` probes Ollama `/api/tags` via `IHttpClientFactory`
3. **HealthCheck**: `LlmHealthCheck` — IHealthCheck, ≤3s timeout, same dual-constructor pattern as QdrantHealthCheck
4. **Greeting**: Compute client-side in `StatusGreetingCard.razor` from `DateTime.Now.Hour`; no DTO field needed (user name already available via CascadingValue)
5. **Overall**: Aggregate client-side in `StatusGreetingCard.razor` — green only when ALL(api, db, qdrant, llm, mockAuth) Ok; red otherwise
6. **UI**: `StatusGreetingCard.razor` — compact horizontal bar, reuse `status-dot` CSS classes
7. **TDD**: All new code RED-GREEN-REFACTOR; `dotnet test Aura.sln` green at every stage

## Affected Areas

| Path | Impact |
|------|--------|
| `Application/Ports/IDbReadinessProvider.cs` | New |
| `Application/Ports/ILlmReadinessProvider.cs` | New |
| `Infrastructure/Adapters/Dashboard/DbReadinessAdapter.cs` | New |
| `Infrastructure/Adapters/Dashboard/LlmReadinessAdapter.cs` | New |
| `Infrastructure/HealthChecks/LlmHealthCheck.cs` | New |
| `UI/Components/Dashboard/StatusGreetingCard.razor` | New |
| `Application/Models/SystemStatusDto.cs` | Modified (+Db, +Llm) |
| `Application/Services/SystemStatusReader.cs` | Modified (+providers, +2 indicators) |
| `UI/Models/SystemStatusResponse.cs` | Modified (+Db, +Llm) |
| `UI/Components/Dashboard/PriorityDashboard.razor` | Modified (+StatusGreetingCard) |
| `Infrastructure/Adapters/Dashboard/DependencyInjection.cs` | Modified (+providers) |
| `Infrastructure/DependencyInjection.cs` | Modified (+LlmHealthCheck) |
| `Api/Endpoints/DashboardEndpoints.cs` | Modified (+log tags) |
| `UI/wwwroot/css/stitch-dashboard.css` | Modified (+card styles) |
| 10 test files | Modified (+new cases) |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| `SystemStatusDto` positional record — adding fields breaks all 14 call sites | Med | Isolated commit + fix all test sites before build |
| LLM probe slows dashboard if Ollama unresponsive | Low | Timeout ≤3s; parallel evaluation |
| Wrong greeting for non-UTC users | Low | Deferred; server UTC for v1 |

## Rollback

Single-commit revert: un-import card from `PriorityDashboard.razor`, revert both DI files, remove new DTO fields (no other consumers yet).

## Dependencies

- Ollama running with `/api/tags` reachable (card shows Error when down — optional)
- `DbHealthCheck` already registered (confirmed in explore)

## Success Criteria

- `dotnet test Aura.sln` green
- API returns Db + Llm fields in `/api/dashboard/status`
- Card greeting matches time-of-day (morning <12, afternoon <18, evening ≥18)
- Overall=Error when any indicator non-Ok; Overall=Ok when all healthy
- Architecture tests pass
