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
| Microsoft Graph (Teams, Outlook, Calendar) | Managed Identity / Entra ID | `Infrastructure/Graph/` |
| GitHub PRs | GitHub App | `Infrastructure/GitHub/` |
| SonarQube | API Key (secret manager) | `Infrastructure/SonarQube/` |
| Dependabot | GitHub App | `Infrastructure/Dependabot/` |
| Qdrant (vector store) | API Key (secret manager) | `Infrastructure/VectorStore/` |
