## Exploration: embedding-provider-hardening

### Current State
The `AzureOpenAiEmbeddingProvider` in `Aura.Infrastructure` is a minimal V1 implementation. It lacks resilience (retries, throttling awareness), configurable timeouts, observability (telemetry/token tracking), and batch embedding support. Currently, `IEmbeddingProvider` accepts a single string, which forces 1:1 API calls per semantic chunk.
Additionally, the semantic-index feature lacks a unified end-to-end (E2E) runtime test. While unit and integration tests exist for isolated components (Outbox, Worker, Qdrant Adapter), there is no single test that verifies a domain event correctly traveling from the `SqliteSemanticOutboxRepository` through the `SemanticIndexSyncWorker`, to the `AzureOpenAiEmbeddingProvider`, into Qdrant, and finally being retrieved by `ISemanticContextRetriever`.

### Affected Areas
- `src/Aura.Application/Ports/IEmbeddingProvider.cs` — Needs signature updates to support batching (`IReadOnlyList<string>`).
- `src/Aura.Infrastructure/VectorStore/AzureOpenAiEmbeddingProvider.cs` — Needs implementation of batching, telemetry (Activity tags), and token limits.
- `src/Aura.Infrastructure/VectorStore/DependencyInjection.cs` — Needs to configure Polly resilience pipelines (`AddStandardResilienceHandler`) on the HTTP client.
- `src/Aura.Application/Services/BasicSemanticChunkExtractor.cs` (or similar) — Might need to aggregate chunks to take advantage of provider batching.
- `tests/Aura.E2E/` — Needs a new scenario file to house the unified E2E integration test.

### Approaches

1. **Native HttpClient with Polly & OpenTelemetry**
   - Pros: Follows existing DI setup; zero new third-party SDK dependencies (uses .NET 8 standard resilience and OTel); keeps provider thin.
   - Cons: Token counting must be handled manually (e.g., via `Microsoft.ML.Tokenizers`); requires manual parsing of Azure's rate-limiting headers.
   - Effort: Medium

2. **Migrate to official `Azure.AI.OpenAI` SDK**
   - Pros: Automatic built-in retries (Azure.Core), token tracking, type-safe API, handles rate limit headers natively.
   - Cons: Adds a heavy external SDK dependency; hides some HTTP traffic details; forces mapping between SDK exceptions and domain exceptions.
   - Effort: Medium

### Recommendation
**Approach 1 (Native HttpClient with Polly & OpenTelemetry)**. 
Aura heavily emphasizes "Ports & Adapters" and "Observable by default". Using standard `AddStandardResilienceHandler()` keeps HTTP resilience logic centralized at the DI level rather than hidden in a vendor SDK. We can track token counts by extracting them from the raw JSON response and attaching them as `Activity` tags for OpenTelemetry. We should update the `IEmbeddingProvider` port to support batch requests to reduce network overhead.
For the E2E test, we should place it in `tests/Aura.E2E/`, utilizing `Testcontainers` for Qdrant and `WireMock.Net` to simulate the Azure OpenAI endpoint to prevent cloud dependency in CI.

### Risks
- **Batching Complexity**: Updating the port to support batching will require the `SemanticIndexSyncWorker` to accumulate chunks before dispatching, which increases worker complexity.
- **Token Limits**: Without a tokenizer library like Tiktoken, we risk sending batches that exceed the model's maximum context window, leading to 400 errors.

### Ready for Proposal
Yes. We have a clear understanding of the architectural boundaries for resilience and telemetry, as well as a strategy for the E2E test. The orchestrator can proceed to the `sdd-propose` phase.
