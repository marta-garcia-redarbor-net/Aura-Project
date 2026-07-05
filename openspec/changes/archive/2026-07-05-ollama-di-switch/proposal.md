# Proposal: Ollama DI Switch for Embeddings

## Intent

Let the embedding provider be swapped via config — Azure OpenAI for production, local Ollama for dev/offline. Currently the DI registration hardcodes OpenAI; adding an Ollama path enables local development without cloud dependency, reduces cost, and supports air-gapped scenarios.

## Scope

### In Scope
- Add `Microsoft.Extensions.AI.Ollama` NuGet to `Aura.Infrastructure`
- Add `Provider` field (`"OpenAI" | "Ollama"`) to `EmbeddingProviderOptions`
- Update `EmbeddingProviderOptionsValidator` to validate the new field
- Refactor `DependencyInjection.cs` to select OpenAI or Ollama pipeline based on config
- Update test configs and `appsettings.Development.json`
- Cover with existing DI composition tests + new unit tests for provider selection

### Out of Scope
- Changing `MeaiEmbeddingProvider` — it stays generic, receives `IEmbeddingGenerator` via DI
- Changing `IEmbeddingProvider` port
- Adding Ollama to non-embedding features (chat, completion)
- Performance benchmarking between providers
- Model download or Ollama installation automation

## Capabilities

### New Capabilities
None

### Modified Capabilities
- `semantic-index`: Add a scenario to "Observable and Resilient Embedding Generation" requirement covering provider selection via config — the system MUST compose the correct generator based on `EmbeddingProviderOptions.Provider`

## Approach

1. Add `Microsoft.Extensions.AI.Ollama` package reference
2. Add `Provider` string property + validator to options
3. In `DependencyInjection.cs`, branch on `options.Provider`:
   - `"OpenAI"` (default): existing OpenAI pipeline
   - `"Ollama"`: create `OllamaApiClient(endpoint)`, call `.AsIEmbeddingGenerator()`, attach same OTel + resilience
4. Update test configs with `Provider: "OpenAI"` for existing tests
5. Add new tests: provider selection resolves correct generator, Ollama path composes without error

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Aura.Infrastructure/Aura.Infrastructure.csproj` | Modified | Add NuGet package |
| `Adapters/Embedding/EmbeddingProviderOptions.cs` | Modified | Add `Provider` property |
| `Adapters/Embedding/EmbeddingProviderOptionsValidator.cs` | Modified | Validate `Provider` |
| `Adapters/Embedding/DependencyInjection.cs` | Modified | Branch on provider |
| `tests/*/appsettings*.json` | Modified | Add `Provider: "OpenAI"` |
| `src/Aura.Workers/appsettings.Development.json` | Modified | Add Ollama config for local |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Ollama not running locally when selected | Medium | Fail-fast at startup, clear error message |
| Ollama model mismatch | Low | Document required model in config |
| Breaking existing OpenAI configs after deploy | Low | Default `Provider` is `"OpenAI"` — backward compat |

## Rollback Plan

- Revert the single `DependencyInjection.cs` file
- Remove the NuGet package reference
- Old configs without `Provider` field work because default is `"OpenAI"`

## Dependencies

- `Microsoft.Extensions.AI.Ollama` 10.6.0 (must match existing MEAI version)
- Ollama runtime (for dev testing)

## Success Criteria

- [ ] Tests pass: provider selection, DI composition, validator, existing scenarios
- [ ] Host starts with `Provider: "OpenAI"` — no change in behavior
- [ ] Host starts with `Provider: "Ollama"` — embeds via local Ollama
- [ ] Missing/invalid provider throws meaningful validation error
