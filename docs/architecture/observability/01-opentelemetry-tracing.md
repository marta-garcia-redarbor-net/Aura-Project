# Observabilidad — OpenTelemetry y trazas distribuidas

> Placeholder. Este documento debe definir la instrumentación con OpenTelemetry para APIs, workers y conectores.

## Quick path

1. Listar `ActivitySource` y spans críticos.
2. Diseñar propagación de contexto y correlation IDs.
3. Definir exporters, sampling y convenciones de tags.

## Debe cubrir

- Instrumentación de `Aura.Api`, `Aura.Workers` y módulos funcionales.
- Context propagation entre jobs, webhooks y proveedores externos.
- Tags para tenant, user, feature y proveedor.
- Manejo de errores, retries y enriched spans.
- Validación mediante tests/entornos observables.

## Pendiente

- [ ] Completar estrategia de tracing y estándares de instrumentación.
