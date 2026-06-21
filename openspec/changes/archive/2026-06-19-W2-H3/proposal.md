# Proposal: W2-H3 — Teams Plugin Mapping and Work Item Persistence

## Intent

The Teams connector adapter is a stub: it logs the request, returns a fixed count, and never parses a Teams payload or builds a `WorkItem`. `connector-execution` explicitly defers Teams field mapping to W2-H3. Triage cannot start without real, canonical work items captured from Teams and durably stored. This change closes that gap in two committed parts toward one outcome: Teams messages become persisted canonical `WorkItem`s.

## Scope

### In Scope
- **Part 1 — Teams field mapping**: map Teams payloads (mock fixtures allowed) to the canonical `WorkItem` shape inside the Infrastructure adapter (anti-corruption layer).
- Part 1 tolerance: accept incomplete/partially invalid Teams payloads without aborting the whole batch when a usable item can still be produced.
- Part 1 traceability: preserve degraded, defaulted, or missing source values in `WorkItem.Metadata`.
- **Part 2 — persistence (immediate committed follow-up)**: persist the mapped `WorkItem` via a provider-neutral port + Infrastructure store.

### Out of Scope
- Outlook, Calendar, GitHub connectors (W2-H4+).
- Checkpoint/delta sync, idempotency, retry, scheduling (W2-H2-T3, separate backlog).
- Changing the provider-neutral `IConnectorAdapter` contract or `ConnectorExecutionResult` shape.

## Capabilities

### New Capabilities
- `teams-connector-mapping`: Part 1 — Teams payload → canonical `WorkItem` mapping rules, partial-payload tolerance, and `Metadata` preservation of degraded values.
- `work-item-persistence`: Part 2 — provider-neutral port to persist a mapped `WorkItem`, with an Infrastructure store implementation.

### Modified Capabilities
- None. `connector-execution` requirements are unchanged; only its W2-H3 scope-deferral is now fulfilled.

## Approach

Adapter-local anti-corruption layer (exploration recommendation). Keep Teams DTOs/fixtures and mapping in `Aura.Infrastructure`; translate to canonical `WorkItem` there. Define a provider-neutral persistence port in `Aura.Application`, implemented in Infrastructure. No Graph/SDK types escape Infrastructure.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsConnectorAdapter.cs` | Modified | Stub becomes mapping anti-corruption layer |
| `src/Aura.Application/Ports/` | New | WorkItem persistence port |
| `src/Aura.Infrastructure/Adapters/.../DependencyInjection.cs` | Modified | Register mapper/store |
| `tests/Aura.UnitTests`, `tests/Aura.ArchitectureTests` | New/Modified | Mapping, error, persistence, boundary tests |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Mapping/persistence API mismatch vs `ConnectorExecutionResult` | Med | Keep result contract intact; persist as a separate port step |
| Mock payloads hide real Graph shape | Med | Version fixtures; align with Graph docs |
| SDK type leaks above Infrastructure | Low | Architecture tests fail fast |

## Rollback Plan

Revert the W2-H3 commits. The adapter returns to the logging stub; the new persistence port/store are removed. No schema or external state is migrated, so revert is clean.

## Dependencies

- Canonical `WorkItem` model and `WorkItemSourceType.TeamsMessage` (already present).

## Success Criteria

- [ ] Teams fixtures map to canonical `WorkItem`s with priority and context.
- [ ] Partial/invalid payloads degrade gracefully; degraded values recorded in `Metadata`.
- [ ] Mapped `WorkItem`s are persisted via the new port (Part 2 delivered, not deferred).
- [ ] Mapping, error, and architecture-boundary tests pass.
