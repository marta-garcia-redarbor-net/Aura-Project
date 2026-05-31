# Ingestión — Visión general

> Placeholder de arquitectura para Aura. Este documento debe detallar cómo la capa de ingestión convierte eventos externos en trabajo normalizado, observable e idempotente.

## Quick path

1. Definir fuentes soportadas y eventos de entrada.
2. Diseñar contratos en `Application` y adaptadores en `Infrastructure`.
3. Asegurar idempotencia, resiliencia, telemetría y testing.

## Debe cubrir

- Modelo canónico de `NormalizedWorkItem`.
- Estrategia webhook + reconciliación programada.
- Checkpoints, delta sync e idempotencia.
- Bounded concurrency, retry y circuit breaker.
- Métricas por conector y correlación end-to-end.

## Pendiente

- [ ] Completar arquitectura detallada de la capa de ingestión.
