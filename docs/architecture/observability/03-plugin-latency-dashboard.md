# Observabilidad — Dashboard de latencia por plugin

> Placeholder. Este documento debe definir cómo observar latencia, percentiles y tasa de error por plugin o proveedor.

## Quick path

1. Identificar plugins/proveedores medidos.
2. Diseñar métricas p50/p95/p99, throughput y error rate.
3. Establecer dashboards y alertas accionables.

## Debe cubrir

- Métricas por plugin, operación y proveedor externo.
- Separación entre latencia propia y latencia dependiente.
- Retry count, timeout rate y saturation signals.
- Uso de histogramas y exemplars.
- Tests de telemetría y validación de cardinalidad.

## Pendiente

- [ ] Completar diseño del dashboard de latencia por plugin.
