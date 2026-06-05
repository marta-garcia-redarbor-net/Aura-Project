# Proposal: embedding-provider-hardening

## Intent

Harden the current minimal embedding/runtime path to improve resilience, batching, and observability, while adopting `Microsoft.Extensions.AI` as the standardized AI foundation instead of building custom infrastructure.

## Scope

### In Scope
- Adopt `Microsoft.Extensions.AI` (MEAI) for the embedding generation layer in `Infrastructure`.
- Update the internal `IEmbeddingProvider` port to support batch requests.
- Implement standard .NET resilience pipelines (retries, timeouts) compatible with MEAI.
- Add OpenTelemetry instrumentation (Activity tags, token tracking) to the provider.
- Implement a unified E2E integration test for the full semantic index sync pipeline.

### Out of Scope
- Introducing a heavier Docker security-proxy or LlamaGuard pattern.
- Replacing Qdrant with another vector store.
- Advanced semantic chunking logic (staying with the basic extractor).
- Exposing MEAI types directly to `Domain` or `Application` (maintaining Clean Architecture).

## Capabilities

### New Capabilities
- None

### Modified Capabilities
- `semantic-index`: Update the outbox synchronization requirements to accumulate and process semantic chunks in batches, optimizing the usage of the embedding provider.

## Approach

Use `Microsoft.Extensions.AI` (MEAI) as the core engine within `Infrastructure` to generate embeddings. The existing `IEmbeddingProvider` port in `Application` will be updated to accept `IReadOnlyList<string>` for batching. The implementation (`AzureOpenAiEmbeddingProvider`) will act as an adapter wrapping MEAI's `IEmbeddingGenerator`, leveraging its built-in telemetry and resilience support. We will avoid exposing MEAI abstractions outside of `Infrastructure` to respect `aura-plugin-design` and `aura-clean-arch-guard` rules.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Application/Ports/IEmbeddingProvider.cs` | Modified | Update signature to support batching (`IReadOnlyList<string>`). |
| `src/Aura.Infrastructure/VectorStore/AzureOpenAiEmbeddingProvider.cs` | Modified | Wrap `Microsoft.Extensions.AI` for batching, resilience, and telemetry. |
| `src/Aura.Infrastructure/VectorStore/DependencyInjection.cs` | Modified | Register MEAI services and resilience handlers. |
| `src/Aura.Workers/SemanticIndex/SemanticIndexSyncWorker.cs` | Modified | Update to batch outbox messages before embedding. |
| `tests/Aura.E2E/` | New | Add E2E test for the full semantic index pipeline. |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Context Window Limits | Medium | Configure MEAI with token counting and safe batch size limits before sending. |
| SDK Abstraction Leaks | Low | Ensure `Microsoft.Extensions.AI` types stay strictly within `Aura.Infrastructure`. |

## Rollback Plan

Revert the PR to restore the previous V1 `AzureOpenAiEmbeddingProvider` and non-batching worker behavior. Ensure Qdrant collections do not require schema changes.

## Dependencies

- `Microsoft.Extensions.AI` (NuGet)
- `Testcontainers.Qdrant` and `WireMock.Net` (for E2E tests)

## Success Criteria

- [ ] Provider successfully processes batched strings using `Microsoft.Extensions.AI`.
- [ ] Retries and timeouts are active and verifiable via OpenTelemetry traces.
- [ ] E2E test proves an outbox event travels fully to Qdrant using WireMock and Testcontainers.
- [ ] No `Microsoft.Extensions.AI` types leak into `Aura.Domain` or `Aura.Application`.
