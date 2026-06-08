## Exploration: embedding-hardening-warning-followup

### Current State
The recent `embedding-provider-hardening` change was archived after successfully migrating to `Microsoft.Extensions.AI`. However, several non-blocking warnings remain:
1. `src/Aura.Workers/Program.cs` lacks a runtime host composition proof test.
2. The real `OpenAIClient` construction path in `DependencyInjection.cs` lacks execution coverage.
3. The Polly timeout resilience policy is registered but lacks explicit proof.
4. The legacy `AzureOpenAiEmbeddingProvider` class and its V1 DI registration in `Aura.Infrastructure.VectorStore` were bypassed but not deleted.

### Affected Areas
- `src/Aura.Infrastructure/VectorStore/AzureOpenAiEmbeddingProvider.cs` — Legacy V1 provider to be deleted.
- `src/Aura.Infrastructure/VectorStore/DependencyInjection.cs` — Needs legacy V1 provider registration removed.
- `tests/Aura.UnitTests/VectorStore/AzureOpenAiEmbeddingProviderTests.cs` — Legacy tests to be deleted.
- `tests/Aura.UnitTests/VectorStore/DependencyInjectionTests.cs` — Needs legacy provider expectation removed.
- `tests/Aura.UnitTests/Infrastructure/EmbeddingDependencyInjectionTests.cs` — Needs tests added for `OpenAIClient` DI resolution and timeout policy verification.
- `tests/Aura.IntegrationTests/Workers/ProgramCompositionTests.cs` (New) — Needs to be created to verify host and background service composition.

### Approaches
1. **Single PR Cleanup** — Remove all legacy code and add the missing composition and timeout tests in one pass.
   - Pros: Low complexity, completely addresses all remaining technical debt from the prior change.
   - Cons: None.
   - Effort: Low (~220 lines modified, mostly test additions and legacy deletions).

### Recommendation
**Single PR Cleanup**
The work is highly cohesive (purely fixing leftover testing and cleanup warnings) and well under the 400-line review budget. Removing the legacy V1 Qdrant Azure provider reduces codebase noise while adding the DI/composition tests improves confidence.

### Risks
- `Program.cs` in `Aura.Workers` is a top-level statements file in .NET 9, so exposing it to tests via `InternalsVisibleTo` or a partial class might be required for the integration test.
- Attempting to resolve `IEmbeddingProvider` in a test without valid environment variables could throw an exception from the OpenAI SDK if validation isn't mocked properly.

### Ready for Proposal
Yes. The scope is well-defined and constrained.