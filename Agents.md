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

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

When the user types `/graphify`, invoke the `skill` tool with `skill: "graphify"` before doing anything else.

Rules:

### When graphify is the right tool
Use `graphify query/path/explain` when the question involves **known symbol names**:
- Relationships between two concrete classes → `graphify path "<A>" "<B>"`
- Methods, fields, and interfaces of a specific class → `graphify query "<ClassName> dependencies"`
- Architecture concepts already indexed → `graphify explain "<concept>"`
- Broad navigation → `graphify-out/wiki/index.md` if it exists

### When to fall back to grep/glob instead
Skip graphify and use grep/glob/Read directly when:
- The question is **semantically broad** (e.g. "what interfaces exist in Ports/") — graphify will match docs instead of code
- The relationship involves **dependency injection resolution** — the graph does not model the DI container, so `path A → B` via an injected interface will return "no path found"
- The query requires **discovering unknown symbols** (e.g. "find all classes that implement X") — grep is more reliable
- graphify returns results only from `openspec/` or `docs/` when code was expected — that is a signal to fall back immediately

### Fallback protocol
1. Run graphify first for concrete-symbol queries.
2. If the result is empty, only contains doc nodes (`openspec/`, `docs/`), or "No path found" — **stop and fall back to grep/glob**.
3. Never retry the same graphify query with rephrasing more than once.
4. Combine both: use graphify for the relationship skeleton, grep to fill in line numbers and confirm.

### Other rules
- Dirty graphify-out/ files are expected after hooks or incremental updates; dirty graph files are not a reason to skip graphify. Only skip graphify if the task is about stale or incorrect graph output, or the user explicitly says not to use it.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
