# Design: W3-H2-B — Audit Trail and Pipeline Propagation

## Technical Approach

Four-stage seam extension across the existing outbox pipeline: (1) extend `NotificationOutboxEntry` with nullable verdict fields, (2) persist them as nullable TEXT columns in SQLite, (3) have `EvaluateAndEnqueueAsync` write the full verdict instead of just `TriggerRule`, and (4) have the worker read persisted fields rather than synthesizing fake verdicts. All new fields are nullable — old rows without migration remain fully valid.

## Architecture Decisions

| Decision | Options / Tradeoff | Chosen Approach / Rationale |
|---|---|---|
| RuleResults storage | Normalized table vs JSON column | **JSON column** — we only pass through, never query individual rules. A separate table adds schema complexity with zero query benefit. |
| Serialization library | Newtonsoft.Json vs System.Text.Json | **System.Text.Json** — already in the .NET stack, no new dependencies, used elsewhere in the solution. |
| Ctor extension | Ctor overload vs optional params on existing | **Optional params on existing ctors** — backward-compatible with all call sites. Verdict params trail after existing ones, defaulting to null. |
| Worker reconstruction | Switch on all fields vs pattern match on Decision | **Single null-coalescing branch**: if `entry.Decision` is non-null, deserialize full verdict; otherwise, fall back to the current synthetic path. |

## Data Flow

```
Before:
  UseCase ──→ Outbox(TriggerRule only) ──→ Worker(synthesizes fake verdict) ──→ Dispatcher

After:
  UseCase ──→ Outbox(TriggerRule + Explanation + Decision + TargetUserId + RuleResults-JSON)
                  │
                  ▼
            Worker(reads persisted fields, deserializes RuleResults,
                   builds InterruptionVerdict from real data)
                  │
                  ▼
            Dispatcher(forward-compatible: adds audit fields to payload)
```

## JSON Schema — RuleResults Column

```json
[
  {
    "ruleName": "vip_sender",
    "matched": true,
    "score": 9.0,
    "confidence": 0.95,
    "reason": "VIP sender detected"
  }
]
```

Serialized from `EvaluationReport.Results` via `JsonSerializer.Serialize()`, deserialized to `List<RuleResult>` then wrapped in `new EvaluationReport(list)`. Empty report serializes as `[]`.

## File Changes

### Domain

| File | Action | Description | Est. |
|---|---|---|---|
| `src/Aura.Domain/WorkItems/NotificationOutboxEntry.cs` | Modify | Add 4 nullable fields (`Explanation`, `Decision`, `TargetUserId`, `RuleResults`). Extend both constructors with optional verdict params after `triggerRule`. | 20 lines |

### Infrastructure

| File | Action | Description | Est. |
|---|---|---|---|
| `src/Aura.Infrastructure/Adapters/Notifications/SqliteNotificationOutboxStore.cs` | Modify | `InitializeSchema`: add 4 nullable TEXT columns via `ALTER TABLE ... ADD COLUMN IF NOT EXISTS` (SQLite ignores duplicates). `EnqueueAsync`: add 4 parameters, write `DBNull` when null. `GetPendingAsync` SELECT: add 4 columns. `ReadEntryFromReader`: read via `IsDBNull`, pass to ctor. | 40 lines |

### Application

| File | Action | Description | Est. |
|---|---|---|---|
| `src/Aura.Application/UseCases/ConnectorExecution/ExecuteConnectorUseCase.cs` | Modify | In `EvaluateAndEnqueueAsync`: serialize `verdict.Report` to JSON, pass `verdict.Explanation`, `verdict.Decision.ToString()`, `verdict.TargetUserId`, and JSON string to the ctor. | 10 lines |

### Api

| File | Action | Description | Est. |
|---|---|---|---|
| `src/Aura.Api/Workers/WorkItemNotificationWorker.cs` | Modify | Add `System.Text.Json` using. In the loop: if `entry.Decision` is non-null, parse enum, deserialize JSON, build `InterruptionVerdict`; else fall back to synthetic. | 25 lines |
| `src/Aura.Api/Adapters/SignalRWorkItemNotificationDispatcher.cs` | Modify | Add `Explanation`, `Decision`, `TargetUserId`, `RuleResults` to the anonymous dispatch payload (add-only, after existing fields). | 5 lines |

