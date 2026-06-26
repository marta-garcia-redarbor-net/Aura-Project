# Aura architectural map

---

## Layers and responsibilities

| Project | Responsibility | Can reference |
|---------|----------------|---------------|
| `Aura.Domain` | Entities, value objects, business rules, domain events | Only its own abstractions |
| `Aura.Application` | Use cases, orchestration, DTOs, validations, policies, ports | Domain |
| `Aura.Infrastructure` | SDKs, persistence, messaging, observability, external connectors, token cache implementations | Application, Domain |
| `Aura.Api` | Endpoints, JWT validation, authorization, health checks, webhooks, SignalR host | Application |
| `Aura.UI` | User interaction, delegated sign-in entry point, bearer token forwarding to API/SignalR | Application contracts via API only |
| `Aura.Workers` | Recurring jobs, polling, background tasks | Application, Infrastructure |

**Golden rule:** dependencies flow inward. Domain knows nothing about outer layers. Infrastructure is the only layer that knows external SDKs and providers.

---

## First-class bounded contexts

| Bounded context | Core concern |
|-----------------|--------------|
| Auth | User sign-in, identity resolution, delegated token handling, token cache lifecycle |
| Ingestion | External-event normalization into canonical work units |
| Triage | Global attention and interrupt-vs-queue decisions |
| Calendar | Upcoming meetings and proactive alerting for the signed-in user |
| Reviewer | Technical review with evidence aggregation |
| Observability | Cross-cutting traces, metrics, logs, and cost visibility |

---

## Key contracts by domain

### Auth

```text
ICurrentUserService
ITokenAcquisitionService
IGraphClientFactory
```

Auth rules:

- `oid` from the validated Entra ID token is the authoritative Aura user identity.
- JWT validation belongs to the API boundary.
- Token cache persistence belongs to Infrastructure.
- The shared token cache is SQLite-backed and reused for MSAL silent renewal.
- If silent renewal fails, Aura must require re-authentication instead of falling back to app-only or mock identity.
- Graph access uses delegated user tokens, not app-only client credentials.
- `ClientId` and `TenantId` come from the Aura Entra ID App Registration; `ClientSecret` is not required for this delegated flow.

### Ingestion

```text
IExternalConnector<TSourceEvent>
IIngestionOrchestrator
INormalizedWorkItemFactory
ICheckpointStore
IWebhookSignatureValidator
IRateLimitPolicyProvider
```

### Triage

```text
IMorningSummaryScheduler
IMorningSummaryComposer
IAttentionBudgetCalculator
IInterruptionPolicyEngine
IFocusStateResolver
IPriorityScoringService
```

`IInterruptionPolicyEngine` is the authoritative global triage decision contract for final interrupt-vs-queue outcomes. If an alias `ITriageEngine` is introduced later, it must remain a naming alias and must not split decision authority across contracts.

### Reviewer

```text
IStaticAnalysisProvider
IDependencyRiskProvider
ISecurityAuditEngine
IUserStoryTraceabilityService
ISemanticRequirementValidator
IReviewDecisionEngine
```

---

## Recommended patterns by domain

| Domain | Recommended patterns |
|--------|----------------------|
| Auth | Boundary validation, token forwarding, adapter-based token cache persistence |
| Ingestion | Adapter, Anti-Corruption Layer, Strategy, Factory, Outbox/Inbox |
| Triage | State, Specification, Chain of Responsibility, policy-based design |
| Reviewer | Pipeline, Strategy, Evidence Aggregator |
| Observability | Decorator for instrumentation, module-level `ActivitySource` |

---

## ActivitySources (OpenTelemetry)

```text
Aura.Api
Aura.Workers
Aura.Auth
Aura.Ingestion
Aura.Triage
Aura.Reviewer
```

---

## External dependencies (adapters only)

| Dependency | Authentication model | Adapter location |
|------------|----------------------|------------------|
| Microsoft Graph (Teams, Outlook, Calendar) | Delegated Microsoft Entra ID user tokens | `Adapters/Connectors/Graph/` and related bounded-context adapters |
| GitHub PRs | GitHub App | `Adapters/Ingestion/GitHub/` |
| SonarQube | API key from secret manager | `Adapters/Reviewer/SonarQube/` |
| Dependabot | GitHub App | `Adapters/Reviewer/Dependabot/` |
| Qdrant (vector store) | API key or local container configuration | `Adapters/Ingestion/SemanticIndex/` |
| OpenAI / MEAI (embeddings) | API key from secret manager | `Adapters/Ingestion/Embedding/` |

### Authentication notes

| Topic | Rule |
|-------|------|
| App Registration values | `ClientId` and `TenantId` belong to the Aura Entra ID App Registration |
| User identity | The user identity comes from token claims, especially `oid` |
| Client secret | Not required for the delegated Graph flow |
| Renewal fallback | Silent renewal failure leads to re-authentication, not an alternate Graph credential model |
| UI → API | Use bearer token forwarding over HTTP/SignalR |
| Production-aligned behavior | API performs real JWT validation |

## Host topology guardrail

`Aura.UI`, `Aura.Api`, and `Aura.Workers` stay as separate hosts/processes in both architecture and deployment documentation. Do not document a merged single-host target as an equivalent end state.

---

## `Aura.Infrastructure` structure

Adapters are organized by functional bounded context first, then by technical implementation. Technology is a detail; the domain shape leads the structure.

```text
Aura.Infrastructure/
├── DependencyInjection.cs              ← only public entry point (`AddAuraInfrastructure`)
└── Adapters/
    ├── Connectors/                     ← provider-specific external integrations
    │   ├── Graph/                      ← delegated Graph client and source providers
    │   └── Calendar/                   ← calendar-specific providers and stores
    ├── Ingestion/                      ← semantic pipeline support
    │   ├── Embedding/                  ← MEAI + OpenAI
    │   ├── SemanticIndex/              ← Qdrant read/write + health check
    │   └── SemanticOutbox/             ← SQLite outbox
    └── Identity/                       ← JWT validation support, current-user adapter
```

**Promotion rule:** when `SemanticPipeline` (Embedding + SemanticIndex + SemanticOutbox) is consumed by a second bounded context (for example Reviewer or Triage), promote those subfolders from `Adapters/Ingestion/` to `Adapters/SemanticPipeline/` as a cross-cutting adapter area.

**New-adapter rule:** each new connector or infrastructure integration belongs under `Adapters/{BoundedContext}/{Technology}/` with its own internal `DependencyInjection.cs`.
