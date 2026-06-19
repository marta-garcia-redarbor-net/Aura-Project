# Design: W2-H3 — Teams Plugin Mapping and Work Item Persistence

## Technical Approach

Replace the `TeamsConnectorAdapter` stub with a real ACL that maps fixture payloads to canonical
`WorkItem`s, then enqueues them into a scoped Application-layer buffer (`IWorkItemBuffer`).
The use case drains the buffer after `adapter.ExecuteAsync` returns and orchestrates persistence
via `IWorkItemStore`. The adapter performs mapping only. `IConnectorAdapter` and
`ConnectorExecutionResult` are untouched.

---

## Architecture Decisions

### WorkItems handoff: scoped buffer port as the adapter-to-use-case channel

| Option | Tradeoff | Decision |
|---|---|---|
| Adapter maps AND persists via injected `IWorkItemStore` | Persistence leaks into Infrastructure; use case loses orchestration control | Rejected — violates approved direction |
| `IWorkItemBuffer` scoped port: adapter enqueues; use case drains and persists | Clean separation; no contract changes to `IConnectorAdapter` or `ConnectorExecutionResult` | ✅ Chosen |
| Embed WorkItems in `ConnectorExecutionResult` | Explicit constraint violation | Rejected |

**Rationale**: The buffer is the handoff contract between adapter (Infrastructure, mapping
responsibility) and use case (Application, persistence responsibility). Both depend on
`Aura.Application.Ports` — no layer boundary is crossed.

### Use case persistence aggregation

| Option | Tradeoff | Decision |
|---|---|---|
| Use case upgrades result to `PartialFailure` on any store error | Accurate status propagation; consistent with existing partial-failure semantics | ✅ Chosen |
| Ignore persistence failures silently | Misleads callers; status reflects only mapping | Rejected |

### In-memory store for W2-H3

| Option | Tradeoff | Decision |
|---|---|---|
| `ConcurrentDictionary<Guid, WorkItem>` + `Lock` in Infrastructure | No external dependency; matches `InMemoryIngestionCheckpointStore` pattern | ✅ Chosen |
| Persistent store (EF Core, Cosmos) | Out of scope; adds infra dependencies | Deferred |

---

## Data Flow

```
ExecuteConnectorUseCase.ExecuteAsync(identity)
    │
    ├─ IIngestionCheckpointStore.GetAsync → windowStart
    ├─ Build ConnectorExecutionRequest(identity, windowStart, now)
    │
    └─ TeamsConnectorAdapter.ExecuteAsync(request, ct)     ← mapping only
            │
            ├─ Load fixture payloads → TeamsMessageDto[]
            ├─ [foreach dto]
            │     TeamsWorkItemMapper.TryMap(dto) → WorkItem?
            │       ├─ Valid  → IWorkItemBuffer.Enqueue(item)
            │       └─ Fatal field missing → null (skip; log; batch continues)
            └─ return ConnectorExecutionResult(count, status, maxProcessedAt)
                                    ↑ unchanged public contract; no WorkItems
    │
    ├─ [Use case drains buffer]
    │     IWorkItemBuffer.Drain() → WorkItem[]
    │       [foreach item]
    │           IWorkItemStore.SaveAsync(item, ct) → WorkItemPersistenceResult
    │               └─ Any failure → captured; result upgraded to PartialFailure
    │
    ├─ PersistCheckpointAsync(result)      ← use case, unchanged
    ├─ EmitTelemetry(result)               ← use case, unchanged
    └─ return ConnectorExecutionResult     ← unchanged public contract
```

---

## File Changes

