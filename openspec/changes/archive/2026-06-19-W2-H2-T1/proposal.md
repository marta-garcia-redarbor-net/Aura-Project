# Proposal: Ingestion Checkpoint Store Contract

## Intent

Repeated ingestion runs must resume from the last processed point instead of re-pulling everything, so the orchestrator (W2-H2-T2/T3) can sync incrementally without duplicates. Today the only persistence seam nearby is `ISemanticOutboxRepository`, which serves semantic index sync — not ingestion checkpoints. This slice defines a narrow, provider-neutral checkpoint-store port so later orchestration work has a stable contract to build on.

## Scope

### In Scope
- An Application-layer checkpoint-store port (capability-named, provider-neutral).
- Checkpoint identity keyed by **connector + source + tenant**.
- Checkpoint value shape: **nullable cursor/deltaToken** + **nullable processed timestamp**.
- First-run semantics: with no previous checkpoint, callers start from a **bounded initial window covering today only**.

### Out of Scope
- Concrete persistence adapter (SQLite/EF) — deferred to a follow-up slice.
- The ingestion orchestrator loop, delta-sync, and deduplication logic (W2-H2-T2/T3).
- Canonical `NormalizedWorkItem` model and global idempotency policy.

## Capabilities

### New Capabilities
- `ingestion-checkpoint-store`: provider-neutral contract to store and retrieve an ingestion checkpoint by connector+source+tenant, holding a nullable cursor/deltaToken and nullable processed timestamp, with bounded today-only first-run behavior.

### Modified Capabilities
- None.

## Approach

Define a minimal port in `Aura.Application/Ports/` per the exploration recommendation. The port exposes get-by-identity and save operations over a canonical checkpoint model owned by Application. SDK/provider types never appear in the contract. The first-run "today only" window is a documented contract behavior, not a stored field. No adapter is implemented in this slice — the contract is what unblocks downstream work.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Application/Ports/` | New | Checkpoint-store port + canonical checkpoint DTO/value |
| `docs/architecture/ingestion/05-normalization-checkpoints.md` | Modified | Record the adopted identity and value shape |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Over-specified fields lock ingestion design early | Med | Keep value to cursor/deltaToken + timestamp only |
| Unstable identity causes later dedup drift | Med | Fix identity = connector + source + tenant now |
| No adapter hides real persistence semantics | Low | Contract-only slice; persistence is a tracked follow-up |

## Rollback Plan

The port and DTO are additive and unreferenced by runtime paths. Revert by deleting the new Application port/model files and reverting the doc edit; no migrations or data involved.

## Dependencies

- Clean Architecture layer boundaries (Domain/Application must not depend on Infrastructure).

## Success Criteria

- [ ] Checkpoint-store port exists in Application with connector+source+tenant identity.
- [ ] Value shape is nullable cursor/deltaToken + nullable processed timestamp.
- [ ] First-run-without-checkpoint contract documents the bounded today-only window.
- [ ] No provider/SDK types leak into the contract.
