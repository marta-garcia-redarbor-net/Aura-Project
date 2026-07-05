# Tasks: Ollama DI Switch for Embeddings

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~95 |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | single-pr |
| Chain strategy | size-exception |

Decision needed before apply: Yes
Chained PRs recommended: No
Chain strategy: size-exception
400-line budget risk: Low

## Phase 1: Foundation

- [x] 1.1 Add `OllamaSharp` 5.4.25 to `src/Aura.Infrastructure/Aura.Infrastructure.csproj` (note: `Microsoft.Extensions.AI.Ollama` is deprecated on NuGet, replaced with OllamaSharp per Microsoft recommendation)
- [x] 1.2 Add `Provider` property (default `"OpenAI"`) to `EmbeddingProviderOptions.cs`

## Phase 2: Provider Validation (TDD)

- [x] 2.1 RED: Write tests for `EmbeddingProviderOptionsValidator` — accept `"OpenAI"`/`"Ollama"`, reject invalid/empty
- [x] 2.2 GREEN: Update `EmbeddingProviderOptionsValidator.cs` to validate Provider is `"OpenAI"` or `"Ollama"`

## Phase 3: DI Composition (TDD)

- [x] 3.1 RED: Write provider selection tests in `EmbeddingDependencyInjectionTests` — each Provider value resolves correct generator, default resolves OpenAI, unsupported throws
- [x] 3.2 GREEN: Add `CreateOpenAIGenerator` / `CreateOllamaGenerator` with switch expression in `DependencyInjection.cs`; share OTel middleware on both branches

## Phase 4: Config & Integration

- [x] 4.1 Add `Provider: "OpenAI"` to all test InMemoryCollection configs in IntegrationTests
- [x] 4.2 Add Ollama config block to `src/Aura.Workers/appsettings.Development.json`
- [x] 4.3 Run `dotnet test Aura.sln` — 984/984 all pass, including Ollama composition scenario

## Phase 5: Cleanup

- [x] 5.1 Verified: no ApiKey validation exists in validator. ApiKey only read in `CreateOpenAIGenerator` via `?? ""`. Ollama path never reads ApiKey.
- [x] 5.2 No temporary scaffolding or debug logging added — all clean.
