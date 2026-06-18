# Design: W1-H6 Dashboard Status & Progress

## Technical Approach

Mirror the existing `graph-connector-status` pattern for both capabilities. Application
services derive state from narrow Infrastructure ports; `Aura.Api` adds two GET-only
endpoints to the existing `/api/dashboard` group; Blazor components consume DTOs with no
business logic. Module progress is served from a seeded Infrastructure adapter registered
against a port interface — swap to a live source requires only a DI re-registration.

---

## Architecture Decisions

| Option | Tradeoff | Decision |
|---|---|---|
| Single combined endpoint vs. two separate endpoints | Combined reduces round-trips; separate enforces the approved capability split and lets each endpoint evolve independently | **Two separate GET endpoints** — `/api/dashboard/system-status` and `/api/dashboard/module-progress` |
| One aggregate readiness port vs. three narrow ports | Aggregate is simpler to register; narrow ports isolate failure signals and let each adapter evolve or be mocked independently | **Three narrow ports**: `IApiReadinessProvider`, `IQdrantReadinessProvider`, `IMockAuthReadinessProvider` |
| Microcopy in Application DTO vs. in UI | UI microcopy leaks presentation decision into UI code; Application DTO carries microcopy so derivation + explanation are co-located and unit-testable | **Microcopy inside `SystemIndicatorDto`** |
| Hardcode seeded data in Application vs. port-first | Hardcode is simpler today but blocks swapping without touching Application; port-first isolates the seam | **`IModuleProgressProvider` port** in Application; `SeededModuleProgressProvider` in Infrastructure |

---

## Data Flow

**System Status**

    SystemStatusPanel.razor
      └─→ ISystemStatusApiClient   HTTP GET /api/dashboard/system-status
            └─→ DashboardEndpoints.GetSystemStatusAsync
                  └─→ ISystemStatusReader.GetStatusAsync (Application)
                        ├─→ IApiReadinessProvider       → AlwaysHealthyApiReadinessAdapter
                        ├─→ IQdrantReadinessProvider    → QdrantReadinessAdapter
                        └─→ IMockAuthReadinessProvider  → MockJwtOptionsReadinessAdapter
                  └─← SystemStatusDto { Api, Qdrant, MockAuth : {State, Microcopy} }
      └─← SystemStatusResponse (UI view model) ← JSON

**Module Progress**

    ModuleProgressPanel.razor
      └─→ IModuleProgressApiClient   HTTP GET /api/dashboard/module-progress
            └─→ DashboardEndpoints.GetModuleProgressAsync
                  └─→ IModuleProgressReader.GetAsync (Application)
                        └─→ IModuleProgressProvider    → SeededModuleProgressProvider
                                                          ↑ swap point for live data
                  └─← ModuleProgressDto { Entries[], IsSeeded: true }
      └─← ModuleProgressResponse (UI view model) ← JSON

---

## File Changes

