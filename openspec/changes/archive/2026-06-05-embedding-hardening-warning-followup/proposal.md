# Proposal: Embedding Hardening Warning Follow-up

Addressing non-blocking warnings identified during the `embedding-provider-hardening` verification phase. This change cleans up legacy code, proves timeout resilience, and adds host composition tests.

## Intent

The previous embedding provider hardening cycle achieved full spec compliance but left a few verification warnings: legacy provider files still existed, timeout behavior lacked a dedicated runtime test, and `Aura.Workers/Program.cs` lacked host composition coverage. This change addresses those warnings to finalize the hardening effort without expanding scope.

## Scope

### In Scope
- Remove legacy `AzureOpenAiEmbeddingProvider` and its associated tests.
- Add an explicit timeout resilience test for `IEmbeddingProvider`.
- Add a host composition test for `Aura.Workers/Program.cs` to prove DI registration order.
- Add tests to cover the real OpenAI generator construction path in `DependencyInjection.cs`.

### Out of Scope
- New features or embedding capabilities.
- Changes to the core `SemanticIndexSyncWorker` logic.
- E2E tests for the full pipeline.

## Capabilities

> This section is the CONTRACT between proposal and specs phases.

### New Capabilities
- None.

### Modified Capabilities
- None.

## Approach

1. **Cleanup**: Delete `AzureOpenAiEmbeddingProvider.cs` and `AzureOpenAiEmbeddingProviderTests.cs`.
2. **Resilience Test**: Add a test in `EmbeddingResilienceTests.cs` that forces a timeout using a fake generator that delays longer than the Polly timeout policy, ensuring `TimeoutRejectedException` is handled or propagated correctly.
3. **Host Composition**: Append `public partial class Program { }` to `Aura.Workers/Program.cs` and create `WorkersHostCompositionTests.cs` in the integration test layer to verify that `IServiceCollection` resolves hosted services correctly.
4. **Coverage Fixes**: Add a test in `EmbeddingDependencyInjectionTests.cs` to exercise the real OpenAI client registration branch.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Infrastructure/Embedding/AzureOpenAiEmbeddingProvider.cs` | Removed | Legacy provider. |
| `tests/Aura.UnitTests/Infrastructure/AzureOpenAiEmbeddingProviderTests.cs` | Removed | Legacy tests. |
| `tests/Aura.IntegrationTests/Embedding/EmbeddingResilienceTests.cs` | Modified | Add timeout proof. |
| `tests/Aura.IntegrationTests/Workers/WorkersHostCompositionTests.cs` | New | Add host composition proof. |
| `src/Aura.Workers/Program.cs` | Modified | Expose partial class for testing. |
| `tests/Aura.UnitTests/Infrastructure/EmbeddingDependencyInjectionTests.cs` | Modified | Cover real OpenAI generator path. |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Timeout test flakiness in CI | Med | Use small, deterministic timeouts or Polly's testing utilities. |
| `Program` class visibility issues | Low | Use standard .NET 8 `public partial class Program { }` at the end of the top-level statements file. |

## Rollback Plan

Revert the PR. No data schema, external infrastructure state, or primary APIs are modified.

## Dependencies

- None.

## Success Criteria

- [ ] Legacy Azure OpenAI provider is completely removed.
- [ ] Timeout behavior is proven at runtime.
- [ ] `Aura.Workers/Program.cs` achieves >0% execution coverage.
- [ ] `DependencyInjection.cs` real construction path is covered.
