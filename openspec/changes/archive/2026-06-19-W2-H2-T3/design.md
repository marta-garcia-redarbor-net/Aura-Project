# Design: W2-H2-T3 — Ingestion Checkpoint Persistence

## Technical Approach

Promote `ExecuteConnectorUseCase` from read-only to read-then-persist by: (1) splitting
`IngestionCheckpoint.ProcessedAt` into two independent timestamps, (2) adding `MaxProcessedAt`
and `PartialFailure` to `ConnectorExecutionResult`, and (3) applying the four-outcome policy
inside `ExecuteAsync` after the adapter returns. All policy logic stays in `Application`;
`Infrastructure` and `Workers` are untouched structurally.

## Architecture Decisions

| # | Option | Tradeoff | Decision |
|---|--------|----------|----------|
| A | Add `PartialFailure` to `ConnectorExecutionStatus` enum | Breaks structural equality on existing test fixtures that construct `Success` results — easy to fix | **Add it.** Spec requires it; the enum is the canonical signal. |
| B | Carry `MaxProcessedAt` on `ConnectorExecutionResult` vs. separate return type | Separate type adds indirection for no benefit at this scope | **Extend the existing record.** Adapters already return it; the field is null-safe for non-partial paths. |
| C | Persist inside `ExecuteAsync` vs. dedicated `CheckpointPolicyService` | Dedicated service is cleaner for future richness but over-engineers this slice | **Inline policy in `ExecuteAsync`** via private `PersistCheckpointAsync` helper. Matches exploration recommendation. |
| D | Window start from `MaxProcessedAt` vs. keeping old `ProcessedAt` name | Field rename requires updating one constructor call in existing tests | **Rename `ProcessedAt` → `MaxProcessedAt`** in `IngestionCheckpoint`. Aligns semantics with spec; no ambiguity. |
| E | Idempotency guard: explicit `Max()` vs. rely on window bounding | Window start = `MaxProcessedAt`, so adapter can't return items older than current max; explicit `Max()` adds safety for edge cases | **No explicit `Max()`** — window bounding is sufficient. Covered by idempotency test scenario. |

## Data Flow

```
ExecuteAsync(identity, ct)
  │
  ├─ GetAsync(identity) ──→ prior: IngestionCheckpoint?
  │   windowStart = prior?.MaxProcessedAt ?? UTC_today_start
  │
  ├─ Find adapter  ──→ null? → Failure result → return  [no save]
  │
  ├─ adapter.ExecuteAsync(request) ──→ result  [or exception → Failure] → return  [no save]
  │
  ├─ PersistCheckpointAsync(identity, prior, result, now, ct)
  │     Status=Failure        → skip SaveAsync
  │     Status=Success, n=0   → SaveAsync(MaxProcessedAt=prior?.MaxProcessedAt, FinishedAt=now)
  │     Status=Success, n>0   → SaveAsync(MaxProcessedAt=result.MaxProcessedAt, FinishedAt=now)
  │     Status=PartialFailure → SaveAsync(MaxProcessedAt=result.MaxProcessedAt, FinishedAt=prior?.ExecutionFinishedAt)
  │
  ├─ EmitTelemetry(activity, result)
  └─ return result
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Application/Models/ConnectorExecutionResult.cs` | Modify | Add `MaxProcessedAt: DateTimeOffset?`; add `PartialFailure` to enum |
| `src/Aura.Application/Models/IngestionCheckpoint.cs` | Modify | Rename `ProcessedAt` → `MaxProcessedAt`; add `ExecutionFinishedAt: DateTimeOffset?` |
| `src/Aura.Application/UseCases/ConnectorExecution/ExecuteConnectorUseCase.cs` | Modify | Use `MaxProcessedAt` for window; add `PersistCheckpointAsync`; update telemetry for `PartialFailure` |
| `src/Aura.Infrastructure/Adapters/Ingestion/InMemoryIngestionCheckpointStore.cs` | No change | Store is shape-agnostic; record change propagates automatically |
| `tests/Aura.UnitTests/Ingestion/ExecuteConnectorUseCaseTests.cs` | Modify | Fix checkpoint construction (new 3-field shape); add 5 new policy/idempotency tests |
| `tests/Aura.ArchitectureTests/IngestionArchitectureTests.cs` | Verify | No code change; run to confirm no SDK leakage after model changes |

## Interfaces / Contracts

```csharp
// Models/ConnectorExecutionResult.cs
public sealed record ConnectorExecutionResult(
    CheckpointIdentity Identity,
    int ItemCount,
    ConnectorExecutionStatus Status,
    string? FailureReason = null,
    DateTimeOffset? MaxProcessedAt = null);   // NEW — null when no items succeeded

public enum ConnectorExecutionStatus { Success, Failure, PartialFailure }  // PartialFailure NEW

// Models/IngestionCheckpoint.cs
public sealed record IngestionCheckpoint(
    string? Cursor,
    DateTimeOffset? MaxProcessedAt,          // RENAMED from ProcessedAt
    DateTimeOffset? ExecutionFinishedAt);    // NEW

// ExecuteConnectorUseCase — private helper (no port change required)
private Task PersistCheckpointAsync(
    CheckpointIdentity identity,
    IngestionCheckpoint? prior,
    ConnectorExecutionResult result,
    DateTimeOffset now,
    CancellationToken ct);
```

**Port contracts unchanged:** `IIngestionCheckpointStore` and `IConnectorAdapter` signatures are
unaffected. Adapters gain the responsibility to populate `MaxProcessedAt` in their results.

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | Full-success+items → both timestamps advance | NSubstitute mock store; verify `SaveAsync` call args |
| Unit | Full-success+no-items → only `ExecutionFinishedAt` advances | Same setup with `ItemCount=0, MaxProcessedAt=null` |
| Unit | Full failure → `SaveAsync` never called | `DidNotReceive().SaveAsync(...)` |
| Unit | Partial failure → only `MaxProcessedAt` advances | `PartialFailure` status; verify `ExecutionFinishedAt=prior.ExecutionFinishedAt` |
| Unit | Idempotency — repeated run does not regress | Second call with same items; assert stored value ≥ first stored value |
| Unit | Existing window-bounding tests | Update fixture construction for 3-field `IngestionCheckpoint` |
| Architecture | No SDK types in Application after model change | Run existing `IngestionArchitectureTests` as-is |

## Migration / Rollout

No migration required. `InMemoryIngestionCheckpointStore` is the persistence backend for this
slice; no schema exists. Record rename (`ProcessedAt` → `MaxProcessedAt`) breaks the existing
two-argument `IngestionCheckpoint` constructor calls — all are in test files and the use case;
each is a trivial one-line fix.

## Open Questions

- None. Policy is fully specified by the six domain rules. All decisions resolved above.
