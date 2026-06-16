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

## Debe cubrir

- Modelo canónico de `NormalizedWorkItem`.
- Estrategia webhook + reconciliación programada.
- Checkpoints, delta sync e idempotencia.
- Bounded concurrency, retry y circuit breaker.
- Métricas por conector y correlación end-to-end.

## Pendiente

- [ ] Completar arquitectura detallada de la capa de ingestión.
- [ ] Agregar adaptadores para Graph (Teams, Outlook, Calendar) bajo `Ingestion/Graph/`.
- [ ] Agregar adaptador para GitHub bajo `Ingestion/GitHub/`.
