# Proposal: Infrastructure Organization Refactor

Refactoring `Aura.Infrastructure` to align with Clean Architecture by organizing files by adapter responsibility and unifying Dependency Injection.

## Quick path

1. Move infrastructure implementations into adapter-centric folders (`Adapters/` and `Shared/`).
2. Create root `DependencyInjection.cs` files for `Aura.Infrastructure` and `Aura.Application`.
3. Update namespaces and consumer registrations (e.g., in `Aura.Workers`).

## Intent

The current `Aura.Infrastructure` structure mixes distinct capabilities within generic technical folders. DI extensions are fragmented across subfolders, and Application-layer services are incorrectly registered inside Infrastructure extensions. This refactor enforces architectural boundaries and reduces cognitive load by grouping implementations by their primary adapter responsibility.

## Scope

### In Scope
- Restructuring folders to `Adapters/Embedding`, `Adapters/SemanticIndex`, `Adapters/SemanticOutbox`, and `Shared/Resilience`.
- Moving files to match the new structure and updating their namespaces.
- Creating a unified `DependencyInjection.cs` in `Aura.Infrastructure`.
- Creating a dedicated `DependencyInjection.cs` in `Aura.Application` for Application-layer services.
- Updating `Aura.Workers/Program.cs` and test suites to consume the new unified DI methods.

### Out of Scope
- Modifying the underlying logic or behavior of the adapters.
- Updating external SDKs or NuGet packages.
- Any changes to the Domain layer.

## Capabilities

### New Capabilities
None

### Modified Capabilities
None (This is a structural refactor; no functional requirements or spec-level behaviors are changing.)

## Approach

**Adapter-centric Folders with Root DI**
1. Move existing files into the targeted folder model (`Adapters/` and `Shared/`).
2. Update all namespaces to reflect the new directory structure.
3. Centralize `IServiceCollection` extensions into `AddAuraInfrastructure` and `AddAuraApplication`.
4. Update consumers (`Aura.Workers` and test projects) to use the new unified extension methods.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Infrastructure/` | Modified | Reorganizes into `Adapters/` and `Shared/`. Consolidates DI registrations. |
| `src/Aura.Application/` | Modified | Adds a dedicated `DependencyInjection.cs` for its own services. |
| `src/Aura.Workers/` | Modified | Updates `Program.cs` to call the new unified DI extensions. |
| `tests/` | Modified | Adjusts `using` directives and DI setups for tests. |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Broken compilation from namespace changes | High | Rely on rigorous build checks and IDE refactoring tools. |
| Missing or incorrect DI registrations | Medium | Verify application startup and ensure all integration tests pass. |

## Rollback Plan

Revert the PR containing these structural changes. Because this is purely organizational with no logic changes, a standard `git revert` is sufficient.

## Dependencies

- None

## Success Criteria

- [ ] All infrastructure implementations are housed in `Adapters/` or `Shared/` folders.
- [ ] `Aura.Infrastructure` exposes a single `AddAuraInfrastructure` extension method.
- [ ] `Aura.Application` exposes a single `AddAuraApplication` extension method.
- [ ] Application services (e.g., `BasicSemanticChunkExtractor`) are correctly registered in the Application DI.
- [ ] The solution compiles without errors and all tests pass.
