# Design: Dashboard System Status Card

## Technical Approach

Add Database + Llm health monitoring to the existing system status infrastructure (ports, adapters, health checks, DTO fields, DI), plus a compact StatusGreetingCard at the top of the priority dashboard. New adapters follow the exact QdrantReadinessAdapter pattern. Greeting and Overall state are computed client-side in Blazor, keeping the API DTO focused on raw indicator data.

Architecture verification (via aura-clean-arch-guard): passes. New ports live in Application, adapters in Infrastructure, health check in Infrastructure, component in UI. No crossing of layer boundaries.

## Architecture Decisions

| Decision | Choice | Alternative | Rationale |
|----------|--------|-------------|-----------|
| Overall indicator | Client-side in StatusGreetingCard | Server-side DTO field | Purely presentational aggregation; avoids coupling API to display rules |
| Greeting computation | Client-side `DateTime.Now.Hour` + `AuthenticationState` | Server-side in DTO | No API contract change needed; user display name already available in Blazor |
| LLM endpoint config | Reuse `LlmAdvisorOptions.Endpoint` | Dedicated health-check options | Same Ollama instance serves both advisor and health; falls back to `EmbeddingProvider:Endpoint` natively |
| Readiness adapter filter | `registration.Name == "database"` / `"llm"` | Type-based filtering | Established pattern in QdrantReadinessAdapter; name matches the existing `HealthCheckRegistration` |
| LlmHealthCheck constructor pattern | Dual-constructor (prod + test delegate) | Single constructor + mocking | Same pattern as QdrantHealthCheck; enables unit testing without HTTP |

## Data Flow

```
HealthCheckService──→DbReadinessAdapter──→IDbReadinessProvider──┐
HealthCheckService──→LlmReadinessAdapter──→ILlmReadinessProvider─┤
IQdrantReadinessProvider─────────────────────────────────────────┼──→SystemStatusReader──→API──→SystemStatusResponse
IApiReadinessProvider────────────────────────────────────────────┘     (5 indicators: Api, Qdrant, MockAuth, Database, Llm)
IMockAuthReadinessProvider─────────────────────────────────────────┘

StatusGreetingCard (Blazor)
  ├── ISystemStatusApiClient → GET /api/dashboard/status → 5 indicators
  ├── AuthenticationState (CascadingParameter) → UserDisplayName
  ├── DateTime.Now → greeting period (morning/afternoon/evening)
  └── Computed: Overall from all 5 indicators
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Application/Ports/IDbReadinessProvider.cs` | Create | `Task<ReadinessSignal> GetReadinessAsync(CancellationToken)` |
| `src/Aura.Application/Ports/ILlmReadinessProvider.cs` | Create | Same contract as Qdrant pattern |
| `src/Aura.Infrastructure/Adapters/Dashboard/DbReadinessAdapter.cs` | Create | `HealthCheckService` → filter `"database"` → map `HealthStatus` to `ReadinessSignal` |
| `src/Aura.Infrastructure/Adapters/Dashboard/LlmReadinessAdapter.cs` | Create | Same; filter `"llm"` |
| `src/Aura.Infrastructure/HealthChecks/LlmHealthCheck.cs` | Create | Dual-constructor; prod: `IHttpClientFactory` → `GET /api/tags` ≤3s; test: delegate |
| `src/Aura.UI/Components/Dashboard/StatusGreetingCard.razor` | Create | Greeting + 5 status dots; compact ≤60px; EventBus refresh + polling |
| `src/Aura.Application/Models/SystemStatusDto.cs` | Modify | Add `Database`, `Llm` fields to positional record |
| `src/Aura.UI/Models/SystemStatusResponse.cs` | Modify | Mirror DTO (add `Database`, `Llm`) |
| `src/Aura.Application/Services/SystemStatusReader.cs` | Modify | Inject 2 new providers + derive 2 new indicators |
| `src/Aura.Infrastructure/Adapters/Dashboard/DependencyInjection.cs` | Modify | `AddScoped<IDbReadinessProvider, DbReadinessAdapter>()` + LLM mirror |
| `src/Aura.Infrastructure/DependencyInjection.cs` | Modify | `.AddCheck<LlmHealthCheck>("llm")` after Qdrant check |
| `src/Aura.Api/Endpoints/DashboardEndpoints.cs` | Modify | Add activity tags + log params for `Database`, `Llm` states |
| `src/Aura.UI/Components/Dashboard/PriorityDashboard.razor` | Modify | Insert `<StatusGreetingCard />` before `<PrioritySummaryCards />` |
| `src/Aura.UI/wwwroot/css/stitch-dashboard.css` | Modify | Add `.status-greeting-card` + `.status-dot--*` classes |

