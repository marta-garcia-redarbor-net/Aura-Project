# Mapa arquitectónico de Aura

---

## Capas y responsabilidades

| Proyecto | Responsabilidad | Puede referenciar |
|----------|-----------------|-------------------|
| `Aura.Domain` | Entidades, value objects, reglas de negocio, eventos de dominio. | Sólo abstracciones propias. |
| `Aura.Application` | Casos de uso, orquestación, DTOs, validaciones, policies. | Domain. |
| `Aura.Infrastructure` | SDKs, persistencia, mensajería, observabilidad, conectores externos. | Application, Domain. |
| `Aura.Api` | Endpoints, authN/authZ, health checks, webhooks. | Application. |
| `Aura.Workers` | Jobs recurrentes, colas, background tasks. | Application, Infrastructure. |

**Regla de oro:** La dependencia fluye hacia adentro. Domain no conoce a nadie. Infrastructure es el único que conoce el mundo externo.

---

## Contratos clave por dominio

### Ingestión

```
IExternalConnector<TSourceEvent>
IIngestionOrchestrator
INormalizedWorkItemFactory
ICheckpointStore
IWebhookSignatureValidator
IRateLimitPolicyProvider
```

### Triáje

```
IMorningSummaryScheduler
IMorningSummaryComposer
IAttentionBudgetCalculator
IInterruptionPolicyEngine
IFocusStateResolver
IPriorityScoringService
```

`IInterruptionPolicyEngine` is the authoritative global triage decision contract for
final interrupt-vs-queue outcomes. If an alias `ITriageEngine` is introduced later,
it should remain a naming alias and must not split decision authority across contracts.

### The Reviewer

```
IStaticAnalysisProvider
IDependencyRiskProvider
ISecurityAuditEngine
IUserStoryTraceabilityService
ISemanticRequirementValidator
IReviewDecisionEngine
```

---

## Patrones por dominio

| Dominio | Patrones recomendados |
|---------|-----------------------|
| Ingestión | Adapter, Anti-Corruption Layer, Strategy, Factory, Outbox/Inbox |
| Triáje | State, Specification, Chain of Responsibility, Policy-based design |
| Reviewer | Pipeline, Strategy, Evidence Aggregator |
| Observabilidad | Decorator (instrumentación), ActivitySource por módulo |

---

## ActivitySources (OpenTelemetry)

```
Aura.Api
Aura.Workers
Aura.Ingestion
Aura.Triage
Aura.Reviewer
```

---

## Dependencias externas (sólo via adaptadores)

| Dependencia | Autenticación | Conector |
|-------------|---------------|----------|
| Microsoft Graph (Teams, Outlook, Calendar) | Managed Identity / Entra ID | `Adapters/Ingestion/Graph/` |
| GitHub PRs | GitHub App | `Adapters/Ingestion/GitHub/` |
| SonarQube | API Key (secret manager) | `Adapters/Reviewer/SonarQube/` |
| Dependabot | GitHub App | `Adapters/Reviewer/Dependabot/` |
| Qdrant (vector store) | API Key (secret manager) | `Adapters/Ingestion/SemanticIndex/` |
| OpenAI / MEAI (embeddings) | API Key (secret manager) | `Adapters/Ingestion/Embedding/` |

---

## Estructura de Aura.Infrastructure

Los adaptadores se organizan por **bounded context funcional** primero, luego por implementación técnica.
La tecnología es un detalle; el dominio es la estructura.

```
Aura.Infrastructure/
├── DependencyInjection.cs              ← único entry point público (AddAuraInfrastructure)
└── Adapters/
    ├── Ingestion/                      ← fuentes de entrada y pipeline semántico
    │   ├── DependencyInjection.cs      ← AddIngestionAdapters() (internal)
    │   ├── Embedding/                  ← MEAI + OpenAI
    │   ├── SemanticIndex/              ← Qdrant read/write + health check
    │   └── SemanticOutbox/             ← SQLite outbox
    └── Identity/                       ← JWT Bearer + ICurrentUserService
```

**Regla de promoción:** cuando `SemanticPipeline` (Embedding + SemanticIndex + SemanticOutbox)
sea consumido por un segundo bounded context (Reviewer, Triage), promover esas subcarpetas
desde `Adapters/Ingestion/` a `Adapters/SemanticPipeline/` como carpeta transversal.

**Regla para nuevos adaptadores:** cada nuevo conector (Graph, GitHub, SonarQube...)
va bajo `Adapters/{BoundedContext}/{Technology}/` con su propio `DependencyInjection.cs` internal.