| File | Action | Description |
|---|---|---|
| `src/Aura.Application/Ports/IWorkItemStore.cs` | Create | Provider-neutral port: `SaveAsync(WorkItem, ct) → WorkItemPersistenceResult` |
| `src/Aura.Application/Ports/IWorkItemBuffer.cs` | Create | Handoff port: `Enqueue(WorkItem)` + `Drain() → IReadOnlyList<WorkItem>` |
| `src/Aura.Application/Models/WorkItemPersistenceResult.cs` | Create | Typed result: `IsSuccess`, `FailureReason`; no exceptions propagate |
| `src/Aura.Application/UseCases/ConnectorExecution/ExecuteConnectorUseCase.cs` | Modify | Inject `IWorkItemBuffer` + `IWorkItemStore`; drain buffer post-adapter; orchestrate persistence |
| `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsMessageDto.cs` | Create | Teams fixture DTO; stays in Infrastructure |
| `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsWorkItemMapper.cs` | Create | Maps `TeamsMessageDto → WorkItem?`; partial-payload tolerance; Metadata tracing |
| `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsConnectorAdapter.cs` | Modify | Inject `IWorkItemBuffer`; mapping + enqueue only; no persistence call |
| `src/Aura.Infrastructure/Adapters/WorkItems/InMemoryWorkItemStore.cs` | Create | `ConcurrentDictionary<Guid, WorkItem>` + `Lock` |
| `src/Aura.Infrastructure/Adapters/WorkItems/InMemoryWorkItemBuffer.cs` | Create | Thread-safe list; `Drain()` swaps internal list and returns snapshot |
| `src/Aura.Infrastructure/Adapters/WorkItems/DependencyInjection.cs` | Create | Registers `IWorkItemStore → InMemoryWorkItemStore` (singleton) + `IWorkItemBuffer → InMemoryWorkItemBuffer` (singleton) |
| `src/Aura.Infrastructure/Adapters/Connectors/DependencyInjection.cs` | Modify | Call `AddWorkItems()` from connector registration |
| `tests/Aura.UnitTests/Ingestion/Teams/TeamsWorkItemMapperTests.cs` | Create | Valid payload, missing optional, unrecognized priority, missing ExternalId (skip), Metadata entries |
| `tests/Aura.UnitTests/Ingestion/Teams/TeamsConnectorAdapterTests.cs` | Create | N fixtures → buffer has N enqueued items; partial skip → fewer enqueues; no store calls |
| `tests/Aura.UnitTests/WorkItems/InMemoryWorkItemStoreTests.cs` | Create | Save returns `Success`; failure path returns typed result |
| `tests/Aura.UnitTests/WorkItems/InMemoryWorkItemBufferTests.cs` | Create | Enqueue/Drain roundtrip; Drain empties buffer |
| `tests/Aura.UnitTests/ConnectorExecution/ExecuteConnectorUseCaseWorkItemTests.cs` | Create | Buffer drained after adapter; store called per item; persistence failure upgrades to `PartialFailure` |
| `tests/Aura.ArchitectureTests/ConnectorExecutionArchitectureTests.cs` | Modify | Add: Teams types absent from Application/Domain; `IWorkItemStore` + `IWorkItemBuffer` in `Aura.Application.Ports` |

---

## Interfaces / Contracts

```csharp
// Aura.Application.Ports
public interface IWorkItemBuffer
{
    void Enqueue(WorkItem item);
    IReadOnlyList<WorkItem> Drain();  // empties the buffer; returns captured items
}

public interface IWorkItemStore
{
    Task<WorkItemPersistenceResult> SaveAsync(WorkItem item, CancellationToken ct);
}

// Aura.Application.Models
public sealed record WorkItemPersistenceResult
{
    public bool IsSuccess { get; init; }
    public string? FailureReason { get; init; }

    public static WorkItemPersistenceResult Success()
        => new() { IsSuccess = true };

    public static WorkItemPersistenceResult Failure(string reason)
        => new() { IsSuccess = false, FailureReason = reason };
}
```

---

## Testing Strategy

| Layer | What to Test | Approach |
|---|---|---|
| Unit | `TeamsWorkItemMapper`: valid/partial/missing-required payloads, priority defaulting, Metadata entries | xUnit facts with inline fixture builders |
| Unit | `TeamsConnectorAdapter`: N fixtures → N buffer enqueues; partial skip → fewer enqueues; zero store calls | xUnit + NSubstitute for `IWorkItemBuffer` |
| Unit | `InMemoryWorkItemStore`: save returns `Success`; failure path typed result | xUnit facts |
| Unit | `InMemoryWorkItemBuffer`: enqueue/drain roundtrip; drain empties buffer | xUnit facts |
| Unit | `ExecuteConnectorUseCase`: buffer drained post-adapter; store called per item; store failure upgrades `ConnectorExecutionResult` to `PartialFailure` | xUnit + NSubstitute for `IWorkItemBuffer` + `IWorkItemStore` |
| Architecture | `IWorkItemStore` + `IWorkItemBuffer` in `Aura.Application.Ports`; Teams DTOs absent from Application/Domain | NetArchTest — extends `ConnectorExecutionArchitectureTests` |

---

## Migration / Rollout

No migration required. `InMemoryWorkItemStore` and `InMemoryWorkItemBuffer` are ephemeral and
carry no schema. Rollback: revert W2-H3 commits; adapter returns to stub; new ports and stores
removed cleanly.

---

## Open Questions

None.
