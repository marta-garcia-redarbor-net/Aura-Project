# Ingestion — Normalization, checkpoints, and idempotency

## Scope of this slice (W2-H2-T1)

This slice defines the **Application-layer checkpoint contract only**.
Persistence adapters and orchestrator behavior are implemented in follow-up slices.

## Ingestion checkpoint contract

### Identity

A checkpoint is uniquely identified by the tuple:

- `connector`
- `source`
- `tenant`

All three identity parts are required non-null, non-empty strings.

### Value shape

Checkpoint value contains exactly these two fields:

- `Cursor` (`string?`) — nullable cursor/delta token
- `ProcessedAt` (`DateTimeOffset?`) — nullable processed timestamp

No additional fields are part of this contract.

### First-run behavior

When `GetAsync(identity)` returns `null`, callers MUST treat this as first-run and bound
their initial fetch window to **UTC today only**:

- `start = UTC 00:00:00`
- `end = UtcNow`

This bound is **caller responsibility** and MUST NOT be stored as checkpoint data.

## Deferred items

- Concrete adapter implementation and persistence round-trip behavior are deferred to **W2-H2-T2/T3**.
- Canonical `NormalizedWorkItem` model and global idempotency policy are out of scope for this slice.

## Observability note

Runtime ingestion lag/throughput metrics are deferred in this slice because only the
checkpoint contract is introduced here (no runtime ingestion path or adapter yet).
