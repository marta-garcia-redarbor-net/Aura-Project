# Design: Qdrant Semantic Index

## Technical Approach

Introduce the `semantic-index` capability across three layers — Domain value objects, Application ports, and an Infrastructure Qdrant adapter — keeping all SDK types confined to `Infrastructure/VectorStore/`. A background worker in `Aura.Workers` consumes outbox events from the canonical store, chunks content, strips PII, and writes derived semantic units through the Application port. Retrieval flows through a read port consumed by Triage and Reviewer use cases. Qdrant is never a source of truth; it is always rebuildable from canonical data.

## Architecture Decisions

| Decision | Choice | Alternatives | Rationale |
|----------|--------|--------------|-----------|
| Port naming | `ISemanticIndexWriter` / `ISemanticContextRetriever` | `IQdrantClient`, `IVectorStore` | Name by domain capability, not provider (aura-plugin-design rule). Enables swap to Milvus/Pinecone without Application changes. |
| Collection routing | `SemanticCollectionType` enum (`ProjectKnowledge`, `ActivityMemory`) in Domain `Enums/` | String-based collection names | Type-safe routing, compile-time validation, matches spec requirement for segregation by volatility. |
| Chunk model | `SemanticChunk` record in Domain with `CanonicalSourceId`, `DomainTags`, `Content`, `Timestamp` | Raw source events | Spec mandates filterable, derived units — not raw passthrough. Record is immutable by design. |
| Sync mechanism | Outbox table + `SemanticIndexSyncWorker` polling | Direct dual-write / domain events via MediatR | Outbox avoids dual-write inconsistency. MediatR events are viable later but require more infra (no event bus yet). Outbox is simplest correct approach for V1. |
| Chunking ownership | `ISemanticChunkExtractor` port in Application | Chunking inside adapter | Chunking is domain-aware logic (PII stripping, tag assignment) — belongs in Application, not Infrastructure. |
| Orphan handling | Retriever validates `CanonicalSourceId` existence; discards orphans silently | Hard-delete sweep | Per spec: graceful discard, no fatal errors. Sweep can be added later as maintenance job. |

## Data Flow

```
Canonical Store ──► Outbox Table ──► SemanticIndexSyncWorker
                                          │
                                   ISemanticChunkExtractor
                                   (chunk + strip PII)
                                          │
                                   ISemanticIndexWriter
                                          │
                                   QdrantSemanticIndexAdapter
                                     ┌────┴────┐
                              ProjectKnowledge  ActivityMemory
                                (Qdrant collections)

Reviewer/Triage UseCase ──► ISemanticContextRetriever
                                     │
                              QdrantSemanticContextAdapter
                                     │
                              Qdrant collections ──► validate canonical ID ──► return chunks
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Domain/SemanticIndex/ValueObjects/SemanticChunk.cs` | Create | Immutable record: CanonicalSourceId, Content, DomainTags, Collection, Timestamp |
| `src/Aura.Domain/SemanticIndex/Enums/SemanticCollectionType.cs` | Create | Enum: `ProjectKnowledge`, `ActivityMemory` |
| `src/Aura.Domain/SemanticIndex/ValueObjects/DomainTag.cs` | Create | Value object for filterable tags |
| `src/Aura.Application/Ports/ISemanticIndexWriter.cs` | Create | `WriteAsync(IReadOnlyList<SemanticChunk>, CancellationToken)` |
| `src/Aura.Application/Ports/ISemanticContextRetriever.cs` | Create | `RetrieveAsync(SemanticQuery, CancellationToken)` returning scored chunks |
| `src/Aura.Application/Ports/ISemanticChunkExtractor.cs` | Create | `ExtractAsync(CanonicalRecord, SemanticCollectionType) -> IReadOnlyList<SemanticChunk>` |
| `src/Aura.Application/Models/SemanticQuery.cs` | Create | Query DTO: text, collection filter, tag filters, top-K |
| `src/Aura.Application/Models/ScoredSemanticChunk.cs` | Create | Result DTO: chunk + relevance score |
| `src/Aura.Infrastructure/VectorStore/QdrantSemanticIndexAdapter.cs` | Create | Implements `ISemanticIndexWriter` using Qdrant .NET SDK |
| `src/Aura.Infrastructure/VectorStore/QdrantSemanticContextAdapter.cs` | Create | Implements `ISemanticContextRetriever` using Qdrant .NET SDK |
| `src/Aura.Infrastructure/VectorStore/QdrantOptions.cs` | Create | Config POCO: endpoint, API key, collection names |
| `src/Aura.Infrastructure/VectorStore/DependencyInjection.cs` | Create | `AddQdrantSemanticIndex(IServiceCollection, IConfiguration)` |
| `src/Aura.Infrastructure/Aura.Infrastructure.csproj` | Modify | Add `Qdrant.Client` NuGet package reference |
| `src/Aura.Workers/SemanticIndexSyncWorker.cs` | Create | `BackgroundService` polling outbox, calling extractor + writer |
| `src/Aura.Workers/Program.cs` | Modify | Register `SemanticIndexSyncWorker` and Qdrant DI |

## Interfaces / Contracts

```csharp
// Domain
public record SemanticChunk(
    Guid Id,
    string CanonicalSourceId,
    string Content,
    SemanticCollectionType Collection,
    IReadOnlyList<DomainTag> Tags,
    DateTimeOffset CreatedAt);

public enum SemanticCollectionType { ProjectKnowledge, ActivityMemory }

// Application Ports
public interface ISemanticIndexWriter
{
    Task WriteAsync(IReadOnlyList<SemanticChunk> chunks, CancellationToken ct);
    Task DeleteByCanonicalIdAsync(string canonicalSourceId, CancellationToken ct);
}

public interface ISemanticContextRetriever
{
    Task<IReadOnlyList<ScoredSemanticChunk>> RetrieveAsync(SemanticQuery query, CancellationToken ct);
}

public interface ISemanticChunkExtractor
{
    Task<IReadOnlyList<SemanticChunk>> ExtractAsync(
        string canonicalSourceId, string content,
        SemanticCollectionType target, CancellationToken ct);
}
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | `SemanticChunk` invariants, `DomainTag` equality, `SemanticCollectionType` routing | xUnit, no mocks needed — pure domain logic |
| Unit | `ISemanticChunkExtractor` default impl (chunking, PII strip) | xUnit + in-memory data; test chunk size, tag assignment, PII removal |
| Unit | `SemanticIndexSyncWorker` orchestration | xUnit + mock ports (NSubstitute); verify extract→write flow, error handling |
| Integration | `QdrantSemanticIndexAdapter` write/read roundtrip | Testcontainers with Qdrant Docker image; verify upsert, search, delete |
| Architecture | Domain has no Qdrant references; Application has no Infrastructure refs | NetArchTest or reflection-based checks in `Aura.ArchitectureTests` |

## Migration / Rollout

No data migration required. Qdrant collections are created on first write (or via a startup health check). Rollback = disable adapter in DI + stop sync worker. Canonical data is unaffected.

## Open Questions

- [ ] Embedding model choice: use an external embedding API (OpenAI, Azure OpenAI) or a local model? This affects `ISemanticChunkExtractor` — it needs an embedding step before writing to Qdrant. V1 should define an `IEmbeddingProvider` port to defer this decision.
- [ ] Outbox table schema: does the project plan to introduce a shared outbox (e.g., for other derived stores) or a semantic-index-specific one? Recommend semantic-specific for V1, generalizable later.
- [x] Mocking library choice resolved: use NSubstitute for worker and port orchestration tests in V1.
