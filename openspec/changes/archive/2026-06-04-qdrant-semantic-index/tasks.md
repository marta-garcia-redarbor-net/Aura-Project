# Tasks: Qdrant Semantic Index

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 600–700 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 → PR 2 → PR 3 |
| Delivery strategy | ask-always |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Domain models + Application ports + domain/arch tests | PR 1 | Base: main; standalone contracts, no SDK deps |
| 2 | Qdrant adapters + DI + NSubstitute setup + integration tests | PR 2 | Base: PR 1 branch; introduces Qdrant SDK |
| 3 | SQLite outbox + sync worker + worker unit tests | PR 3 | Base: PR 2 branch; wires sync pipeline end-to-end |

## Phase 1: Domain + Application Contracts (TDD: RED→GREEN)

- [x] 1.1 Create `SemanticCollectionType` enum (`ProjectKnowledge`, `ActivityMemory`) in `src/Aura.Domain/SemanticIndex/Enums/` *(renamed from SemanticCollection, moved to Enums/)*
- [x] 1.2 Create `DomainTag` value object with equality in `src/Aura.Domain/SemanticIndex/ValueObjects/` *(moved to ValueObjects/)*
- [x] 1.3 Create `SemanticChunk` immutable record (`Id`, `CanonicalSourceId`, `Content`, `Collection`, `Tags`, `CreatedAt`) in `src/Aura.Domain/SemanticIndex/ValueObjects/` *(moved to ValueObjects/)*
- [x] 1.4 Create `ISemanticIndexWriter` port in `src/Aura.Application/Ports/` — `WriteAsync`, `DeleteByCanonicalIdAsync`
- [x] 1.5 Create `ISemanticContextRetriever` port — `RetrieveAsync(SemanticQuery) → IReadOnlyList<ScoredSemanticChunk>`
- [x] 1.6 Create `ISemanticChunkExtractor` port — domain-aware chunking + PII strip
- [x] 1.7 Create `IEmbeddingProvider` port — `GenerateEmbeddingAsync(string) → ReadOnlyMemory<float>`
- [x] 1.8 Create `SemanticQuery` and `ScoredSemanticChunk` DTOs in `src/Aura.Application/Models/`
- [x] 1.9 Unit tests: `SemanticChunk` invariants, `DomainTag` equality, collection routing in `tests/Aura.UnitTests/SemanticIndex/`
- [x] 1.10 Architecture tests: Domain has zero Qdrant/Infrastructure refs; Application has zero Infrastructure refs in `tests/Aura.ArchitectureTests/`

## Phase 2: Qdrant Adapters + DI

- [x] 2.1 Add `Qdrant.Client` NuGet to `src/Aura.Infrastructure/Aura.Infrastructure.csproj`
- [x] 2.2 Create `QdrantOptions` config POCO in `src/Aura.Infrastructure/VectorStore/`
- [x] 2.3 Create `QdrantSemanticIndexAdapter` implementing `ISemanticIndexWriter` in `src/Aura.Infrastructure/VectorStore/`
- [x] 2.4 Create `QdrantSemanticContextAdapter` implementing `ISemanticContextRetriever` with orphan-chunk discard in `src/Aura.Infrastructure/VectorStore/`
- [x] 2.5 Create `DependencyInjection.cs` with `AddQdrantSemanticIndex` extension in `src/Aura.Infrastructure/VectorStore/`
- [x] 2.6 Add `NSubstitute` NuGet to `tests/Aura.UnitTests/Aura.UnitTests.csproj`
- [x] 2.7 Integration tests: write/read roundtrip with Testcontainers + Qdrant image in `tests/Aura.IntegrationTests/VectorStore/`

## Phase 3: SQLite Outbox + Sync Worker (TDD: RED→GREEN)

- [x] 3.1 Create `SemanticOutboxEntry` entity + SQLite table schema in `src/Aura.Application/Models/` (entity) + `src/Aura.Infrastructure/Persistence/` (schema) *(moved entity to Application to avoid Architecture violation)*
- [x] 3.2 Create `ISemanticOutboxRepository` port in `src/Aura.Application/Ports/` + SQLite implementation in `src/Aura.Infrastructure/Persistence/`
- [x] 3.3 Create `SemanticIndexSyncWorker` (`BackgroundService`) polling outbox → extract → embed → write in `src/Aura.Workers/`
- [x] 3.4 Register `SemanticIndexSyncWorker` + Qdrant DI in `src/Aura.Workers/Program.cs`
- [x] 3.5 Unit tests: worker orchestration with NSubstitute mocks — verify extract→embed→write flow, error handling, orphan skip in `tests/Aura.UnitTests/Workers/`

## Phase 4: Corrective Patch (Post-Verification Fixes)

- [x] C.1 Create `EmbeddedSemanticChunk` DTO in `src/Aura.Application/Models/` — pairs chunk + pre-computed embedding
- [x] C.2 Change `ISemanticIndexWriter.WriteAsync` to accept `IReadOnlyList<EmbeddedSemanticChunk>` — writer only persists
- [x] C.3 Implement `BasicSemanticChunkExtractor` in `src/Aura.Application/Services/` — chunk splitting, PII stripping, tagging — 11 tests
- [x] C.4 Implement `AzureOpenAiEmbeddingProvider` in `src/Aura.Infrastructure/VectorStore/` — minimal V1, explicit hardening TODOs — 6 tests
- [x] C.5 Remove duplicated embedding from `QdrantSemanticIndexAdapter`; worker creates `EmbeddedSemanticChunk`
- [x] C.6 Fix DI: register `ISemanticChunkExtractor`, `IEmbeddingProvider`, `ISemanticOutboxRepository` — 3 new DI tests
