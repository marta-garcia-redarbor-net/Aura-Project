# Tasks: Embedding Provider Hardening

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 580-680 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 (contracts + adapter + unit tests) -> PR 2 (worker + resilience + E2E) |
| Delivery strategy | ask-always |
| Chain strategy | feature-branch-chain |

Decision needed before apply: No (resolved)
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | MEAI adapter with batch port, config, arch guard, and unit tests | PR 1 | Base: main; standalone deliverable; adapter usable without worker |
| 2 | Batching worker, resilience integration tests, and E2E pipeline test | PR 2 | Base: PR 1 branch; depends on batch port + adapter from PR 1 |

## Phase 1: Contracts & DTO (Application layer)

- [x] 1.1 Update `IEmbeddingProvider` in `src/Aura.Application/Ports/IEmbeddingProvider.cs`: change signature to `Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken ct)`
- [x] 1.2 Create `EmbeddedSemanticChunk` record in `src/Aura.Application/Models/EmbeddedSemanticChunk.cs` pairing `SemanticChunk` + `ReadOnlyMemory<float>` — already existed with correct shape
- [x] 1.3 Update `ISemanticIndexWriter` in `src/Aura.Application/Ports/ISemanticIndexWriter.cs`: change `WriteAsync` to accept `IReadOnlyList<EmbeddedSemanticChunk>` — already matched design

## Phase 2: MEAI Adapter & Config (Infrastructure layer)

- [x] 2.1 Add `Microsoft.Extensions.AI.OpenAI` and `Microsoft.Extensions.Resilience` packages to `src/Aura.Infrastructure/Aura.Infrastructure.csproj`
- [x] 2.2 Create `EmbeddingProviderOptions` in `src/Aura.Infrastructure/Embedding/EmbeddingProviderOptions.cs` with `Endpoint`, `DeploymentName`, `MaxBatchSize`, `MaxTokensPerBatch`, `TimeoutSeconds`, `MaxRetries`
- [x] 2.3 Create `EmbeddingProviderOptionsValidator` in `src/Aura.Infrastructure/Embedding/EmbeddingProviderOptionsValidator.cs` implementing `IValidateOptions<EmbeddingProviderOptions>` (fail-fast on missing endpoint/model/invalid limits)
- [x] 2.4 Create `MeaiEmbeddingProvider` in `src/Aura.Infrastructure/Embedding/MeaiEmbeddingProvider.cs` wrapping `IEmbeddingGenerator<string, Embedding<float>>` with batch-splitting by `MaxBatchSize` and `MaxTokensPerBatch`, custom Activity tags (`batch_size`, `token_usage`, `model_name`)
- [x] 2.5 Create DI wiring in `src/Aura.Infrastructure/Embedding/DependencyInjection.cs`: register MEAI pipeline with `UseOpenTelemetry()`, Polly `ResiliencePipeline` (429/503 retry + timeout), options validation, and `IEmbeddingProvider` adapter

## Phase 3: Architecture Guard & Unit Tests

- [x] 3.1 Add MEAI leakage tests to `tests/Aura.ArchitectureTests/SemanticIndexArchitectureTests.cs`: Domain and Application `ShouldNot.HaveDependencyOn("Microsoft.Extensions.AI")`
- [x] 3.2 Create `tests/Aura.UnitTests/Infrastructure/MeaiEmbeddingProviderTests.cs`: mock `IEmbeddingGenerator`, verify batch splitting (by item count and token limit), verify Activity tag emission, verify mapping from MEAI `Embedding<float>` to `ReadOnlyMemory<float>`

## Phase 4: Worker & Wiring

- [x] 4.1 Migrated `SemanticIndexSyncWorker` to batch API: replaced per-chunk `GenerateEmbeddingAsync` with batch `GenerateEmbeddingsAsync` + LINQ `Zip` for enrichment
- [x] 4.1-fix **CORRECTIVE**: Refactored worker to accumulate chunks across all pending entries into a single `GenerateEmbeddingsAsync` call, then distribute results back per-entry for write isolation (spec: "Syncing new evidence in batches"). Added 4 cross-entry accumulation tests.
- [x] 4.2 Updated `src/Aura.Workers/Program.cs`: added `AddMeaiEmbeddingProvider(builder.Configuration)` to wire MEAI adapter (overrides old registration from `AddQdrantSemanticIndex`)

## Phase 5: Integration & E2E Tests

- [x] 5.1 Created `tests/Aura.IntegrationTests/Embedding/EmbeddingResilienceTests.cs`: fake `IEmbeddingGenerator` simulating 429/503/400, real Polly DI pipeline; tests retry success, exhaustion, and non-retryable pass-through
- [x] 5.2 Created `tests/Aura.IntegrationTests/Embedding/SemanticIndexPipelineTests.cs`: real Qdrant via Testcontainers + deterministic embedder + `BasicSemanticChunkExtractor`; validates full extract→embed→write→retrieve pipeline with relevance ranking
