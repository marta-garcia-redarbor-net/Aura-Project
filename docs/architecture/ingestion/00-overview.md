# Ingestion — Overview

Aura ingestion converts external events into normalized, observable, and idempotent work units.

## Quick path

1. Ingest source events from supported connectors.
2. Normalize payloads into canonical `WorkItem` entities.
3. Attach source-specific signals and preliminary score metadata.
4. Emit canonical work to Application-level triage.

## Adaptadores implementados

Los siguientes adaptadores viven bajo `src/Aura.Infrastructure/Adapters/Ingestion/`:

| Adaptador | Path | Puerto que implementa |
|-----------|------|-----------------------|
| MEAI Embedding | `Ingestion/Embedding/` | `IEmbeddingProvider` |
| Qdrant SemanticIndex | `Ingestion/SemanticIndex/` | `ISemanticIndexWriter`, `ISemanticContextRetriever` |
| SQLite SemanticOutbox | `Ingestion/SemanticOutbox/` | `ISemanticOutboxRepository` |

El health check de Qdrant (`QdrantHealthCheck`) vive junto a su adaptador en `Ingestion/SemanticIndex/`.

## Connector adapters implemented

The connector adapter layer lives under `src/Aura.Infrastructure/Adapters/Connectors/` and maps provider payloads to canonical `WorkItem` entities.

| Connector | Path | Mapping/classification behavior |
|-----------|------|---------------------------------|
| Teams | `Connectors/Teams/` | Maps Teams payloads to canonical `WorkItem` with metadata traceability and batch-skip tolerance |
| Outlook | `Connectors/Outlook/` | Maps Outlook email payloads to canonical `WorkItem` (`SourceType = OutlookEmail`) with multi-signal preliminary scoring (`Importance + subject + sender + body`) and deadline-cue metadata |

Outlook preliminary scoring is additive and deterministic:
- `score >= 6` => `Critical`
- `score >= 2` => `High`
- `score >= 0` => `Medium`
- `score < 0` => `Low`

All scoring inputs are written into `WorkItem.Metadata` keys under `outlook.scoring.*`, and deadline cues are recorded via `outlook.deadline.*` keys.

## Boundary contract with triage

Connector-level scoring is **preliminary only** and remains source-specific.

- Connectors normalize and pre-score.
- The global triage engine (`IInterruptionPolicyEngine`) owns final interrupt-vs-queue decisions.

No ingestion connector is allowed to own the final interruption decision.

## Coverage

- Canonical `WorkItem` normalization model
- Webhook + reconciliation strategy
- Checkpoints, delta sync, and idempotency
- Bounded concurrency, retry, and circuit breaker behavior
- Connector-level observability and end-to-end correlation

## Open follow-ups

- [ ] Add remaining Graph Calendar adapters under `Ingestion/Graph/`.
- [ ] Add GitHub ingestion adapter under `Ingestion/GitHub/`.
