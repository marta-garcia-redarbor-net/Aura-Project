# Design: Infrastructure Organization Refactor

## Technical Approach

Restructure `Aura.Infrastructure` from generic technical folders (`Embedding/`, `VectorStore/`, `Persistence/`) to adapter-centric folders under `Adapters/`. Consolidate fragmented DI into a single `AddAuraInfrastructure` entry point. Extract Application-layer service registration (`BasicSemanticChunkExtractor`) into a new `AddAuraApplication` extension in `Aura.Application`. No logic changes — purely structural with namespace updates.

## Architecture Decisions

| Decision | Alternatives | Rationale |
|----------|-------------|-----------|
| Keep `EmbeddingResiliencePolicyBuilder` in `Adapters/Embedding/`, not `Shared/Resilience/` | Move to `Shared/Resilience/` | Builder depends on `EmbeddingProviderOptions` — it's embedding-specific. Create `Shared/Resilience/` only when a genuinely shared policy exists. |
| Add `Microsoft.Extensions.DependencyInjection.Abstractions` to `Aura.Application.csproj` | Keep DI in Infrastructure only | Application needs its own `AddAuraApplication` to register `BasicSemanticChunkExtractor`. The Abstractions package is a meta-package with no runtime coupling to containers. |
| Rename `VectorStore/` to `Adapters/SemanticIndex/` | Keep `VectorStore` name | `SemanticIndex` matches the domain port names (`ISemanticIndexWriter`, `ISemanticContextRetriever`). Provider name (`Qdrant`) stays on the class, not the folder. |
| Rename `Persistence/` to `Adapters/SemanticOutbox/` | Keep `Persistence/` | `Persistence` is generic; `SemanticOutbox` describes the domain responsibility. Future persistence adapters get their own `Adapters/{Responsibility}/` folder. |
| Make per-adapter DI methods `internal` | Keep `public` | Only `AddAuraInfrastructure` should be the public API. Adapter-level methods become internal, enforced by `InternalsVisibleTo` for tests. |

## Data Flow

DI composition after refactor:

```
Program.cs
  ├─ AddAuraApplication()              ← Aura.Application
  │    └─ ISemanticChunkExtractor → BasicSemanticChunkExtractor
  └─ AddAuraInfrastructure(config)     ← Aura.Infrastructure
       ├─ AddEmbeddingAdapter(config)         [internal]
       │    └─ IEmbeddingProvider → MeaiEmbeddingProvider + Polly + OTel
       ├─ AddSemanticIndexAdapter(config)     [internal]
       │    └─ ISemanticIndexWriter, ISemanticContextRetriever → Qdrant adapters
       └─ AddSemanticOutboxAdapter(config)    [internal]
            └─ ISemanticOutboxRepository → SqliteSemanticOutboxRepository
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Infrastructure/DependencyInjection.cs` | Create | Unified `AddAuraInfrastructure` calling internal adapter DI methods |
| `src/Aura.Infrastructure/Adapters/Embedding/*.cs` | Move | From `Embedding/` — 5 files, update namespace to `Aura.Infrastructure.Adapters.Embedding` |
| `src/Aura.Infrastructure/Adapters/SemanticIndex/*.cs` | Move | From `VectorStore/` — 4 files (excl. old DI), namespace `Aura.Infrastructure.Adapters.SemanticIndex` |
| `src/Aura.Infrastructure/Adapters/SemanticIndex/DependencyInjection.cs` | Modify | Rename method to `AddSemanticIndexAdapter`, make `internal`, remove `BasicSemanticChunkExtractor` and `SqliteSemanticOutboxRepository` registrations |
| `src/Aura.Infrastructure/Adapters/Embedding/DependencyInjection.cs` | Modify | Rename method to `AddEmbeddingAdapter`, make `internal` |
| `src/Aura.Infrastructure/Adapters/SemanticOutbox/SqliteSemanticOutboxRepository.cs` | Move | From `Persistence/`, namespace `Aura.Infrastructure.Adapters.SemanticOutbox` |
| `src/Aura.Infrastructure/Adapters/SemanticOutbox/DependencyInjection.cs` | Create | `internal` `AddSemanticOutboxAdapter` with SQLite outbox registration |
| `src/Aura.Application/DependencyInjection.cs` | Create | `AddAuraApplication` registering `BasicSemanticChunkExtractor` |
| `src/Aura.Application/Aura.Application.csproj` | Modify | Add `Microsoft.Extensions.DependencyInjection.Abstractions` package reference |
| `src/Aura.Workers/Program.cs` | Modify | Replace two extension calls with `AddAuraApplication()` + `AddAuraInfrastructure(config)` |
| `src/Aura.Infrastructure/Embedding/` | Delete | Folder emptied after move |
| `src/Aura.Infrastructure/VectorStore/` | Delete | Folder emptied after move |
| `src/Aura.Infrastructure/Persistence/` | Delete | Folder emptied after move |
| `tests/Aura.UnitTests/Infrastructure/Embedding*.cs` | Modify | Update `using` directives to new namespaces |
| `tests/Aura.UnitTests/VectorStore/DependencyInjectionTests.cs` | Modify | Update namespaces, remove chunk extractor/outbox assertions (moved to Application DI tests) |
| `tests/Aura.IntegrationTests/Workers/WorkersHostCompositionTests.cs` | Modify | Use `AddAuraApplication()` + `AddAuraInfrastructure(config)` |
| `tests/Aura.UnitTests/Application/DependencyInjectionTests.cs` | Create | Tests for `AddAuraApplication` (chunk extractor registration) |
| `tests/Aura.UnitTests/Infrastructure/InfrastructureDependencyInjectionTests.cs` | Create | Tests for unified `AddAuraInfrastructure` |

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | `AddAuraInfrastructure` resolves all adapters | ServiceCollection assertions (existing pattern) |
| Unit | `AddAuraApplication` resolves `ISemanticChunkExtractor` | ServiceCollection assertion |
| Unit | Per-adapter internal DI methods still resolve correctly | Existing tests updated with new namespaces |
| Integration | Workers host composition via unified DI | Existing `WorkersHostCompositionTests` updated |
| Architecture | Clean Architecture boundaries hold after namespace changes | Existing `SemanticIndexArchitectureTests` — passes without changes (checks `Aura.Infrastructure` prefix, not subfolders) |

## Migration / Rollout

No migration required. Purely structural — `git revert` of the PR is sufficient rollback.

## Open Questions

- [ ] None — all decisions are clear from the proposal and codebase analysis.
