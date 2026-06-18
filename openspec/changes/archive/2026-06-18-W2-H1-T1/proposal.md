# Proposal: Refine Mandatory WorkItem Fields

## Intent

The `WorkItem` entity only enforces non-empty `Title` and `Source`. The backlog DoD requires every captured item to carry origin, priority, timestamp, and minimal metadata so downstream triage and multi-source ingestion are deterministic. This change pins the exact mandatory-field contract and normalization rules for the canonical `WorkItem`.

## Scope

### In Scope
- Add mandatory fields to `WorkItem`: `Priority`, `Metadata`, `externalId`, `sourceType`, `correlationId`, `capturedAtUtc`, `schemaVersion`.
- Enforce invariants in the Domain constructor; reject construction when required inputs are missing.
- Normalization rules: system-generated `correlationId` fallback; `capturedAtUtc` = source timestamp or ingestion timestamp; `schemaVersion` fixed to `v1`.
- `sourceType` validated against a closed set.
- Update `PluginRegistry`, `HelloKernelWorker`, and unit tests to the new shape.

### Out of Scope
- Persistence/storage of `WorkItem`.
- Real ingestion adapters (Teams, Slack, Outlook, Calendar, GitHub).
- Introducing a separate factory/value-object abstraction (deferred).

## Capabilities

### New Capabilities
- `work-item-contract`: canonical mandatory-field contract for `WorkItem` — required fields, `sourceType` closed set, `correlationId` generation fallback, `capturedAtUtc` resolution, and `schemaVersion = v1`.

### Modified Capabilities
- `plugin-kernel`: `WorkItem` construction invariants are extended to require the full mandatory contract; state-transition behavior is unchanged.

## Approach

Extend the Domain entity directly (exploration recommendation). Refine the `WorkItem` constructor to accept and validate the mandatory fields, generating defaults where the contract allows (`correlationId`, `capturedAtUtc` fallback, fixed `schemaVersion`). Stays entirely within Domain/Application/Workers — no SDKs or transport.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Domain/WorkItems/WorkItem.cs` | Modified | New mandatory fields, invariants, normalization |
| `src/Aura.Application/Kernel/PluginRegistry.cs` | Modified | Consumes new constructor shape |
| `src/Aura.Workers/HelloKernelWorker.cs` | Modified | Constructs `WorkItem` with full contract |
| `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` | Modified | Pin new invariants |
| `tests/Aura.UnitTests/Kernel/PluginRegistryTests.cs` | Modified | Adapt to new shape |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Required constructor params ripple into tests/bootstrap | High | Update all call sites in same change; lock with tests |
| `Metadata` shape underspecified | Med | Define minimal key-value contract in specs phase |
| `sourceType` set grows later | Med | Closed set with explicit initial values; extend via new change |

## Rollback Plan

Revert the change commit(s). `WorkItem` returns to the two-arg constructor and call sites recompile against the prior shape; no data migration involved.

## Dependencies

- None beyond the existing `plugin-kernel` capability.

## Success Criteria

- [ ] `WorkItem` rejects construction when any mandatory field is missing or `sourceType` is outside the closed set.
- [ ] `correlationId` is auto-generated when absent; `capturedAtUtc` falls back to ingestion time; `schemaVersion` equals `v1`.
- [ ] All unit tests pass with the new contract pinned.