### Docs

| File | Action | Description | Est. |
|---|---|---|---|
| `docs/architecture/triage/00-overview.md` | Modify | Add audit trail section: verdict fields persisted in outbox. | 5 lines |
| `docs/architecture/triage/02-proactive-interruptions.md` | Modify | Mention persisted explanation chain in observability contract. | 5 lines |

### Tests

| File | Action | Description | Est. |
|---|---|---|---|
| `tests/Aura.UnitTests/Domain/NotificationOutboxEntryTests.cs` | Modify | Cover both constructors with null + full-verdict paths. | 30 lines |
| `tests/Aura.UnitTests/Infrastructure/SqliteNotificationOutboxStoreTests.cs` | Modify | Round-trip: write entry with full verdict, read back, verify. Backward-compat: read pre-migration NULL row. | 50 lines |
| `tests/Aura.UnitTests/Workers/WorkItemNotificationWorkerTests.cs` | Modify | Persisted verdict path, fallback path, null RuleResults handling. | 40 lines |
| `tests/Aura.UnitTests/Dispatchers/SignalRWorkItemNotificationDispatcherTests.cs` | Modify | Verify new fields in payload shape, existing fields unchanged. | 30 lines |

## Worker Flow — Before vs After

**Before:**
```csharp
var verdict = new InterruptionVerdict(
    InterruptionDecision.InterruptNow,       // hardcoded
    new EvaluationReport(Array.Empty<RuleResult>()),  // empty
    entry.TriggerRule);                       // only real field
```

**After:**
```csharp
InterruptionVerdict verdict;
if (entry.Decision is not null)
{
    var decision = Enum.Parse<InterruptionDecision>(entry.Decision);
    var report = entry.RuleResults is not null
        ? new EvaluationReport(JsonSerializer.Deserialize<List<RuleResult>>(entry.RuleResults)!)
        : new EvaluationReport([]);
    verdict = new InterruptionVerdict(decision, report, entry.TriggerRule,
        entry.Explanation, entry.TargetUserId);
}
else
{
    // Fallback for old rows without verdict fields
    verdict = new InterruptionVerdict(InterruptionDecision.InterruptNow,
        new EvaluationReport([]), entry.TriggerRule);
}
```

## Dispatcher Payload — Before vs After

**Before:** `{ Id, Title, SourceType, Priority, TriggerRule, Reason }`

**After:** `{ Id, Title, SourceType, Priority, TriggerRule, Reason, Explanation, Decision, TargetUserId, RuleResults }`

New fields are at the end of the anonymous object. Existing JavaScript consumers see the same shape they already handle — extra properties are ignored by SignalR JSON deserialization unless explicitly mapped.

## Testing Strategy

| Layer | What to Test | Approach |
|---|---|---|
| Unit — Domain | Ctor fields: null vs populated, guard clauses | Direct `NotificationOutboxEntry` construction |
| Unit — Serialization | `RuleResult` JSON round-trip: single, multiple, empty list | Serialize/deserialize in isolation |
| Unit — Store | Full-verdict write + read; NULL-column backward-compat; DBNull write | In-memory SQLite `SqliteNotificationOutboxStore` |
| Unit — Worker | Persisted verdict reconstruction; fallback for null fields; null RuleResults | Mock store returning controlled entries |
| Unit — Dispatcher | Payload shape: new fields present, existing fields unchanged | Capture the anonymous object sent to `SendAsync` via hub context mock |

## Migration / Rollout

No data migration. `InitializeSchema` uses `ALTER TABLE ... ADD COLUMN IF NOT EXISTS` — idempotent, safe to run on existing databases. Old rows have NULL in all new columns; the worker fallback path handles them transparently.

## Open Questions

None.
