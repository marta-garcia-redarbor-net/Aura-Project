# Proposal: qdrant-semantic-index

## Intent
Formalize Qdrant as Aura’s semantic index capability, establishing explicit boundaries, intended data classes, collection strategy, and V1 scope. Qdrant is needed to enable the Reviewer and Triage agents to quickly retrieve relevant contextual evidence (past decisions, project knowledge, and activity memory).

## Scope

### In Scope
- Define Qdrant as a derived semantic index in `Aura.Infrastructure`, behind `Aura.Application` ports.
- Establish two initial collections: `ProjectKnowledge` (slow-moving project evidence) and `ActivityMemory` (fast-moving triage context).
- Define semantic units as chunks/derived evidence, rather than raw whole source events.
- Implement application ports (e.g., `ISemanticContextRetriever`, `ISemanticIndexWriter`).
- Establish the data sync flow using a worker/outbox pattern so it remains a derived index.

### Out of Scope
- Docker/Infrastructure-as-code deployments for Qdrant (deferred to a later infrastructure phase).
- Complex hybrid search tuning or re-ranking (V1 focuses on basic semantic retrieval).
- Using Qdrant as the primary transactional source of truth (strictly prohibited).

## Capabilities

### New Capabilities
- `semantic-index`: The ability to write derived evidence chunks to Qdrant and retrieve context via application ports, segregated by domain collections (`ProjectKnowledge` and `ActivityMemory`).

### Modified Capabilities
- None

## Approach
Implement `semantic-index` in `Aura.Infrastructure` adapting `Aura.Application` interfaces (Ports and Adapters). We will define ports for writing derived semantic units (chunks of evidence) and reading/retrieving context. We will use a worker/outbox pattern to keep Qdrant updated as a derived store, avoiding dual-write problems with the main database.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Aura.Application/Ports` | New | Interfaces for semantic read/write operations |
| `Aura.Infrastructure/VectorStore` | New | Qdrant adapter implementation of the application ports |
| `Aura.Workers` | Modified | Background worker logic to sync derived evidence to Qdrant |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Vector index drift from source of truth | Medium | Enforce outbox pattern and strict derived-index-only policy |
| Qdrant leaking into domain logic | Low | Enforce Clean Architecture via `aura-clean-arch-guard` checks, keep all Qdrant SDK references in Infrastructure |

## Rollback Plan
Since Qdrant is purely a derived index, rollback involves disabling the Qdrant adapter in DI, stopping the sync worker, and removing Qdrant collections. The system will fall back to a degraded but functional state without semantic context, relying entirely on primary data stores.

## Dependencies
- Qdrant .NET SDK
- Existing outbox/worker infrastructure for async synchronization

## Success Criteria
- [ ] Application ports for semantic write/read are defined and agnostic of Qdrant.
- [ ] Infrastructure adapter implements Qdrant correctly.
- [ ] Semantic index is updated via outbox/worker, ensuring it remains derived.
- [ ] Tests verify that domain logic has zero dependencies on Qdrant.