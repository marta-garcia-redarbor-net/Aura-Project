# Apply Progress: Infrastructure Organization Refactor

**Change**: infrastructure-organization-refactor
**Mode**: Strict TDD
**Status**: 18/18 tasks complete + 2 corrective tasks (verify fix)

## Corrective Apply (Post-Verify Fix)

The initial apply completed all 18 tasks successfully. Verify failed due to:
1. Missing `TDD Cycle Evidence` artifact
2. Missing runtime test for spec scenario "Infrastructure file placement"
3. Missing negative DI test for `AddAuraInfrastructure` not registering `ISemanticChunkExtractor`

This corrective apply adds the missing tests and persists this evidence artifact.

---

## TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1 | `tests/Aura.UnitTests/Application/DependencyInjectionTests.cs` | Unit | N/A (new) | ✅ Written | ✅ Passed | ✅ 2 cases (type + lifetime) | ➖ None needed |
| 1.2 | — | Config | — | — | — | — | — |
| 1.3 | (covered by 1.1 tests) | Unit | N/A (new) | ✅ Written | ✅ Passed | ✅ 2 cases | ➖ None needed |
| 2.1 | `tests/Aura.UnitTests/Infrastructure/InfrastructureDependencyInjectionTests.cs` | Unit | N/A (new) | ✅ Written | ✅ Passed | ✅ 4 services (embedding, writer, retriever, outbox) | ➖ None needed |
| 2.2 | (covered by 2.1 + architecture tests) | Structural | ✅ 9/9 arch tests pre-passing | ✅ Written | ✅ Passed | ➖ Structural move | ➖ None needed |
| 2.3 | `tests/Aura.UnitTests/Infrastructure/InfrastructureDependencyInjectionTests.cs` | Unit | ✅ baseline passing | ✅ Written | ✅ Passed | ➖ Single rename | ➖ None needed |
| 2.4 | (covered by 2.1 + architecture tests) | Structural | ✅ 9/9 arch tests pre-passing | ✅ Written | ✅ Passed | ➖ Structural move | ➖ None needed |
| 2.5 | `tests/Aura.UnitTests/Infrastructure/InfrastructureDependencyInjectionTests.cs` | Unit | ✅ baseline passing | ✅ Written | ✅ Passed | ✅ 4 services resolved | ➖ None needed |
| 2.6 | (covered by 2.1 outbox assertion) | Structural | ✅ baseline passing | ✅ Written | ✅ Passed | ➖ Structural move | ➖ None needed |
| 2.7 | `tests/Aura.UnitTests/Infrastructure/InfrastructureDependencyInjectionTests.cs` | Unit | N/A (new) | ✅ Written | ✅ Passed | ➖ Single service | ➖ None needed |
| 2.8 | `tests/Aura.UnitTests/Infrastructure/InfrastructureDependencyInjectionTests.cs` | Unit | N/A (new) | ✅ Written | ✅ Passed | ✅ 4 services in one root | ➖ None needed |
| 3.1 | `tests/Aura.IntegrationTests/Workers/WorkersHostCompositionTests.cs` | Integration | ✅ 17/17 pre-passing | ✅ Written | ✅ Passed | ✅ Multiple services resolved | ➖ None needed |
| 3.2 | `tests/Aura.UnitTests/Infrastructure/MeaiEmbeddingProviderTests.cs` | Unit | ✅ 148/148 pre-passing | ➖ Existing tests | ✅ Passed | ➖ Namespace update only | ➖ None needed |
| 3.3 | `tests/Aura.UnitTests/VectorStore/DependencyInjectionTests.cs` | Unit | ✅ pre-passing | ➖ Existing tests | ✅ Passed | ➖ Scope narrowing | ➖ None needed |
| 3.4 | `tests/Aura.IntegrationTests/Workers/WorkersHostCompositionTests.cs` | Integration | ✅ 17/17 pre-passing | ✅ Written | ✅ Passed | ✅ Multiple services | ➖ None needed |
| 4.1 | (covered by architecture tests) | Structural | ✅ 9/9 pre-passing | ➖ Deletion only | ✅ Passed | ➖ None | ➖ None needed |
| 4.2 | — | Build | — | — | ✅ `dotnet build` → 0 errors | — | — |
| 4.3 | — | Suite | — | — | ✅ 175 passed / 0 failed | — | — |
| **Fix-A** | `tests/Aura.ArchitectureTests/InfrastructureOrganizationTests.cs` | Architecture | ✅ 9/9 arch tests pre-passing | ✅ Written (3 tests) | ✅ 3/3 passed | ✅ 3 cases (files in Adapters, no legacy folders, non-empty adapters) | ➖ None needed |
| **Fix-B** | `tests/Aura.UnitTests/Infrastructure/InfrastructureDependencyInjectionTests.cs` | Unit | ✅ 150/150 pre-passing | ✅ Written (2 tests) | ✅ 2/2 passed | ✅ 2 cases (descriptor null + resolution throws) | ➖ None needed |

