# Tasks: W3-H2-B — Audit Trail and Pipeline Propagation

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~260 |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

```text
Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Low
```

All changes are add-only, nullable, backward-compatible. ~260 lines well within the 800-line budget. Single PR is fine.

## Phase 1: Domain — Extend NotificationOutboxEntry (TDD)

- [x] **1.1 RED** — Write `NotificationOutboxEntryTests`: cover both ctors with null verdict path and full-verdict path; verify 4 new nullable fields (Explanation, Decision, TargetUserId, RuleResults)
- [x] **1.2 GREEN** — Add `public string? Explanation`, `Decision`, `TargetUserId`, `RuleResults` to `NotificationOutboxEntry.cs`; extend both constructors with optional verdict params defaulting to null (after `triggerRule`); add `using System.Text.Json` for serialization

## Phase 2: Infrastructure — SQLite Schema + CRUD (TDD)

- [x] **2.1 RED** — Write `SqliteNotificationOutboxStoreTests`: full-verdict round-trip (write + read), backward-compat (NULL column read), DBNull write when verdict absent
- [x] **2.2 GREEN** — In `SqliteNotificationOutboxStore.cs`: add 4 nullable TEXT columns via `ALTER TABLE ... ADD COLUMN IF NOT EXISTS` in `InitializeSchema`; extend `EnqueueAsync` params/INSERT with 4 columns (write `DBNull` when null); extend SELECT in `GetPendingAsync`; add `IsDBNull` checks in `ReadEntryFromReader`; pass to extended ctor

## Phase 3: Use Case — Persist Full Verdict (TDD)

- [x] **3.1 RED** — Write `ExecuteConnectorUseCase` tests: verify `InterruptNow` creates entry with full verdict fields populated; verify `Queue`/`Defer` do not create entries; verify `RuleResults` stores serialized JSON of `EvaluationReport`
- [x] **3.2 GREEN** — In `EvaluateAndEnqueueAsync`: serialize `verdict.Report.Results` via `JsonSerializer.Serialize()`; pass `verdict.Explanation`, `verdict.Decision.ToString()`, `verdict.TargetUserId`, JSON string to the verdict-aware ctor overload

## Phase 4: Worker + Dispatcher — Propagate Verdict (TDD)

- [x] **4.1 RED** — Write `WorkItemNotificationWorkerTests`: persisted verdict path (Decision non-null → deserialize), fallback path (null fields → synthetic), null RuleResults → empty report
- [x] **4.2 GREEN** — In `WorkItemNotificationWorker.cs`: add `System.Text.Json` using; replace synthetic verdict with null-coalescing branch: if `entry.Decision is not null`, parse enum, deserialize `RuleResults` JSON, build `InterruptionVerdict` from persisted fields; else fall back to current synthetic path
- [x] **4.3 RED** — Write `SignalRWorkItemNotificationDispatcherTests`: verify new fields (Explanation, Decision, TargetUserId, RuleResults) in payload; verify existing fields unchanged
- [x] **4.4 GREEN** — In `SignalRWorkItemNotificationDispatcher.cs`: add `Explanation`, `Decision`, `TargetUserId`, `RuleResults` to the anonymous dispatch payload (add-only, after existing fields); change visibility from `internal sealed` to `public sealed` to allow test instantiation

## Phase 5: Documentation

- [x] **5.1** — In `docs/architecture/triage/00-overview.md`: add audit trail section noting verdict fields persisted in outbox for explainability
- [x] **5.2** — In `docs/architecture/triage/02-proactive-interruptions.md`: mention persisted explanation chain under observability contract
