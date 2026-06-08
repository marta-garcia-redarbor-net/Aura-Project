# Tasks: Embedding Hardening Warning Follow-up

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 350-400 |
| 400-line budget risk | Medium |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Medium

**Rationale**: ~280 of the changed lines are deletions of dead code (trivial to review). Net additions are ~110 lines of test code. Cognitive review load is Low despite the raw count being borderline.

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Legacy cleanup + all new tests | PR 1 | Single PR; deletions are trivial, additions are test-only |

## Phase 1: Legacy Cleanup (TDD: remove dead code, update existing tests)

- [x] 1.1 Delete `src/Aura.Infrastructure/VectorStore/AzureOpenAiEmbeddingProvider.cs`
- [x] 1.2 Delete `tests/Aura.UnitTests/VectorStore/AzureOpenAiEmbeddingProviderTests.cs`
- [x] 1.3 Remove `IEmbeddingProvider` factory registration (lines 47-62 + associated `using`) from `src/Aura.Infrastructure/VectorStore/DependencyInjection.cs`
- [x] 1.4 Remove `AddQdrantSemanticIndex_ResolvesEmbeddingProvider` test from `tests/Aura.UnitTests/VectorStore/DependencyInjectionTests.cs`
- [x] 1.5 Run `dotnet build Aura.sln` -- confirm zero compile errors after cleanup

## Phase 2: Resilience & Host Composition Tests

- [x] 2.1 Add `GenerateEmbeddingsAsync_TimeoutExceeded_ThrowsTimeoutRejectedException` to `tests/Aura.IntegrationTests/Embedding/EmbeddingResilienceTests.cs` (stalling `TaskCompletionSource` fake, 1s Polly timeout, assert `TimeoutRejectedException`)
- [x] 2.2 Append `public partial class Program { }` to `src/Aura.Workers/Program.cs`
- [x] 2.3 Add `<ProjectReference>` to `Aura.Workers` in `tests/Aura.IntegrationTests/Aura.IntegrationTests.csproj`
- [x] 2.4 Create `tests/Aura.IntegrationTests/Workers/WorkersHostCompositionTests.cs` -- resolve `IEmbeddingProvider`, `ISemanticIndexWriter`, `IHostedService` from `ServiceCollection` wired via same extensions as `Program.cs`

## Phase 3: DI Coverage

- [x] 3.1 Add real MEAI generator resolution test in `tests/Aura.UnitTests/Infrastructure/EmbeddingDependencyInjectionTests.cs` -- `BuildServiceProvider().GetRequiredService<IEmbeddingProvider>()` with in-memory config, assert correct pipeline type

## Phase 4: Verification

- [x] 4.1 Run `dotnet test Aura.sln` -- all tests green
- [x] 4.2 Run `dotnet build Aura.sln` -- zero warnings from changed files