| File | Action | Description |
|---|---|---|
| `src/Aura.Application/Ports/ISystemStatusReader.cs` | Create | Port consumed by the API endpoint |
| `src/Aura.Application/Ports/IApiReadinessProvider.cs` | Create | Narrow raw-signal port for API self-check |
| `src/Aura.Application/Ports/IQdrantReadinessProvider.cs` | Create | Narrow raw-signal port for Qdrant reachability |
| `src/Aura.Application/Ports/IMockAuthReadinessProvider.cs` | Create | Narrow raw-signal port for mock-auth config check |
| `src/Aura.Application/Ports/IModuleProgressReader.cs` | Create | Port consumed by the API endpoint |
| `src/Aura.Application/Ports/IModuleProgressProvider.cs` | Create | Data-access port — seeded → live swap point |
| `src/Aura.Application/Models/SystemStatusDto.cs` | Create | `SystemIndicatorState`, `SystemIndicatorDto`, `SystemStatusDto` |
| `src/Aura.Application/Models/ModuleProgressDto.cs` | Create | `ModuleProgressState`, `ModuleEntryDto`, `ModuleProgressDto` |
| `src/Aura.Application/Services/SystemStatusReader.cs` | Create | Derives tri-state from the three raw-signal ports |
| `src/Aura.Application/Services/ModuleProgressReader.cs` | Create | Delegates to `IModuleProgressProvider`; wraps result in DTO |
| `src/Aura.Application/DependencyInjection.cs` | Modify | Register `ISystemStatusReader`, `IModuleProgressReader` (scoped) |
| `src/Aura.Infrastructure/Adapters/Dashboard/AlwaysHealthyApiReadinessAdapter.cs` | Create | Returns `true`; trivially healthy when the endpoint is callable |
| `src/Aura.Infrastructure/Adapters/Dashboard/QdrantReadinessAdapter.cs` | Create | Reads `QdrantHealthCheck` result or performs lightweight TCP check |
| `src/Aura.Infrastructure/Adapters/Dashboard/MockJwtOptionsReadinessAdapter.cs` | Create | Checks `MockJwtOptions` has non-empty `Key`, `Issuer`, `Audience` |
| `src/Aura.Infrastructure/Adapters/Dashboard/SeededModuleProgressProvider.cs` | Create | Returns hardcoded entries; always sets `IsSeeded = true` |
| `src/Aura.Infrastructure/Adapters/Dashboard/DependencyInjection.cs` | Create | Internal DI wiring for the four Dashboard adapters |
| `src/Aura.Infrastructure/DependencyInjection.cs` | Modify | Call `AddDashboardAdapters(configuration, environment)` |
| `src/Aura.Api/Endpoints/DashboardEndpoints.cs` | Modify | Add `GET /system-status` and `GET /module-progress` inside existing authorized group |
| `src/Aura.UI/Models/SystemStatusResponse.cs` | Create | View model for system-status JSON deserialization |
| `src/Aura.UI/Models/ModuleProgressResponse.cs` | Create | View model for module-progress JSON deserialization |
| `src/Aura.UI/Services/ISystemStatusApiClient.cs` | Create | API client interface |
| `src/Aura.UI/Services/SystemStatusApiClient.cs` | Create | `HttpClient`-based client; matches `GraphConnectorApiClient` pattern |
| `src/Aura.UI/Services/IModuleProgressApiClient.cs` | Create | API client interface |
| `src/Aura.UI/Services/ModuleProgressApiClient.cs` | Create | `HttpClient`-based client |
| `src/Aura.UI/Components/Dashboard/SystemStatusPanel.razor` | Create | Three indicators with loading/error states; no edit affordance |
| `src/Aura.UI/Components/Dashboard/ModuleProgressPanel.razor` | Create | Progress list with loading/empty/error states; no edit affordance |
| `tests/Aura.UnitTests/Dashboard/SystemStatusReaderTests.cs` | Create | All derivation combinations per indicator; NSubstitute mocks |
| `tests/Aura.UnitTests/Dashboard/ModuleProgressReaderTests.cs` | Create | Reader contract; provider-agnostic via NSubstitute |
| `tests/Aura.ArchitectureTests/DashboardArchitectureTests.cs` | Create | Infrastructure types absent from Application and UI namespaces |

---

## Interfaces / Contracts

```csharp
// Aura.Application/Models/SystemStatusDto.cs
public enum SystemIndicatorState { Ok, Warning, Error }
public sealed record SystemIndicatorDto(SystemIndicatorState State, string Microcopy);
public sealed record SystemStatusDto(SystemIndicatorDto Api, SystemIndicatorDto Qdrant, SystemIndicatorDto MockAuth);

// Aura.Application/Models/ModuleProgressDto.cs
public enum ModuleProgressState { Pending, InProgress, Completed }
public sealed record ModuleEntryDto(string ModuleId, ModuleProgressState State);
public sealed record ModuleProgressDto(IReadOnlyList<ModuleEntryDto> Entries, bool IsSeeded);

// Narrow signal ports (non-obvious: three interfaces, not one aggregate)
public interface IApiReadinessProvider      { Task<bool> IsHealthyAsync(CancellationToken ct); }
public interface IQdrantReadinessProvider   { Task<bool> IsHealthyAsync(CancellationToken ct); }
public interface IMockAuthReadinessProvider { bool IsConfigured(); }

// Data-access port — the swap seam
public interface IModuleProgressProvider    { Task<ModuleProgressDto> GetAsync(CancellationToken ct); }
```

---

## Testing Strategy

| Layer | What to Test | Approach |
|---|---|---|
| Unit | `SystemStatusReader` — all tri-state derivation combinations across three indicators | xUnit + NSubstitute + `NullLogger<T>` |
| Unit | `ModuleProgressReader` — delegates to provider unchanged; `IsSeeded` preserved | xUnit + NSubstitute |
| Architecture | Infrastructure and provider-specific types absent from `Aura.Application` and `Aura.UI` namespaces | NetArchTest in `DashboardArchitectureTests` |

E2E / Playwright: explicitly out of scope per proposal.

---

## Migration / Rollout

No migration required. **Live-data swap path for module progress** (explicit):

1. Create a new class in `src/Aura.Infrastructure/Adapters/Dashboard/` implementing `IModuleProgressProvider`.
2. Update the DI binding in `src/Aura.Infrastructure/Adapters/Dashboard/DependencyInjection.cs` — replace `SeededModuleProgressProvider` with the new adapter.
3. The live adapter sets `IsSeeded = false` in the returned `ModuleProgressDto`.
4. Zero changes required to `Aura.Application`, `Aura.Api`, or `Aura.UI`.

---

## Open Questions

- None.
