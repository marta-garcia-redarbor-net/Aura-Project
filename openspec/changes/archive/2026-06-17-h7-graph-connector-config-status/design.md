# Design: Graph Connector Configuration Status

## Technical Approach

Introduce a read-only configuration-status capability following the existing Ports & Adapters pattern.
Infrastructure binds `GraphConnectorOptions` from appsettings/env vars and exposes raw settings via
`IGraphConnectorSettingsProvider`. Application derives the four-state enum via `GraphConnectorStatusReader`
and exposes it through `IGraphConnectorStatusReader`. API surfaces a GET-only DTO endpoint. UI provides
a self-contained `GraphConnectorStatusPanel` component that calls a typed HTTP client.

No Graph SDK is introduced. No domain entity is required (pure read-model). Derivation logic lives in
Application and is unit-testable in isolation.

## Architecture Decisions

| Option | Tradeoff | Decision |
|--------|----------|----------|
| One port (`IGraphConnectorStatusReader` backed by infra that derives status) | Simpler, but mixes binding + derivation in Infrastructure — untestable in Application | ✗ Rejected |
| Two ports: `IGraphConnectorSettingsProvider` (raw settings) + Application service derives status | Infrastructure only binds; derivation logic is unit-tested without infrastructure | ✓ **Chosen** |
| Endpoint under `/api/dashboard/...` | Reuses group but breaks domain separation | ✗ Rejected |
| Endpoint under `/api/connectors/graph/status` | Clear domain namespace, extensible for future connectors | ✓ **Chosen** |
| Panel fetches via `CascadingValue` from `MainLayout` | Couples layout to connector state; inflates MainLayout | ✗ Rejected |
| Panel self-fetches via injected `IGraphConnectorApiClient` | Isolated; mirrors existing `IDashboardApiClient` typed-client pattern | ✓ **Chosen** |

## Data Flow

```
appsettings.json / env vars
        │
        ▼
GraphConnectorOptions  (Infrastructure binding)
        │
        ▼
IGraphConnectorSettingsProvider  (port, implemented by AppSettingsGraphConnectorSettingsProvider)
        │
        ▼
GraphConnectorStatusReader  (Application service, implements IGraphConnectorStatusReader)
        │  derivation rules (ordered):
        │  1. !Enabled         → Disabled
        │  2. !TenantId && !ClientId  → MissingConfig
        │  3. !TenantId || !ClientId || !HasValidCredentialsBlock → PartialConfig
        │  4. all present      → ValidConfig
        ▼
GraphConnectorStatusDto  (Application model, returned to API)
        │
        ▼
GET /api/connectors/graph/status  → HTTP 200 { "state": "ValidConfig" }
        │
        ▼
GraphConnectorApiClient  (UI typed client, implements IGraphConnectorApiClient)
        │
        ▼
GraphConnectorStatusPanel.razor  (read-only, data-testid per state, no edit controls)
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Application/Ports/IGraphConnectorSettingsProvider.cs` | Create | Port for reading raw connector settings |
| `src/Aura.Application/Ports/IGraphConnectorStatusReader.cs` | Create | Port the API endpoint consumes |
| `src/Aura.Application/Models/GraphConnectorSettings.cs` | Create | Raw settings record (Enabled, TenantId, ClientId, HasValidCredentialsBlock) |
| `src/Aura.Application/Models/GraphConnectorStatusDto.cs` | Create | DTO + `GraphConnectorState` enum |
| `src/Aura.Application/Services/GraphConnectorStatusReader.cs` | Create | Derivation service |
| `src/Aura.Application/DependencyInjection.cs` | Modify | Register `GraphConnectorStatusReader` as scoped |
| `src/Aura.Infrastructure/Adapters/GraphConnector/GraphConnectorOptions.cs` | Create | Options class (SectionName = `"GraphConnector"`) |
| `src/Aura.Infrastructure/Adapters/GraphConnector/AppSettingsGraphConnectorSettingsProvider.cs` | Create | Adapter: maps options → `GraphConnectorSettings` |
| `src/Aura.Infrastructure/Adapters/GraphConnector/DependencyInjection.cs` | Create | Internal DI: bind options + register adapter |
| `src/Aura.Infrastructure/DependencyInjection.cs` | Modify | Call `AddGraphConnectorAdapter(configuration)` |
| `src/Aura.Api/Endpoints/GraphConnectorEndpoints.cs` | Create | `MapGraphConnectorEndpoints()`, GET only, RequireAuthorization |
| `src/Aura.Api/Program.cs` | Modify | `app.MapGraphConnectorEndpoints()` |
| `src/Aura.Api/appsettings.Development.json` | Modify | Add `GraphConnector` section with defaults |
| `src/Aura.UI/Services/IGraphConnectorApiClient.cs` | Create | UI typed client interface |
| `src/Aura.UI/Services/GraphConnectorApiClient.cs` | Create | Typed HTTP client (`GET /api/connectors/graph/status`) |
| `src/Aura.UI/Models/GraphConnectorStatusResponse.cs` | Create | UI-side response record (`string State`) |
| `src/Aura.UI/Components/GraphConnector/GraphConnectorStatusPanel.razor` | Create | Self-contained panel; 4 states; `data-testid` per state |
| `src/Aura.UI/Pages/Index.razor` | Modify | Inject `IGraphConnectorApiClient`; add `<GraphConnectorStatusPanel>` |
| `src/Aura.UI/Program.cs` | Modify | Register `IGraphConnectorApiClient` typed HTTP client |
| `tests/Aura.UnitTests/GraphConnector/GraphConnectorStatusReaderTests.cs` | Create | 4 derivation scenarios + ordering edge cases |
| `tests/Aura.IntegrationTests/GraphConnector/GraphConnectorStatusEndpointTests.cs` | Create | API contract: 4 config scenarios, GET-only, auth guard |
| `tests/Aura.E2E/GraphConnector/GraphConnectorStatusSmokeTests.cs` | Create | UI smoke: stub API client, assert `data-testid` per state |
| `tests/Aura.ArchitectureTests/GraphConnectorArchitectureTests.cs` | Create | Assert no Graph SDK type in Application or UI namespaces |

