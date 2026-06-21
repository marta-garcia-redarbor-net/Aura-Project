# Ingestión — Visión general

> Placeholder de arquitectura para Aura. Este documento debe detallar cómo la capa de ingestión convierte eventos externos en trabajo normalizado, observable e idempotente.

## Quick path

1. Definir fuentes soportadas y eventos de entrada.
2. Diseñar contratos en `Application` y adaptadores en `Infrastructure`.
3. Asegurar idempotencia, resiliencia, telemetría y testing.

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
| Outlook | `Connectors/Outlook/` | Maps Outlook email payloads to canonical `WorkItem` (`SourceType = OutlookEmail`) with multi-signal priority scoring (`Importance + subject + sender + body`) and deadline-cue metadata |

Outlook scoring is additive and deterministic:
- `score >= 6` => `Critical`
- `score >= 2` => `High`
- `score >= 0` => `Medium`
- `score < 0` => `Low`

All scoring inputs are written into `WorkItem.Metadata` keys under `outlook.scoring.*`, and deadline cues are recorded via `outlook.deadline.*` keys.

## Debe cubrir

- Modelo canónico de `NormalizedWorkItem`.
- Estrategia webhook + reconciliación programada.
- Checkpoints, delta sync e idempotencia.
- Bounded concurrency, retry y circuit breaker.
- Métricas por conector y correlación end-to-end.

## Pendiente

- [ ] Completar arquitectura detallada de la capa de ingestión.
- [ ] Agregar adaptadores restantes para Graph (Calendar) bajo `Ingestion/Graph/`.
- [ ] Agregar adaptador para GitHub bajo `Ingestion/GitHub/`.
