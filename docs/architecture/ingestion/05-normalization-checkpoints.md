# Ingestión — Normalización, checkpoints e idempotencia

> Placeholder. Este documento debe fijar el contrato de normalización, persistencia de checkpoints y estrategia de deduplicación.

## Quick path

1. Definir el modelo canónico y sus invariantes.
2. Diseñar almacenamiento de checkpoints por fuente.
3. Establecer reglas de idempotencia y re-procesamiento seguro.

## Debe cubrir

- Estructura de `NormalizedWorkItem`.
- Claves de deduplicación y versionado de eventos.
- Persistencia de delta tokens, cursors o watermarks.
- Reprocesamiento, backfill y recuperación ante fallas.
- Métricas de duplicados, lag y throughput.

## Pendiente

- [ ] Completar contrato canónico y política global de idempotencia.