## Test Summary

- **Total tests written (this change)**: ~20 new/modified test methods across the change
- **Total tests passing (full suite)**: 180
- **Layers used**: Unit (150), Architecture (12), Integration (17), E2E (1)
- **Approval tests** (refactoring): None — structural moves validated by existing test continuity
- **Pure functions created**: 0 (DI wiring and structural organization, no business logic)

## Corrective Fix Tests Detail

### Fix-A: Infrastructure File Placement (Spec: Adapter-Centric Organization)

**Test file**: `tests/Aura.ArchitectureTests/InfrastructureOrganizationTests.cs`

| Test Method | What It Proves |
|-------------|----------------|
| `InfrastructureSourceFiles_MustResideInAdaptersOrSharedFolders` | All subdirectory .cs files are under `Adapters/` or `Shared/` — no generic technical folders |
| `InfrastructureProject_MustNotContainLegacyGenericFolders` | Legacy `Embedding/`, `VectorStore/`, `Persistence/` directories do not exist at root |
| `InfrastructureAdapters_EachSubfolderMustContainSourceFiles` | Each adapter folder has real source — not empty scaffolding |

### Fix-B: Negative DI Leakage (Spec: Dedicated Application DI)

**Test file**: `tests/Aura.UnitTests/Infrastructure/InfrastructureDependencyInjectionTests.cs`

| Test Method | What It Proves |
|-------------|----------------|
| `AddAuraInfrastructure_DoesNotRegister_ISemanticChunkExtractor` | Descriptor-level: no `ISemanticChunkExtractor` in service collection after `AddAuraInfrastructure` |
| `AddAuraInfrastructure_ResolvingISemanticChunkExtractor_Throws` | Runtime-level: attempting to resolve throws `InvalidOperationException` — behavioral proof of absence |

## Files Changed (Corrective Fix)

| File | Action | What Was Done |
|------|--------|---------------|
| `tests/Aura.ArchitectureTests/InfrastructureOrganizationTests.cs` | Created | 3 architecture tests proving file placement spec scenario |
| `tests/Aura.UnitTests/Infrastructure/InfrastructureDependencyInjectionTests.cs` | Modified | Added 2 negative DI leakage tests |

## Deviations from Design

None — implementation matches design.

## Issues Found

None.

## Spec Compliance After Fix

| Requirement | Scenario | Test | Result |
|-------------|----------|------|--------|
| Adapter-Centric Organization | Infrastructure file placement | `InfrastructureOrganizationTests` (3 methods) | ✅ COMPLIANT |
| Unified Infrastructure DI | Registering infrastructure services | `InfrastructureDependencyInjectionTests` (4 positive + 2 negative) | ✅ COMPLIANT |
| Dedicated Application DI | Registering application services | `ApplicationDependencyInjectionTests` + negative DI proof | ✅ COMPLIANT |
