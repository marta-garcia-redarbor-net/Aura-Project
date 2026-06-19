# Design: W2-H2-T2 — Teams-First Connector Execution Flow

## Technical Approach

Introduce a capability-named `IConnectorAdapter` port in `Aura.Application.Ports`, implement it
with `TeamsConnectorAdapter` confined to `Aura.Infrastructure/Adapters/Connectors/Teams/`. A new
`ExecuteConnectorUseCase` in `Aura.Application/UseCases/ConnectorExecution/` (1) reads the
existing checkpoint read-only to derive a fetch window, (2) routes to the matching adapter by
`ConnectorName`, and (3) emits correlated telemetry via `ActivitySource` + `Meter` + `ILogger`.
`Aura.Workers` gains a thin `ConnectorExecutionWorker : BackgroundService` that resolves and
triggers the use case via `IServiceScopeFactory`. No checkpoint writes, no field mapping beyond
a stub item count (W2-H3).

## Architecture Decisions

| Option | Tradeoff | Decision |
|--------|----------|----------|
| Single `IConnectorExecutor` with built-in router vs. `IEnumerable<IConnectorAdapter>` routed by use case | Separate router class adds indirection; DI enumeration is native .NET and matches existing `IPlugin`/`IPluginRegistry` pattern | **`IConnectorAdapter` per provider; use case routes by `ConnectorName` string** |
| Use case in `Application/Services/` vs. new `Application/UseCases/` folder | `Services/` holds query readers; a dedicated `UseCases/` folder makes screaming-architecture intent explicit | **New `UseCases/ConnectorExecution/` folder** |
| Telemetry in use case vs. decorator | Decorator adds indirection for a single use case; `LoggerMessage` source-gen matches existing `GraphConnectorStatusReader` pattern | **Telemetry emitted directly in `ExecuteConnectorUseCase`** |
| Teams adapter under `Adapters/GraphConnector/` vs. new `Adapters/Connectors/Teams/` | `GraphConnector/` is for connector management, not ingestion execution; separate subtree avoids mixed concerns | **New `Adapters/Connectors/Teams/` subtree** |

## Data Flow

```
ConnectorExecutionWorker (Workers)
  └─→ IServiceScopeFactory → ExecuteConnectorUseCase (Application/UseCases)
         ├─ IIngestionCheckpointStore.GetAsync()  [read-only; W2-H2-T1 contract]
         │    → WindowStart = checkpoint.ProcessedAt ?? UtcToday 00:00:00
         │    → WindowEnd   = UtcNow
         ├─ IConnectorAdapter match by ConnectorName
         │    └─→ TeamsConnectorAdapter (Infrastructure/Adapters/Connectors/Teams)
         │              → Microsoft.Graph SDK call  [stub item count; W2-H3 maps fields]
         │              → ConnectorExecutionResult { Identity, ItemCount, Status }
         └─ ActivitySource.StartActivity() + Counter<int>.Add() + ILogger[LoggerMessage]
              (all share Activity.Current?.Id as correlation identifier)
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Application/Ports/IConnectorAdapter.cs` | Create | Port: `ConnectorName` + `ExecuteAsync(ConnectorExecutionRequest, CancellationToken)` |
| `src/Aura.Application/Models/ConnectorExecutionRequest.cs` | Create | `CheckpointIdentity Identity`, `DateTimeOffset WindowStart`, `DateTimeOffset WindowEnd` |
| `src/Aura.Application/Models/ConnectorExecutionResult.cs` | Create | `CheckpointIdentity Identity`, `int ItemCount`, `ConnectorExecutionStatus Status`, `string? FailureReason` |
| `src/Aura.Application/UseCases/ConnectorExecution/ExecuteConnectorUseCase.cs` | Create | Checkpoint read → window derivation → adapter dispatch → telemetry |
| `src/Aura.Application/DependencyInjection.cs` | Modify | Add `services.AddScoped<ExecuteConnectorUseCase>()` |
| `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsConnectorAdapter.cs` | Create | `IConnectorAdapter` impl; Graph SDK confined here; stub count; sealed partial with `LoggerMessage` |
| `src/Aura.Infrastructure/Adapters/Connectors/DependencyInjection.cs` | Create | `internal static AddConnectorAdapters()`; registers `TeamsConnectorAdapter` as `IConnectorAdapter` (singleton) |
| `src/Aura.Infrastructure/DependencyInjection.cs` | Modify | Call `services.AddConnectorAdapters(configuration)` |
| `src/Aura.Workers/ConnectorExecutionWorker.cs` | Create | `BackgroundService`; one-shot run via `IServiceScopeFactory`; logs start/result |
| `src/Aura.Workers/Program.cs` | Modify | Add `AddHostedService<ConnectorExecutionWorker>()` in full-mode block |
| `tests/Aura.UnitTests/Ingestion/ExecuteConnectorUseCaseTests.cs` | Create | Window derivation (checkpoint present/absent), adapter routing, failure propagation |
| `tests/Aura.ArchitectureTests/ConnectorExecutionArchitectureTests.cs` | Create | `IConnectorAdapter` in `Aura.Application.Ports`; no `Microsoft.Graph` ref above Infrastructure |

## Interfaces / Contracts

```csharp
// Aura.Application.Ports
public interface IConnectorAdapter
{
    string ConnectorName { get; }
    Task<ConnectorExecutionResult> ExecuteAsync(
        ConnectorExecutionRequest request, CancellationToken ct);
}

// Aura.Application.Models
public sealed record ConnectorExecutionRequest(
    CheckpointIdentity Identity,
    DateTimeOffset WindowStart,
    DateTimeOffset WindowEnd);

public sealed record ConnectorExecutionResult(
    CheckpointIdentity Identity,
    int ItemCount,
    ConnectorExecutionStatus Status,
    string? FailureReason = null);

public enum ConnectorExecutionStatus { Success, Failure }
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | Checkpoint present → `WindowStart` = `ProcessedAt`; absent → UTC today 00:00:00 | `InMemoryIngestionCheckpointStore` fake (existing in `Ingestion/Fakes/`) |
| Unit | Unregistered connector name → `Status=Failure`, no exception thrown | Stub `IConnectorAdapter` returning a named connector; assert routing failure |
| Unit | Adapter failure result → returned unchanged; error-level log emitted | Hand-written stub adapter + xUnit `FakeLogger` / NSubstitute |
| Unit | `ConnectorExecutionResult` failure MUST have non-null reason | Pure record construction assertions |
| Architecture | `IConnectorAdapter` resides in `Aura.Application.Ports` | NetArchTest `ResideInNamespace` (mirrors `IngestionArchitectureTests` pattern) |
| Architecture | `Aura.Application` has no `Microsoft.Graph` dependency | NetArchTest `ShouldNot().HaveDependencyOn("Microsoft.Graph")` |

## Migration / Rollout

No migration required. `ConnectorExecutionWorker` is additive and registered only in the
existing full-mode block in `Program.cs`. Removing its `AddHostedService` call restores prior
no-op behavior; no schema changes or persisted state are introduced in this slice.

## Open Questions

- [ ] **One-shot vs. loop**: `ConnectorExecutionWorker` designed as one-shot (run once, stop). Confirm before implementation — affects whether it uses `ExecuteAsync`-then-`StopApplication()` or a timed loop.
- [ ] **Connector name string**: canonical identifier for Teams (e.g. `"teams"` or `"microsoft-teams"`) must be agreed before DI wiring to avoid key mismatch with existing `CheckpointIdentity.Connector` values.
