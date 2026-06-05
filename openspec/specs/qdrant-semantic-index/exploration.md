## Exploration: qdrant-semantic-index

### Current State
Aura currently processes ingestion, triage, and review tasks based on explicit rules and structured data. However, resolving context (e.g., finding relevant past PRs, matching code to architectural guidelines, or retrieving historical triage decisions) requires semantic search. Currently, there is no semantic index in place, and relying solely on relational queries or LLM context windows is unscalable and expensive.

### Affected Areas
- `Aura.Application` — Needs new domain-centric ports (e.g., `ISemanticContextRetriever` and `ISemanticIndexer`) to manage and query context without knowing about Qdrant.
- `Aura.Infrastructure/VectorStore` — Needs the `Qdrant` implementation of these ports (adapter pattern).
- `Aura.Workers` — Needs background jobs to listen for canonical DB changes (Inbox/Outbox) and update the Qdrant index asynchronously.
- `Aura.Reviewer` & `Aura.Triage` — Will consume the retriever port to augment review evidence and triage decisions.

### Approaches (Collection Structure)
1. **Single Unified Collection (`Aura.SemanticContext`)** — All chunks (docs, rules, PRs, triage events) live in one collection, differentiated by Qdrant payload filtering.
   - Pros: Simple to deploy and query; cross-domain retrieval is easy.
   - Cons: Index can become bloated; embedding models must be the same for everything.
   - Effort: Low

2. **Domain-Based Collections (`ProjectKnowledge` vs `ActivityMemory`)** — Split static/slow-moving knowledge (rules, PRs, docs) from fast-moving user activity (triage interruptions, feedback).
   - Pros: Better lifecycle management; allows different embedding models in the future; isolates noisy data.
   - Cons: Requires multi-collection querying if context spans both domains.
   - Effort: Medium

### Approaches (Source of Truth)
1. **Qdrant as Primary Store** — Storing full objects inside Qdrant payloads.
   - Pros: No need to join with SQL/Structured DB.
   - Cons: Violates `aura-review-evidence` rule (Qdrant should only be optional semantic support). Sync issues.

2. **Qdrant as Derived Index (Recommended)** — Qdrant only stores vectors, chunk text, and a `SourceId` pointing to the canonical DB.
   - Pros: Complies with Aura architecture; robust against vector DB wipes.
   - Cons: Requires an eventual consistency/sync mechanism (e.g., Worker publishing to Qdrant).

### Recommendation
We recommend **Domain-Based Collections** (e.g., `ProjectKnowledge` and `ActivityMemory`) to separate static rules from fast-moving events, and treating Qdrant strictly as a **Derived Index** (Approach 2). 

**Minimal Metadata Shape (Payload):**
- `SourceId`: Reference to the canonical relational entity.
- `SourceType`: `Rule`, `UserStory`, `PR`, `Doc`, `TriageEvent`.
- `ChunkIndex`: Position in the document.
- `Tags`: For strict filtering (e.g., `["domain:reviewer", "tenant:default"]`).

### Risks
- **Desynchronization:** The relational database and Qdrant could drift if the sync worker fails. Must use Outbox pattern.
- **Over-reliance:** The vector database might accidentally become a source of truth if developers query it for business logic instead of using it as a semantic heuristic.
- **Security:** Embedding sensitive Teams/PR data into Qdrant requires careful payload filtering to avoid leaking context.

### Ready for Proposal
Yes. The architectural boundaries are clear (Qdrant in Infrastructure, Port in Application, Derived Index pattern via Workers). We can move to `sdd-propose` to define the interfaces and the exact sync workflow.