## Interfaces / Contracts

```csharp
// Application/Ports/IDbReadinessProvider.cs — identical contract to IQdrantReadinessProvider
public interface IDbReadinessProvider
{
    Task<ReadinessSignal> GetReadinessAsync(CancellationToken cancellationToken);
}

// Application/Ports/ILlmReadinessProvider.cs — same contract
public interface ILlmReadinessProvider
{
    Task<ReadinessSignal> GetReadinessAsync(CancellationToken cancellationToken);
}

// SystemStatusDto (final positional record)
public sealed record SystemStatusDto(
    SystemIndicatorDto Api,
    SystemIndicatorDto Qdrant,
    SystemIndicatorDto MockAuth,
    SystemIndicatorDto Database,
    SystemIndicatorDto Llm);

// SystemStatusResponse (mirror)
public sealed record SystemStatusResponse(
    SystemIndicatorResponse Api,
    SystemIndicatorResponse Qdrant,
    SystemIndicatorResponse MockAuth,
    SystemIndicatorResponse Database,
    SystemIndicatorResponse Llm);
```

## Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| Unit | LlmHealthCheck — delegate constructor | GIVEN healthy/unhealthy delegate → THEN returns `HealthCheckResult.Healthy`/`.Unhealthy` |
| Unit | DbReadinessAdapter | GIVEN stubbed `HealthCheckService` with Healthy/Degraded/Unhealthy → THEN maps to correct `ReadinessSignal` |
| Unit | LlmReadinessAdapter | Same pattern |
| Unit | SystemStatusReader | GIVEN all 5 providers injected → THEN DTO contains 5 indicators in correct positions |
| Integration | API endpoint contract | GET `/api/dashboard/status` returns `db` + `llm` fields; POST returns 405 |
| Unit | StatusGreetingCard — render | GIVEN `SystemStatusResponse` → THEN greeting + 5 dots render; API fail shows "--" |
| Unit | StatusGreetingCard — Overall | GIVEN mixed indicator states → THEN Overall is Ok only when all Ok, Error on any Error, Warning on any Warning (no Error) |
| Unit | StatusGreetingCard — greeting | GIVEN `DateTime.Now.Hour` < 12 → "Good morning"; 12-17 → "Good afternoon"; 18+ → "Good evening" |
| UI | Accessibility | GIVEN 5 dots rendered → THEN each has `aria-label` matching "{name}: {state}" |

## Migration / Rollout

No migration required. DTO changes are additive positional record fields — all 14 existing call sites MUST be fixed in the same commit to avoid compilation failure. New card rendering is additive in the layout. Feature-flag: not needed (card is a visual addition with graceful fallback when API is down).

## Implementation Order

1. **Infrastructure**: ports (`IDbReadinessProvider`, `ILlmReadinessProvider`), health checks (`LlmHealthCheck`), adapters (`DbReadinessAdapter`, `LlmReadinessAdapter`), DI
2. **Application**: DTO (`SystemStatusDto`), reader (`SystemStatusReader`) with derived indicators
3. **API**: endpoint logging + activity tags for Database, Llm
4. **UI**: model (`SystemStatusResponse`), component (`StatusGreetingCard.razor`), CSS
5. **Tests**: all 10 test files covering unit + integration scenarios
6. **Build + verify**: `dotnet build` + `dotnet test Aura.sln`

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| `SystemStatusDto` positional record — adding 2 fields breaks all 14 existing call sites | High | Build failure | Isolated commit; fix all test sites before build (mechanical update — each site just adds 2 params) |
| LLM probe timeout slows dashboard response | Low | Perceived latency | ≤3s timeout per health check; independent probes (not cascaded); UI shows "—" on failure |
| Wrong greeting for non-UTC users | Low | Cosmetic | Server local time for v1; user-configurable timezone deferred to non-goal |
| LlmHealthCheck not registered when LLM disabled | Low | Missing indicator | Always register `LlmHealthCheck` in DI regardless of `LlmAdvisor:Enabled` flag; check is harmless when Ollama is unreachable |

## Open Questions

None.
