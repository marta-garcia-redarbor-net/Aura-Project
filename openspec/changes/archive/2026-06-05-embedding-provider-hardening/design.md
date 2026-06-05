# Design: Embedding Provider Hardening

## Technical Approach

Wrap `Microsoft.Extensions.AI`'s `IEmbeddingGenerator<string, Embedding<float>>` inside a clean Infrastructure adapter (`MeaiEmbeddingProvider`) that implements our `IEmbeddingProvider` port. Wire resilience via Polly v8 `ResiliencePipeline`, telemetry via MEAI's built-in OTel middleware plus custom Activity tags, and batch-splitting inside the adapter. Update the port to batch input. Introduce `EmbeddedSemanticChunk` DTO so the worker orchestrates embed-then-write explicitly.

## Architecture Decisions

| Decision | Choice | Alternatives | Rationale |
|---|---|---|---|
| AI abstraction layer | MEAI `IEmbeddingGenerator` | Semantic Kernel `ITextEmbeddingGenerationService`; raw HTTP | MEAI is focused on embeddings without pulling SK's Kernel/plugin/agent overhead. SK is better suited for future agent orchestration (Reviewer, Triage), not adapter hardening. |
| Resilience mechanism | Polly v8 `ResiliencePipeline` wrapping adapter calls | MEAI pipeline middleware; Azure.Core internal retry | MEAI has no built-in resilience middleware. Polly v8 gives explicit retry/circuit-breaker/timeout with backoff. Azure.Core retry is internal and insufficiently configurable. |
| Embedding delivery to writer | Explicit `EmbeddedSemanticChunk` DTO | Writer calls `IEmbeddingProvider` internally | Worker orchestrates extract-embed-write. Each step independently testable. Writer stays single-responsibility. |
| Telemetry | MEAI `UseOpenTelemetry()` + custom Activity tags | Manual Activity creation only | MEAI OTel middleware provides standard spans; custom tags add `batch_size`, `token_usage`, `model_name` on top. |
| Config validation | `IValidateOptions<EmbeddingProviderOptions>` | Manual checks at startup | Standard .NET options pattern. Fail-fast on missing endpoint, model, or invalid limits. |

## Data Flow

```
Outbox Events
  |
  v
SemanticIndexSyncWorker (Workers)
  |
  |-- ISemanticChunkExtractor.ExtractAsync()         [Application port]
  |     => chunks: IReadOnlyList<SemanticChunk>
  |
  |-- Split(chunks, maxBatchSize)                    [Worker-local logic]
  |     => sub-batches respecting item/token limits
  |
  |-- IEmbeddingProvider.GenerateEmbeddingsAsync()   [Application port]
  |     => vectors: IReadOnlyList<ReadOnlyMemory<float>>
  |     => emits OTel span: batch_size, token_usage
  |     => wraps MEAI IEmbeddingGenerator (Infra only)
  |     => retry: Polly v8 (429, 503, timeout)
  |
  |-- Zip(chunks, vectors) => EmbeddedSemanticChunk[] [Application DTO]
  |
  +-- ISemanticIndexWriter.WriteAsync()              [Application port]
        => Qdrant adapter (Infrastructure)
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Application/Ports/IEmbeddingProvider.cs` | Modify | Batch signature: `IReadOnlyList<string>` in, `IReadOnlyList<ReadOnlyMemory<float>>` out |
| `src/Aura.Application/Models/EmbeddedSemanticChunk.cs` | Create | Pairs `SemanticChunk` + `ReadOnlyMemory<float>` |
| `src/Aura.Application/Ports/ISemanticIndexWriter.cs` | Modify | Accept `IReadOnlyList<EmbeddedSemanticChunk>` instead of plain chunks |
| `src/Aura.Infrastructure/Embedding/MeaiEmbeddingProvider.cs` | Create | Adapter: MEAI `IEmbeddingGenerator` to `IEmbeddingProvider` |
| `src/Aura.Infrastructure/Embedding/EmbeddingProviderOptions.cs` | Create | Config: endpoint, deployment, max batch size, timeout, retries |
| `src/Aura.Infrastructure/Embedding/EmbeddingProviderOptionsValidator.cs` | Create | `IValidateOptions` fail-fast validation |
| `src/Aura.Infrastructure/Embedding/DependencyInjection.cs` | Create | Wire MEAI pipeline + Polly + adapter |
| `src/Aura.Infrastructure/Aura.Infrastructure.csproj` | Modify | Add MEAI provider package, `Microsoft.Extensions.Resilience` |
| `src/Aura.Workers/SemanticIndex/SemanticIndexSyncWorker.cs` | Create | Batching worker orchestrating the pipeline |
| `src/Aura.Workers/Program.cs` | Modify | Register Infrastructure services + worker |
| `tests/Aura.UnitTests/Infrastructure/MeaiEmbeddingProviderTests.cs` | Create | Mock `IEmbeddingGenerator`, test adapter + batch split |
| `tests/Aura.IntegrationTests/Embedding/EmbeddingResilienceTests.cs` | Create | WireMock + real Polly pipeline (429 retry, timeout) |
| `tests/Aura.ArchitectureTests/SemanticIndexArchitectureTests.cs` | Modify | Add MEAI leakage guard for Domain and Application |
| `tests/Aura.E2E/SemanticIndexPipelineTests.cs` | Create | WireMock (AI API) + Testcontainers (Qdrant), full pipeline |

