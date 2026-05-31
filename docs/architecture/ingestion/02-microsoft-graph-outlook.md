# Microsoft Graph — Outlook Connector

> Placeholder. Este documento debe definir la ingestión de correos y señales relevantes desde Outlook vía Microsoft Graph.

## Quick path

1. Delimitar qué correos deben entrar al modelo canónico.
2. Definir filtros, categorías, prioridad y sincronización incremental.
3. Diseñar observabilidad, resiliencia y políticas de privacidad.

## Debe cubrir

- Contrato `IExternalConnector<OutlookEvent>`.
- Delta queries, checkpoints y reintentos.
- Reglas para adjuntos, remitentes y clasificación.
- Redacción/minimización de datos sensibles.
- Métricas por volumen, latencia y errores.

## Pendiente

- [ ] Completar criterios de selección y estrategia de sync para Outlook.
