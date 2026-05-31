# GitHub SDK — Pull Requests Connector

> Placeholder. Este documento debe definir cómo Aura ingiere Pull Requests, comentarios y metadatos de revisión desde GitHub.

## Quick path

1. Definir eventos y metadatos de PR necesarios para Reviewer.
2. Diseñar integración con GitHub App y webhooks.
3. Mapear diffs y estado de checks al modelo canónico.

## Debe cubrir

- Contrato `IExternalConnector<GithubPullRequestEvent>`.
- Webhooks, reconciliación programada y manejo de rate limits.
- Estados de PR, reviewers, checks y labels.
- Evidencia necesaria para reviewer y trazabilidad.
- Observabilidad de latencia y errores por operación GitHub.

## Pendiente

- [ ] Completar diseño del conector de Pull Requests basado en GitHub SDK.
