# Design: Kernel Skeleton

## Technical Approach

Introduce a sequential plugin pipeline isolated to Domain, Application, and Workers. `WorkItem` is a domain entity with encapsulated state transitions. `IPlugin`/`IPluginRegistry` are application-level contracts (not ports — they represent internal kernel logic, not external adapters). `HelloKernelWorker` is a fire-once worker that validates the full pipeline wiring. No Api or Infrastructure changes. Matches proposal's Pipeline/Middleware approach and satisfies all spec scenarios.

## Architecture Decisions

| Decision | Options Considered | Choice | Rationale |
|---|---|---|---|
| WorkItem location | Domain entity vs Application model | Domain entity (class) | Has invariants and state transitions; not a DTO. Follows spec requirement for encapsulated state. Existing value objects are records — entity needs mutability. |
| Kernel contracts namespace | `Application/Ports` vs `Application/Kernel` | `Application/Kernel` | Ports are for external boundaries (adapters). Kernel contracts are internal application abstractions — distinct concern. |
| Pipeline error strategy | Skip-and-continue vs abort-on-failure | Abort remaining plugins for that WorkItem | Spec REQ-3 explicitly requires aborting subsequent plugins on failure while preserving worker stability. |
| Logging in Application | Propagate errors to Worker vs add `Logging.Abstractions` | Add `Microsoft.Extensions.Logging.Abstractions` to Application | Spec requires the pipeline itself to log errors. This is a standard .NET abstraction (same level as DI abstractions already referenced), not a provider SDK. |
| DI registration shape | Inline in `AddAuraApplication` vs separate `AddKernel()` | Private `AddKernel()` called from `AddAuraApplication()` | Isolates kernel DI from semantic DI. Reduces merge risk with parallel H5 changes. |

## Data Flow

```
HelloKernelWorker (Workers)
    │ creates WorkItem(Pending)
    │ resolves IPluginRegistry
    │ calls ExecuteAsync(workItem, ct)
    ▼
PluginRegistry (Application/Kernel)
    │ workItem.MarkProcessing()
    ├─ HelloPlugin.ExecuteAsync(workItem, ct)  ← success
    ├─ [future plugins...]
    │   on exception → log, workItem.MarkFaulted(reason), abort
    └─ workItem.MarkCompleted()
```

## File Changes

| File | Action | Description |
|---|---|---|
| `src/Aura.Domain/WorkItems/WorkItemStatus.cs` | Create | Enum: `Pending`, `Processing`, `Completed`, `Faulted` |
| `src/Aura.Domain/WorkItems/WorkItem.cs` | Create | Entity with `Id`, `Title`, `Source`, `Status`, `CreatedAt`. Methods: `MarkProcessing()`, `MarkCompleted()`, `MarkFaulted(string)` with transition guards. |
| `src/Aura.Application/Kernel/IPlugin.cs` | Create | `Task ExecuteAsync(WorkItem item, CancellationToken ct)` |
| `src/Aura.Application/Kernel/IPluginRegistry.cs` | Create | `Task ExecuteAsync(WorkItem item, CancellationToken ct)` |
| `src/Aura.Application/Kernel/PluginRegistry.cs` | Create | Sequential execution with try/catch per plugin, `ILogger`, abort-on-failure semantics. |
| `src/Aura.Application/Kernel/Plugins/HelloPlugin.cs` | Create | No-op plugin that logs execution. Proves pipeline wiring. |
| `src/Aura.Application/Aura.Application.csproj` | Modify | Add `Microsoft.Extensions.Logging.Abstractions` package reference. |
| `src/Aura.Application/DependencyInjection.cs` | Modify | Add private `AddKernel()` registering `IPluginRegistry` → `PluginRegistry` (singleton) and `IPlugin` → `HelloPlugin`. Called from `AddAuraApplication()`. |
| `src/Aura.Workers/HelloKernelWorker.cs` | Create | `BackgroundService`, runs once: create dummy WorkItem, execute pipeline, log result, stop. |
| `src/Aura.Workers/Program.cs` | Modify | Add `builder.Services.AddHostedService<HelloKernelWorker>()`. |

## Interfaces / Contracts

```csharp
// Aura.Application.Kernel
public interface IPlugin
{
    Task ExecuteAsync(WorkItem item, CancellationToken ct);
}

public interface IPluginRegistry
{
    Task ExecuteAsync(WorkItem item, CancellationToken ct);
}
```

```csharp
// Aura.Domain.WorkItems
public sealed class WorkItem
{
    public Guid Id { get; }
    public string Title { get; }
    public string Source { get; }
    public WorkItemStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public string? FaultReason { get; private set; }

    public void MarkProcessing();   // Pending → Processing
    public void MarkCompleted();    // Processing → Completed
    public void MarkFaulted(string reason); // Processing → Faulted
}
```

## Testing Strategy

| Layer | What to Test | Approach |
|---|---|---|
| Unit | `WorkItem` state transitions (valid + invalid) | xUnit, direct construction, assert `InvalidOperationException` on bad transitions |
| Unit | `PluginRegistry` sequential execution, empty registry, abort-on-failure | xUnit, mock `IPlugin` list, verify call order and abort behavior |
| Unit | `AddKernel()` DI resolution | xUnit, `ServiceCollection` + `BuildServiceProvider`, assert types resolve |
| Architecture | Kernel contracts stay in Application, WorkItem stays in Domain, no Infrastructure leaks | NetArchTest assertions extending existing `SemanticIndexArchitectureTests` pattern |

Integration/E2E tests are not justified for this skeleton — the unit + architecture tests cover all spec scenarios with lower cost.

## Migration / Rollout

No migration required. Additive change with zero impact on existing workers or the API layer.

## Open Questions

None — all decisions are self-contained within the skeleton scope.
