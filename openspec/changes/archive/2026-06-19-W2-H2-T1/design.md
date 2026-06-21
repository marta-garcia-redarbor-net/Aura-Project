# Design: Ingestion Checkpoint Store Contract

## Technical Approach

Add two immutable value records (`CheckpointIdentity`, `IngestionCheckpoint`) and one
Application-layer port (`IIngestionCheckpointStore`) to `Aura.Application`. No adapter
is created in this slice — the contract alone unblocks W2-H2-T2/T3. Follows the
`ISemanticOutboxRepository` pattern: interface in `Ports/`, models in `Models/`, no SDK
types escaping `Infrastructure`. First-run window is a documented caller responsibility,
not a stored field.

## Architecture Decisions

| Decision | Options | Choice | Rationale |
|---|---|---|---|
| Model type for identity/value | `sealed class` vs `sealed record` | `sealed record` | Pure value containers with no mutable state; structural equality is free with records; matches .NET 9 idioms |
| Identity placement | Embed identity in value model vs separate port parameters | Separate params (identity + value) | Spec explicitly separates identity and value; prevents triple-string confusion at call sites; matches spec scenario wording |
| First-run window | Store today-window field vs caller contract | Caller contract, documented in XML doc | Spec is unambiguous: store returns null, caller applies UTC-today bound; embedding would conflate persistence with business rule |
| Model namespace | Sub-namespace `Aura.Application.Models.Ingestion` vs flat `Aura.Application.Models` | Flat `Aura.Application.Models` | All existing Application models (SemanticOutboxEntry, AuraUser, etc.) live flat; no sub-namespace precedent |

## Data Flow

```
Ingestion caller
  │
  ├─ GetAsync(identity) ──→ IIngestionCheckpointStore ──→ adapter (future)
  │                                                        │
  │         ┌──────────── null (first run) ◄──────────────┘
  │         │
  │         │  caller applies UTC-today window (00:00 → now)
  │         ▼
  │    fetch from source connector using bounded window
  │
  └─ SaveAsync(identity, new checkpoint) ──→ IIngestionCheckpointStore
```

Resumption path (checkpoint present):

```
GetAsync ──→ IngestionCheckpoint { Cursor, ProcessedAt }
           caller uses cursor/timestamp directly — no today-window applied
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Application/Models/CheckpointIdentity.cs` | Create | `sealed record` — composite key (Connector, Source, Tenant) with guard against null/empty per spec |
| `src/Aura.Application/Models/IngestionCheckpoint.cs` | Create | `sealed record` — value shape (Cursor: string?, ProcessedAt: DateTimeOffset?) |
| `src/Aura.Application/Ports/IIngestionCheckpointStore.cs` | Create | Application port with `GetAsync` / `SaveAsync`; first-run contract documented in XML doc |
| `tests/Aura.UnitTests/Ingestion/CheckpointIdentityTests.cs` | Create | Guard-invariant tests for `CheckpointIdentity` (null/empty per field, valid construction) |
| `tests/Aura.ArchitectureTests/IngestionArchitectureTests.cs` | Create | NetArchTest rules: port in `Aura.Application.Ports`, no SDK/Infrastructure references in Application |
| `docs/architecture/ingestion/05-normalization-checkpoints.md` | Modify | Replace placeholder with adopted identity/value shape and first-run caller contract |

## Interfaces / Contracts

```csharp
// Aura.Application.Models.CheckpointIdentity
public sealed record CheckpointIdentity(string Connector, string Source, string Tenant);
// constructor MUST guard: Connector, Source, Tenant non-null/non-empty → ArgumentException

// Aura.Application.Models.IngestionCheckpoint
public sealed record IngestionCheckpoint(string? Cursor, DateTimeOffset? ProcessedAt);
// both fields nullable by contract — no guard needed
```

```csharp
// Aura.Application.Ports.IIngestionCheckpointStore
using Aura.Application.Models;

/// <summary>
/// Port for ingestion checkpoint persistence.
/// When <see cref="GetAsync"/> returns null (no prior checkpoint), callers MUST
/// bound the initial data fetch to the UTC-today window (00:00:00 → UtcNow).
/// Implementation lives in Infrastructure — never reference SDK types here.
/// </summary>
public interface IIngestionCheckpointStore
{
    /// <summary>Returns the stored checkpoint for the given identity, or null if none exists.</summary>
    Task<IngestionCheckpoint?> GetAsync(CheckpointIdentity identity, CancellationToken ct);

    /// <summary>Writes or replaces the checkpoint for the given identity.</summary>
    Task SaveAsync(CheckpointIdentity identity, IngestionCheckpoint checkpoint, CancellationToken ct);
}
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | `CheckpointIdentity` guard invariants: null/empty for each of Connector, Source, Tenant | xUnit `[Theory]` with `ArgumentException` expected — mirrors `SemanticOutboxEntryTests` |
| Unit | `IngestionCheckpoint` accepts simultaneous null fields | xUnit `[Fact]`, plain construction |
| Unit | `CheckpointIdentity` equality is structural (two instances with same values are equal) | xUnit `[Fact]`, record equality assertion |
| Architecture | Port resides in `Aura.Application.Ports`; Application does not reference Infrastructure or any external SDK | NetArchTest in `IngestionArchitectureTests.cs` |
| Integration | Persistence round-trip (save→get returns same value; unknown identity returns null) | **Deferred** to adapter slice; no adapter exists in this slice |

## Migration / Rollout

No migration required. This slice is additive (new files only); no existing runtime path
references it. Rollback = delete the three new source files and revert the doc edit.

## Open Questions

- None. All ambiguous decisions (identity shape, first-run semantics, adapter deferral) are
  locked by orchestrator instructions and aligned with spec requirements.
