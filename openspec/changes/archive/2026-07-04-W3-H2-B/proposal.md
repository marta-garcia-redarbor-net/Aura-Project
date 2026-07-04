# Proposal: W3-H2-B - Audit Trail and Pipeline Propagation

## Intent

Persist the structured `InterruptionVerdict` through the outbox, worker, and dispatch pipeline so consumers can reconstruct why a notification was sent, not just that it was sent.

## Scope

### In Scope
- Extend `NotificationOutboxEntry` with verdict audit fields (Explanation, Decision, TargetUserId, RuleResult summary)
- Extend `SqliteNotificationOutboxStore` schema with nullable columns
- Persist full verdict in `EvaluateAndEnqueueAsync`
- Worker: deserialize and pass through persisted explanation (stop synthesizing fake verdicts)
- Dispatcher: add audit fields to SignalR payload (add-only, backward-compatible)
- Tests: round-trip persistence, backward-compatible read-back, dispatch shape
- Docs: triage architecture audit trail

### Out of Scope
- Scoring, policy, or engine changes (locked by W3-H2-A)
- T4 Teams preliminary scoring
- UI changes or new endpoints
- Re-triage from stored data

## Capabilities

### New Capabilities
- `notification-outbox-audit`: durable audit trail for interruption verdicts.

### Modified Capabilities
- `connector-execution`: `EvaluateAndEnqueueAsync` persists full verdict, not just TriggerRule.

## Approach

Four-stage seam extension:
1. **Domain**: nullable verdict fields on `NotificationOutboxEntry` + dedicated ctor overload.
2. **Infrastructure**: nullable TEXT columns in SQLite; update `EnqueueAsync` and `ReadEntryFromReader`.
3. **Use Case**: `EvaluateAndEnqueueAsync` persists Explanation, Decision, TargetUserId, TriggerRule, serialized RuleResult summary.
4. **Worker + Dispatcher**: worker builds verdict from persisted data; dispatcher adds audit fields add-only.

All new fields nullable. Old rows with NULL work without migration. No engine or policy code touched.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Domain/WorkItems/NotificationOutboxEntry.cs` | Modified | Add audit fields + ctor overload |
| `src/Aura.Infrastructure/Adapters/Notifications/SqliteNotificationOutboxStore.cs` | Modified | Schema columns, extended CRUD |
| `src/Aura.Application/UseCases/ConnectorExecution/ExecuteConnectorUseCase.cs` | Modified | Persist full verdict |
| `src/Aura.Api/Workers/WorkItemNotificationWorker.cs` | Modified | Deserialize persisted verdict |
| `src/Aura.Api/Adapters/SignalRWorkItemNotificationDispatcher.cs` | Modified | Add audit fields to payload |
| `tests/Aura.UnitTests/` | New/Modified | Persistence, read-back, dispatch |
| `docs/architecture/triage/` | Modified | Audit trail docs |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Schema additions break existing rows | Low | Nullable columns, NULL defaults |
| Worker reads old rows without new fields | Low | Null-coalesce; fallback to TriggerRule |
| Dispatch changes break frontend | Med | Add-only; existing fields unchanged |

## Rollback Plan

Drop new schema columns, revert `NotificationOutboxEntry` fields, restore use-case/worker/dispatcher. Old NULL-column rows remain valid.

## Dependencies

- W3-H2-A: `InterruptionVerdict`, `InterruptionDecision`, `EvaluationReport`, `RuleResult`

## Success Criteria

- [ ] Outbox persists and reads back full verdict explanation chain
- [ ] Worker passes through persisted explanation (no synthetic verdict)
- [ ] Dispatcher payload includes audit fields, existing consumers unaffected
- [ ] All existing tests pass; new tests cover round-trip, backward compat

## Size Estimate

~350-450 changed lines. Within 800-line review budget.
