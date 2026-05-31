# Microsoft Graph — Teams Connector

> Placeholder. Este documento debe definir el conector de Teams para mensajes, menciones y actividad de canales usando Microsoft Graph.

## Quick path

1. Listar eventos de Teams que Aura necesita ingerir.
2. Diseñar permisos Graph, webhooks y fallback de polling.
3. Mapear eventos a `NormalizedWorkItem` con checkpoints.

## Debe cubrir

- Contrato `IExternalConnector<TeamsEvent>`.
- Permisos mínimos y estrategia de autenticación.
- Límites de rate, retries con jitter y manejo de throttling.
- Normalización, deduplicación y correlación.
- Tests de integración con sandbox/mocks.

## Pendiente

- [ ] Completar endpoints, permisos y estrategia incremental para Teams.
