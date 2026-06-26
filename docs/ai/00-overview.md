# Aura — Overview and flows

Aura reduces engineering team cognitive load by ingesting signals from multiple sources (Teams, Outlook, Calendar, GitHub), prioritizing them based on the user's current context, and running hybrid technical review with verifiable evidence.

---

## End-to-end flows

### Login and delegated authentication

```text
User opens Aura.UI
  → signs in with Microsoft Entra ID
  → receives delegated user token
  → token contains oid
  → Aura.UI forwards bearer token to Aura.Api and SignalR
  → Aura.Api validates JWT
  → Aura uses oid as the current user identity
  → Graph-backed features call Microsoft Graph with delegated user permissions
```

### Ingestion → Triage

```text
External connector
  → normalizes payload to canonical WorkItem
  → attaches preliminary source signals
  → global triage policy evaluates impact, deadline, dependencies, and risk
  → emits interrupt, queue, or defer decision
```

### Calendar → User awareness

```text
Graph Calendar data for the signed-in user
  → Aura fetches delegated calendar events
  → dashboard shows upcoming meetings
  → worker checks alert windows
  → SignalR pushes meeting alerts to the UI
```

### PR → Intelligent review

```text
GitHub PR enters Aura
  → SonarQube: code smells, bugs, coverage gates
  → Dependabot: vulnerable libraries, severity, fix availability
  → OWASP/MITRE: security risk analysis for the change context
  → SemanticValidator: diff + tests + acceptance criteria
  → ReviewDecisionEngine:
      Approved | Changes Requested | Security Escalation | Needs Human Review
```

### Cross-cutting observability

```text
Each relevant step emits Activity (OpenTelemetry)
  → correlation id per request/job/workflow
  → metrics: tokens, cost, p50/p95/p99 latency, retry count
  → dashboards: cost per feature/plugin/model, latency per provider
```

---

## High-level decisions

| Area | Decision |
|------|----------|
| Authentication | Delegated auth with Microsoft Entra ID |
| User identity | Use token `oid` as the Aura user identifier |
| Token lifecycle | Persist the MSAL token cache in SQLite, attempt silent renewal first, require re-auth if renewal fails |
| Graph integration | Use delegated user tokens, not app-only credentials |
| App Registration config | `ClientId` and `TenantId` come from the Aura Entra ID App Registration |
| Client secret | Not required for the delegated Graph flow |
| Deployment scope | Local Docker-based deployment first |
| Host topology | Keep `Aura.UI`, `Aura.Api`, and `Aura.Workers` separated |

Aura documentation should present this as a single target architecture. Do not document app-only Graph auth, mock/default identity, or a merged single-host deployment as competing target states.

## Next files

- AI operating rules → [`01-operating-rules.md`](./01-operating-rules.md)
- Layer map and contracts → [`02-architecture-map.md`](./02-architecture-map.md)
- Authentication architecture → [`../architecture/auth/00-overview.md`](../architecture/auth/00-overview.md)
- Deployment architecture → [`../architecture/deployment/00-overview.md`](../architecture/deployment/00-overview.md)