## Interfaces / Contracts

```csharp
// Updated port (Application)
public interface IEmbeddingProvider
{
    Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts, CancellationToken ct);
}

// New DTO (Application)
public sealed record EmbeddedSemanticChunk
{
    public required SemanticChunk Chunk { get; init; }
    public required ReadOnlyMemory<float> Embedding { get; init; }
}

// Updated writer (Application)
public interface ISemanticIndexWriter
{
    Task WriteAsync(IReadOnlyList<EmbeddedSemanticChunk> chunks, CancellationToken ct);
    Task DeleteByCanonicalIdAsync(string canonicalSourceId, CancellationToken ct);
}

// Config (Infrastructure)
public sealed class EmbeddingProviderOptions
{
    public required string Endpoint { get; set; }
    public required string DeploymentName { get; set; }
    public int MaxBatchSize { get; set; } = 16;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
}
```

## Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| Unit | `MeaiEmbeddingProvider`, batch splitter, config validation | Mock `IEmbeddingGenerator<string, Embedding<float>>`; verify mapping, splitting, Activity tags |
| Integration | Resilience pipeline (retry on 429, timeout, circuit break) | WireMock.Net faking AI API + real Polly pipeline |
| Architecture | MEAI types not leaking into Domain/Application | NetArchTest: `ShouldNot.HaveDependencyOn("Microsoft.Extensions.AI")` on Domain + Application assemblies |
| E2E | Full pipeline: extract, embed, write, retrieve | WireMock.Net (AI API) + Testcontainers.Qdrant; synthetic events driven in-process |

## Migration / Rollout

No migration required. Qdrant collections remain schema-unchanged. The `IEmbeddingProvider` port signature changes from single to batch; all consumers are in-tree and updated atomically.

## Open Questions

- [ ] **MEAI provider package**: Which concrete provider? `Microsoft.Extensions.AI.OpenAI` (covers Azure OpenAI via `Azure.AI.OpenAI`) vs `Microsoft.Extensions.AI.AzureAIInference`? Depends on target Azure AI service.
- [ ] **Outbox store for E2E**: No `IOutboxStore` port exists yet. Should the E2E test use an in-memory outbox substitute to drive the full pipeline, or test only the embed-write-retrieve slice and defer full outbox E2E?
- [ ] **Batch size defaults**: What are reasonable defaults for `MaxBatchSize` (items per request)? Should we add a `MaxTokensPerBatch` limit? Provider limits vary.
- [ ] **Semantic Kernel scope**: SK is explicitly deferred to future agent orchestration (Reviewer, Triage). Confirm acceptable for this change.
