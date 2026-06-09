# Exploration: infrastructure-organization-refactor

## Goal
Explore the pending refactor of `Aura.Infrastructure` so the project can continue from a cleaner foundation.

## Current State
`Aura.Infrastructure` currently organizes files into generic technical folders (`Embedding/`, `Persistence/`, `VectorStore/`). This mixes multiple capabilities (like semantic outbox and vector indices) within broad folders, and fragments Dependency Injection (`DependencyInjection.cs`) across multiple subfolders instead of providing a unified composition root. Additionally, Application layer services (like `BasicSemanticChunkExtractor`) are wrongly registered within Infrastructure DI extensions.

## Affected Areas
- `src/Aura.Infrastructure/VectorStore/` — Contains Qdrant adapters mixed with Outbox DI and ChunkExtractor DI.
- `src/Aura.Infrastructure/Embedding/` — Contains MEAI provider and resilience policies.
- `src/Aura.Infrastructure/Persistence/` — Contains SQLite outbox.
- `src/Aura.Workers/Program.cs` — Directly calls fragmented DI extensions.
- `src/Aura.Application/` — Missing a dedicated DI extension for its own services.

## Approaches

1. **Adapter-centric Folders with Root DI (Recommended)**
   - Group infrastructure implementations by the primary adapter responsibility.
   - Extract cross-cutting concerns (like Resilience) to a `Shared/` folder.
   - Unify all Infrastructure DI in a single root `DependencyInjection.cs`.
   - Extract Application DI to its own project.
   - **Pros:** Clear alignment with Clean Architecture and Ports & Adapters. Easier to swap adapters in the future.
   - **Cons:** Requires namespace changes and touching multiple files.
   - **Effort:** Low (mostly structural moves and namespace updates).

2. **Leave as is**
   - Continue adding technical folders.
   - **Pros:** No immediate refactor work.
   - **Cons:** Cognitive load increases. Unclear boundaries. Violation of architecture guidelines (Application services registered in Infrastructure DI).
   - **Effort:** None.

## Recommendation
**Adapter-centric Folders with Root DI**

**Target Folder Model:**
- `src/Aura.Infrastructure/Adapters/Embedding/` (`MeaiEmbeddingProvider.cs`, options, validators)
- `src/Aura.Infrastructure/Adapters/SemanticIndex/` (`QdrantSemanticIndexAdapter.cs`, context adapters, mapping, options)
- `src/Aura.Infrastructure/Adapters/SemanticOutbox/` (`SqliteSemanticOutboxRepository.cs`)
- `src/Aura.Infrastructure/Shared/Resilience/` (`EmbeddingResiliencePolicyBuilder.cs`)
- `src/Aura.Infrastructure/DependencyInjection.cs` (Root extension method `AddAuraInfrastructure`)
- `src/Aura.Application/DependencyInjection.cs` (Root extension method `AddAuraApplication` to house `BasicSemanticChunkExtractor`)

**In Scope:**
- Moving files into the structure above.
- Updating namespaces across the moved files.
- Creating the unified DI extensions.
- Updating `Aura.Workers/Program.cs` and Integration/Unit test suites to consume the new unified extensions.

**Out of Scope:**
- Any logic changes to the adapters themselves.
- Updating SDK versions.
- Changes to the Domain layer.

## Risks
- Broken test compilation due to namespace changes.
- Forgotten `using` statements in `Aura.Workers` or test projects.
- Ensure that the application still runs locally since the DI pipeline is being restructured.

## Review Size Forecast
- **Estimated review size:** ~350 lines changed (mostly structural).
- **Recommendation:** Single PR. The budget is 400 lines, and since these are largely file moves and namespace updates without logic changes, a single PR is safe and reviewable. Chaining is not required.

## Ready for Proposal
Yes. The problem is well-understood, the target structure is clearly mapped out, and the boundaries match our Clean Architecture Guard guidelines.
