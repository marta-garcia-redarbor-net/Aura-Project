## Exploration: W2-H3 — Teams plugin mapping

### Current State
The repo already has a stub Teams connector adapter in `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsConnectorAdapter.cs`. It only logs the request, returns a fixed item count, and does not parse any Teams payload or build `WorkItem` instances. The Application port is still provider-neutral (`IConnectorAdapter` returns `ConnectorExecutionResult`), while the canonical work item model already exists in `Aura.Domain.WorkItems` with `WorkItemSourceType.TeamsMessage` available.

The architecture docs for Teams are still placeholders: `docs/architecture/ingestion/01-microsoft-graph-teams.md` says Teams should map events to `NormalizedWorkItem`, and `docs/architecture/ingestion/00-overview.md` still lists Graph adapters as pending. Story planning/backlog for W2-H3 is explicitly about Teams DTOs/mock payloads, mapping payloads to `WorkItem`, and adding mapping/error tests.

### Affected Areas
- `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsConnectorAdapter.cs` — current stub must become the Teams-specific anti-corruption layer.
- `src/Aura.Infrastructure/Adapters/Connectors/DependencyInjection.cs` — may need registration for mapping helpers/fixtures if they are extracted.
- `src/Aura.Domain/WorkItems/WorkItem.cs` and `WorkItemSourceType.cs` — define the canonical target model and Teams source type.
- `tests/Aura.UnitTests/Infrastructure/InfrastructureDependencyInjectionTests.cs` — verifies the Teams adapter is registered.
- `tests/Aura.UnitTests/Ingestion/ExecuteConnectorUseCaseTests.cs` — existing connector contract tests may need new cases if adapter behavior changes.
- `tests/Aura.ArchitectureTests/ConnectorExecutionArchitectureTests.cs` — guards against Microsoft Graph leaking into Application.
- `docs/architecture/ingestion/01-microsoft-graph-teams.md` — placeholder design text that should be aligned with implementation.

### Approaches
1. **Adapter-local anti-corruption layer** — keep Teams DTOs/mock payloads and the mapping logic inside `Aura.Infrastructure`, and have the adapter translate them into `WorkItem`-shaped internal data before producing the execution result.
   - Pros: matches Clean Architecture, keeps Graph-specific types out of Application, smallest change surface, easiest to stage with fixtures and mapping tests.
   - Cons: mapping logic is Teams-specific and may be duplicated later for Outlook.
   - Effort: Medium

2. **Shared Application mapping contract** — introduce an Application-level factory/mapper interface for normalized work items and let Teams DTOs flow through that abstraction.
   - Pros: explicit reusable contract, easier to share with Outlook/Calendar later.
   - Cons: premature abstraction for one provider, more files/wiring, higher risk of over-scoping W2-H3.
   - Effort: Medium/High

### Recommendation
Use the adapter-local anti-corruption layer. The current slice is still Teams-only, the port is already provider-neutral, and the codebase has no shared normalized ingestion mapper yet. Keep Teams payload DTOs and fixtures in Infrastructure, map them to canonical `WorkItem` data there, and prove the behavior with focused unit tests.

### Risks
- The story says “map to `WorkItem`”, but the live connector contract returns `ConnectorExecutionResult`, so the slice can drift into an API mismatch if the consumer expects actual work-item emission.
- Mock payloads can become too synthetic and hide real Graph shape mismatches.
- If Teams SDK or Graph DTOs leak above Infrastructure, architecture tests should fail immediately.

### Ready for Proposal
Yes — the implementation boundary is clear enough. The next phase should define the exact Teams mock payload shape, the mapping rules to `WorkItem`, and the error cases to cover in tests.