## Interfaces / Contracts

```csharp
// Application.Models
public enum GraphConnectorState { Disabled, MissingConfig, PartialConfig, ValidConfig }
public sealed record GraphConnectorStatusDto(GraphConnectorState State);
public sealed record GraphConnectorSettings(
    bool Enabled, string? TenantId, string? ClientId, bool HasValidCredentialsBlock);

// Application.Ports
public interface IGraphConnectorSettingsProvider { GraphConnectorSettings GetSettings(); }
public interface IGraphConnectorStatusReader
    { Task<GraphConnectorStatusDto> GetStatusAsync(CancellationToken cancellationToken); }

// Infrastructure options (SectionName = "GraphConnector")
internal sealed class GraphConnectorOptions
    { bool Enabled; string? TenantId; string? ClientId; string? ClientSecret; }

// API DTO shape: GET /api/connectors/graph/status → 200 { "state": "ValidConfig" }

// UI.Models
public sealed record GraphConnectorStatusResponse(string State);
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | Derivation logic for all 4 states + priority ordering | Stub `IGraphConnectorSettingsProvider`; 5 test cases |
| Integration | GET returns correct state per config scenario; POST/PUT/DELETE → 405; 401 without token | `WebApplicationFactory<ApiMarker>`, stub `IGraphConnectorStatusReader` |
| E2E | UI renders correct `data-testid` for each of the 4 states; no edit affordance | `WebApplicationFactory<UiMarker>`, stub `IGraphConnectorApiClient` |
| Architecture | No Graph SDK type in Application or UI | `NetArchTest` on assemblies |

## Migration / Rollout

No migration required. New endpoint and UI panel added alongside existing dashboard. Revert is clean:
remove endpoint registration, UI panel, Infrastructure adapter, and tests.

## Open Questions

- None. All state rules, config source, and UI constraints are explicitly confirmed in the change brief.
