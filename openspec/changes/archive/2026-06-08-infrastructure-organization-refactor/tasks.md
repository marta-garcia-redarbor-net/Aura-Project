# Tasks: Infrastructure Organization Refactor

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 250–320 |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | ask-always |
| Chain strategy | pending |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Full infrastructure restructure + unified DI + tests | PR 1 | Single PR to `main`; purely structural, no logic changes |

## Phase 1: Application DI (TDD)

- [x] 1.1 RED — Create `tests/Aura.UnitTests/Application/DependencyInjectionTests.cs` asserting `AddAuraApplication` registers `ISemanticChunkExtractor` as `BasicSemanticChunkExtractor`
- [x] 1.2 GREEN — Add `Microsoft.Extensions.DependencyInjection.Abstractions` to `src/Aura.Application/Aura.Application.csproj`
- [x] 1.3 GREEN — Create `src/Aura.Application/DependencyInjection.cs` with public `AddAuraApplication` extension method registering `BasicSemanticChunkExtractor`

## Phase 2: Infrastructure Restructure + Adapter DI (TDD)

- [x] 2.1 RED — Create `tests/Aura.UnitTests/Infrastructure/InfrastructureDependencyInjectionTests.cs` asserting `AddAuraInfrastructure` resolves all adapter services (embedding, semantic index, outbox)
- [x] 2.2 Move `src/Aura.Infrastructure/Embedding/*.cs` → `src/Aura.Infrastructure/Adapters/Embedding/`, update namespace to `Aura.Infrastructure.Adapters.Embedding`
- [x] 2.3 Rename `AddMeaiEmbeddingProvider` → `AddEmbeddingAdapter`, make `internal` in `Adapters/Embedding/DependencyInjection.cs`
- [x] 2.4 Move `src/Aura.Infrastructure/VectorStore/*.cs` → `src/Aura.Infrastructure/Adapters/SemanticIndex/`, update namespace to `Aura.Infrastructure.Adapters.SemanticIndex`
- [x] 2.5 Rename `AddQdrantSemanticIndex` → `AddSemanticIndexAdapter`, make `internal`, remove `BasicSemanticChunkExtractor` and `SqliteSemanticOutboxRepository` registrations from `Adapters/SemanticIndex/DependencyInjection.cs`
- [x] 2.6 Move `src/Aura.Infrastructure/Persistence/SqliteSemanticOutboxRepository.cs` → `src/Aura.Infrastructure/Adapters/SemanticOutbox/`, update namespace to `Aura.Infrastructure.Adapters.SemanticOutbox`
- [x] 2.7 Create `src/Aura.Infrastructure/Adapters/SemanticOutbox/DependencyInjection.cs` — `internal` `AddSemanticOutboxAdapter` with SQLite outbox registration
- [x] 2.8 GREEN — Create `src/Aura.Infrastructure/DependencyInjection.cs` — public `AddAuraInfrastructure(IConfiguration)` calling the three internal adapter methods

## Phase 3: Consumer + Test Updates

- [x] 3.1 Update `src/Aura.Workers/Program.cs` — replace `AddQdrantSemanticIndex` + `AddMeaiEmbeddingProvider` with `AddAuraApplication()` + `AddAuraInfrastructure(config)`
- [x] 3.2 Update `using` directives in `tests/Aura.UnitTests/Infrastructure/EmbeddingDependencyInjectionTests.cs`, `MeaiEmbeddingProviderTests.cs`, `EmbeddingProviderOptionsValidatorTests.cs` to new namespaces
- [x] 3.3 Update `tests/Aura.UnitTests/VectorStore/DependencyInjectionTests.cs` — new namespaces, remove chunk extractor/outbox assertions (now in Application DI tests)
- [x] 3.4 Update `tests/Aura.IntegrationTests/Workers/WorkersHostCompositionTests.cs` — use `AddAuraApplication()` + `AddAuraInfrastructure(config)`

## Phase 4: Cleanup + Verification

- [x] 4.1 Delete empty folders: `src/Aura.Infrastructure/Embedding/`, `VectorStore/`, `Persistence/`
- [x] 4.2 Run `dotnet build Aura.sln` — verify zero errors
- [x] 4.3 Run `dotnet test Aura.sln` — verify all tests pass including architecture tests
