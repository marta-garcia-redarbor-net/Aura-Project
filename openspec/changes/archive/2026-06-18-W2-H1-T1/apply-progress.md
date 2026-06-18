# Apply Progress: W2-H1-T1

## Delivery
- Mode: Strict TDD
- Delivery strategy: `ask-always`
- Approved apply mode: single PR
- Work unit: 1 (full change scope)

## Completed Tasks
- [x] 1.1 RED: Added failing tests for mandatory constructor inputs in `WorkItemTests`
- [x] 1.2 RED: Added failing tests for sourceType closed set, normalization rules, and fixed schemaVersion
- [x] 1.3 RED: Updated registry-related tests to the new `WorkItem` contract shape
- [x] 2.1 Created `WorkItemSourceType` enum in Domain
- [x] 2.2 Created `WorkItemPriority` enum in Domain
- [x] 2.3 Extended `WorkItem` constructor with mandatory fields and argument guards
- [x] 2.4 Implemented normalization for `CorrelationId`, `CapturedAtUtc`, and fixed `SchemaVersion = "v1"`
- [x] 3.1 Updated Application kernel handling to work with expanded `WorkItem` contract context
- [x] 3.2 Updated `HelloKernelWorker` to pass full mandatory caller inputs and optional captured timestamp
- [x] 3.3 Updated worker structured logs with `ExternalId`, `SourceType`, `Priority`, `CorrelationId`
- [x] 4.1 Refactored test setup with a reusable valid `WorkItem` helper
- [x] 4.2 Verified Clean Architecture boundaries (Domain/Application remain framework-free)
- [x] 4.3 Aligned `WorkItem` naming/comments/shape with `work-item-contract` and `plugin-kernel` specs
- [x] 5.1 Executed `dotnet test Aura.sln`
- [x] 5.2 Executed `dotnet build Aura.sln`
- [x] 5.3 Added explicit spec-to-test mapping in `tasks.md`

## TDD Cycle Evidence
| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1 | `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` | Unit | ❌ Skipped baseline run before test edits | ✅ Wrote constructor field-guard tests first | ✅ Passed in targeted run and full suite | ✅ Multiple missing-input paths (externalId/title/source/metadata + enum invalid paths) | ✅ Added `CreateValidWorkItem` helper |
| 1.2 | `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` | Unit | ❌ Skipped baseline run before test edits | ✅ Wrote normalization/closed-set tests first | ✅ Passed in targeted run and full suite | ✅ Provided vs absent `correlationId`; provided vs absent `capturedAtUtc`; fixed schema assertion | ✅ Consolidated setup without weakening assertions |
| 1.3 | `tests/Aura.UnitTests/Kernel/PluginRegistryTests.cs` | Unit | ❌ Skipped baseline run before test edits | ✅ Updated tests to new constructor shape before production contract changes | ✅ Passed in targeted run and full suite | ✅ Registry scenarios already include success/empty/failure/abort paths | ✅ Added local `CreateWorkItem` helper |
| 2.1 | `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` | Unit | N/A (new enum file) | ✅ Enum referenced by RED tests before file existed | ✅ All tests compile/pass after enum creation | ➖ Structural (enum definition) | ➖ None needed |
| 2.2 | `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` | Unit | N/A (new enum file) | ✅ Enum referenced by RED tests before file existed | ✅ All tests compile/pass after enum creation | ➖ Structural (enum definition) | ➖ None needed |
| 2.3 | `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` | Unit | ✅ Relevant tests executed after constructor change | ✅ Constructor-behavior tests already red-defined | ✅ Guards now pass all mandatory-field tests | ✅ Diverse valid/invalid constructor paths | ✅ Introduced constants/properties in entity shape |
| 2.4 | `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` | Unit | ✅ Relevant tests executed after normalization change | ✅ Red tests for fallback/preservation/schema existed first | ✅ Fallback and preservation tests passing | ✅ both branches for each normalization rule covered | ✅ Constructor assignments kept minimal/clear |
| 3.1 | `tests/Aura.UnitTests/Kernel/PluginRegistryTests.cs` | Unit | ✅ Registry tests run after Application changes | ✅ Existing failing contract updates existed before green wiring | ✅ Registry tests and full suite pass | ✅ Error/success/empty/abort flows still pass | ✅ Structured log now includes contract fields |
| 3.2 | `tests/Aura.UnitTests/Kernel/HelloPluginTests.cs` + solution tests | Unit | ✅ Targeted kernel tests run after worker update | ✅ Contract update pressure came from RED constructor changes | ✅ All tests pass and solution compiles | ✅ Multiple worker item fields + nullable captured timestamp path | ✅ Worker creation block made explicit and self-contained |
| 3.3 | `tests/Aura.UnitTests/Kernel/PluginRegistryTests.cs` + solution tests | Unit | ✅ Post-change test execution | ✅ Logging expectations implied by contract fields and compile checks | ✅ Passing targeted and full suite runs | ➖ Logging-only behavioral surface in this slice | ✅ Message template aligned with required fields |
| 4.1 | `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` | Unit | ✅ Tests rerun after helper refactor | ✅ Refactor guarded by existing red-defined tests | ✅ No regressions in targeted/full runs | ✅ Helper used across constructor + transition scenarios | ✅ Reduced duplication while preserving assertions |
| 4.2 | `tests/Aura.ArchitectureTests/*.cs` + solution build/tests | Architecture + Build | ✅ `dotnet test Aura.sln` + `dotnet build Aura.sln` | ➖ Structural boundary validation | ✅ Architecture tests and build green | ➖ Boundary validation task | ✅ No SDK/framework leakage added to Domain/Application |
| 4.3 | `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` | Unit | ✅ Relevant tests rerun after naming/comment alignment | ➖ Alignment-only (no new behavior) | ✅ Tests remained green | ➖ Single-output structural alignment | ✅ Naming/comments aligned with spec vocabulary |
| 5.1 | `dotnet test Aura.sln` | Verification | N/A | ➖ Verification task | ✅ Passed (all test projects green) | ➖ Verification task | ➖ None |
| 5.2 | `dotnet build Aura.sln` | Verification | N/A | ➖ Verification task | ✅ Passed with 0 warnings/0 errors | ➖ Verification task | ➖ None |
| 5.3 | `openspec/changes/W2-H1-T1/tasks.md` | Artifact | N/A | ➖ Mapping/documentation task | ✅ Mapping section added | ➖ Single-output artifact task | ✅ Mapping is explicit and traceable |

## Test Summary
- Total tests written/updated in this change: 26+ (WorkItem/PluginRegistry/HelloPlugin focused)
- Targeted execution (kernel/work-item scope): `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~WorkItemTests|FullyQualifiedName~PluginRegistryTests|FullyQualifiedName~HelloPluginTests"` → **36 passed**
- Full suite execution: `dotnet test Aura.sln` → **all projects passed**
- Build verification: `dotnet build Aura.sln` → **success (0 warnings, 0 errors)**
- Layers used: Unit, Architecture, Integration (via full-suite verification)
- Approval tests: None (this was feature-contract expansion, not legacy behavior refactoring)
- Pure functions created: 0 (entity/worker contract update task)

## Notes
- Strict TDD caveat: Safety Net baseline was not run before modifying existing test files. This is explicitly recorded in TDD evidence.
- Clean Architecture guard: Domain and Application remain free of external provider SDK coupling.
