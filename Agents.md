# Aura — Router de Agentes

> Aura es un asistente de ingeniería sobre **.NET 8 / ASP.NET Core** que reduce la carga mental del equipo mediante ingestión multi-fuente (Teams, Outlook, Calendar, GitHub), triáje cognitivo y revisión técnica híbrida con evidencia verificable.

**Regla de uso:** Lee sólo el fichero referenciado que necesitás. No cargues todo el árbol.

---

## Principios no negociables

| Principio | Regla |
|-----------|-------|
| Clean Architecture | `Domain` y `Application` no dependen de SDKs ni frameworks externos. |
| Ports & Adapters | Graph, GitHub, SonarQube, Dependabot y observabilidad entran por adaptadores. |
| SOLID | Cada agente/capacidad tiene una responsabilidad y contratos explícitos. |
| Observable por defecto | Todo caso de uso relevante emite trazas, métricas y logs correlacionados. |
| Security by Default | Least privilege, OWASP y MITRE desde diseño, no como parche. |
| TDD first | Tests y telemetría se entregan en el mismo cambio que el código. |

---

## Estructura de solución

```
src/   Aura.Api | Aura.Application | Aura.Domain | Aura.Infrastructure | Aura.Workers
tests/ Aura.UnitTests | Aura.IntegrationTests | Aura.E2E | Aura.ArchitectureTests
docs/  ai/ | architecture/ | skills/
```

Detalles de responsabilidades por capa → [`docs/ai/02-architecture-map.md`](./docs/ai/02-architecture-map.md)

---

## Índice de dominios funcionales

| Dominio | Guía de arquitectura |
|---------|----------------------|
| Ingestión Multi-Source | [`docs/architecture/ingestion/00-overview.md`](./docs/architecture/ingestion/00-overview.md) |
| Triáje y Carga Mental | [`docs/architecture/triage/00-overview.md`](./docs/architecture/triage/00-overview.md) |
| The Reviewer | [`docs/architecture/reviewer/00-overview.md`](./docs/architecture/reviewer/00-overview.md) |
| Observabilidad y Métricas | [`docs/architecture/observability/00-overview.md`](./docs/architecture/observability/00-overview.md) |
| Infraestructura de Calidad | [`docs/architecture/quality/00-overview.md`](./docs/architecture/quality/00-overview.md) |

---

## Guías operativas para agentes IA

| Tema | Fichero |
|------|---------|
| Visión general y flujos | [`docs/ai/00-overview.md`](./docs/ai/00-overview.md) |
| Reglas de operación IA | [`docs/ai/01-operating-rules.md`](./docs/ai/01-operating-rules.md) |
| Mapa arquitectónico | [`docs/ai/02-architecture-map.md`](./docs/ai/02-architecture-map.md) |
| Reglas de entrega | [`docs/ai/03-delivery-rules.md`](./docs/ai/03-delivery-rules.md) |
| Estrategia UI incremental | [`docs/ai/04-ui-incremental-strategy.md`](./docs/ai/04-ui-incremental-strategy.md) |
| Atomización de tareas | [`docs/ai/05-task-atomization.md`](./docs/ai/05-task-atomization.md) |
| Catálogo de skills | [`docs/ai/06-skill-catalog.md`](./docs/ai/06-skill-catalog.md) |

Skills activas y rutas exactas para delegación → [`.atl/skill-registry.md`](./.atl/skill-registry.md)

---

## Flujos principales

Resumen operativo y flujos end-to-end → [`docs/ai/00-overview.md`](./docs/ai/00-overview.md)

---

## Decisión arquitectónica final

Aura es una solución **modular, observable, testeable y orientada a dominio**. Los SDKs externos son detalles de infraestructura, NO el núcleo del sistema. Si mezclás Graph, GitHub, SonarQube, reglas de negocio y transporte en la misma capa, rompés mantenibilidad, testabilidad y performance. Este router existe para evitar ese caos desde el día uno.
