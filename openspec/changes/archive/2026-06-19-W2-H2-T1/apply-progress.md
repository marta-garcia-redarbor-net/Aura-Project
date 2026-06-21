# Implementation Progress

**Change**: W2-H2-T1  
**Mode**: Strict TDD

### Completed Tasks
- [x] 1.1 Create `tests/Aura.UnitTests/Ingestion/CheckpointIdentityTests.cs` with failing theory tests for null/empty `Connector`, `Source`, and `Tenant` guards.
- [x] 1.2 Add failing tests for `IngestionCheckpoint` nullable fields and structural equality.
- [x] 1.3 Create `tests/Aura.ArchitectureTests/IngestionArchitectureTests.cs` with failing NetArchTest rules for Provider Isolation and port placement.
- [x] 2.1 Create `src/Aura.Application/Models/CheckpointIdentity.cs` as `sealed record` with constructor guards.
- [x] 2.2 Create `src/Aura.Application/Models/IngestionCheckpoint.cs` as `sealed record` with `Cursor` and `ProcessedAt`.
- [x] 2.3 Create `src/Aura.Application/Ports/IIngestionCheckpointStore.cs` with `GetAsync` and `SaveAsync` signatures.
- [x] 2.4 Add XML contract text for first-run today-only behavior and non-persistence requirement.
- [x] 3.1 Update `docs/architecture/ingestion/05-normalization-checkpoints.md` with identity/value shape.
- [x] 3.2 Document first-run caller responsibility and adapter deferral to W2-H2-T2/T3.
- [x] 3.3 Add observability deferral note for this contract-only slice.
- [x] 4.1 Make RED tests pass without Infrastructure/SDK leakage into Application.
- [x] 4.2 Refactor duplicated assertion pattern in `CheckpointIdentityTests.cs`.
- [x] 4.3 Run `dotnet test Aura.sln` successfully.
- [x] 5.1 Create `openspec/changes/W2-H2-T1/apply-progress.md` with RED/GREEN/REFACTOR evidence for apply and remediation tasks.
- [x] 5.2 Add a 9-scenario spec-to-test ledger in `apply-progress.md` with test names and pass status.
- [x] 6.1 Create `tests/Aura.UnitTests/Ingestion/Fakes/InMemoryIngestionCheckpointStore.cs` with dictionary replace-on-save semantics.
- [x] 6.2 Create `tests/Aura.UnitTests/Ingestion/Support/IngestionCheckpointCallerHarness.cs` applying UTC-today window on `GetAsync == null`.
- [x] 6.3 Inject deterministic `Func<DateTimeOffset> utcNow` into the harness for stable first-run assertions.
- [x] 7.1 Create `tests/Aura.UnitTests/Ingestion/InMemoryCheckpointStoreContractTests.cs` for identity independence and replacement on same identity.
- [x] 7.2 In `InMemoryCheckpointStoreContractTests.cs`, add `Get` tests for stored checkpoint and null on unknown identity.
- [x] 7.3 In `InMemoryCheckpointStoreContractTests.cs`, add save→get tests for value round-trip and null-field preservation.
- [x] 7.4 Create `tests/Aura.UnitTests/Ingestion/IngestionCheckpointFirstRunWindowTests.cs` for null-checkpoint today-window and existing-checkpoint bypass.
- [x] 8.1 Run new ingestion runtime suites, then `dotnet test Aura.sln --collect:"XPlat Code Coverage"`; append evidence in `apply-progress.md`.
- [x] 8.2 Update `openspec/changes/W2-H2-T1/verify-report.md` so untested scenarios map to passing runtime tests.
- [x] 8.3 Link remediation tasks in `tasks.md` to `apply-progress.md` and final verification evidence.

### TDD Cycle Evidence
| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1 | `tests/Aura.UnitTests/Ingestion/CheckpointIdentityTests.cs` | Unit | N/A (new) | ✅ Written (missing `CheckpointIdentity`) | ✅ Passed (`dotnet test ...CheckpointIdentityTests`) | ✅ 2 cases per field (null/empty) | ✅ Helper extracted in 4.2 |
| 1.2 | `tests/Aura.UnitTests/Ingestion/CheckpointIdentityTests.cs` | Unit | N/A (new) | ✅ Written (missing `IngestionCheckpoint`) | ✅ Passed after model creation | ✅ null/null + structural equality scenario | ✅ Parse value consolidated with invariant culture |
| 1.3 | `tests/Aura.ArchitectureTests/IngestionArchitectureTests.cs` | Architecture | N/A (new) | ✅ Written (missing `IIngestionCheckpointStore`) | ✅ Passed after port creation | ✅ namespace placement + no Infrastructure/SDK dependencies | ➖ None needed |
| 2.1 | `tests/Aura.UnitTests/Ingestion/CheckpointIdentityTests.cs` | Unit | ✅ RED baseline captured from compile failure | ✅ Existing RED test drove model creation | ✅ Passed after adding guarded record | ✅ constructor valid + 3 guarded parts | ➖ Covered by existing tests |
| 2.2 | `tests/Aura.UnitTests/Ingestion/CheckpointIdentityTests.cs` | Unit | ✅ RED baseline captured from compile failure | ✅ Existing RED test drove model creation | ✅ Passed after adding checkpoint record | ✅ null fields + equality behavior | ➖ Covered by existing tests |
| 2.3 | `tests/Aura.ArchitectureTests/IngestionArchitectureTests.cs` | Architecture | ✅ RED baseline captured from compile failure | ✅ Existing RED architecture test drove port creation | ✅ Passed after adding port | ✅ placement + dependency isolation checks | ➖ Covered by existing tests |
| 2.4 | `tests/Aura.ArchitectureTests/IngestionArchitectureTests.cs` | Architecture | ✅ Prior architecture tests green before doc comments update | ✅ Behavior constrained by RED architecture contract | ✅ Passed with XML contract in interface | ➖ Single contract text behavior | ➖ None needed |
| 3.1 | N/A (docs task) | Documentation | N/A | ✅ Spec-driven doc assertions written via task criteria | ✅ Verified by manual content check | ➖ Single doc behavior | ✅ Placeholder removed, structure clarified |
| 3.2 | N/A (docs task) | Documentation | N/A | ✅ Spec/design requirement translated into doc text | ✅ Verified by manual content check | ➖ Single doc behavior | ➖ None needed |
| 3.3 | N/A (docs task) | Documentation | N/A | ✅ Task requirement written into doc | ✅ Verified by manual content check | ➖ Single doc behavior | ➖ None needed |
| 4.1 | `CheckpointIdentityTests.cs` + `IngestionArchitectureTests.cs` | Unit + Architecture | ✅ Focused test runs before/after implementation | ✅ Initially failed due missing types | ✅ Both focused suites passed | ✅ Multiple scenarios retained | ✅ No provider leakage introduced |
| 4.2 | `tests/Aura.UnitTests/Ingestion/CheckpointIdentityTests.cs` | Unit | ✅ Focused test baseline green before refactor | ✅ Approval-style preservation via existing assertions | ✅ Focused suite remained green | ➖ Single refactor target | ✅ `AssertInvalidIdentityPart` extracted |
| 4.3 | `Aura.sln` | Full suite | ✅ Targeted suites green before full run | ✅ Full-run gate required by task | ✅ `dotnet test Aura.sln` passed (all projects) | ➖ Single verification task | ➖ None needed |
| 5.1 | `openspec/changes/W2-H2-T1/apply-progress.md` | Documentation | ✅ Existing apply evidence loaded from Engram (#1849) | ✅ New remediation evidence structure added first | ✅ Artifact now present and auditable | ➖ Single artifact behavior | ✅ Merged previous + new task evidence |
| 5.2 | `openspec/changes/W2-H2-T1/apply-progress.md` | Documentation | ✅ Existing spec/test state baseline from `verify-report.md` | ✅ 9-scenario ledger drafted before remediation test files existed | ✅ Ledger now maps all scenarios to passing runtime tests | ✅ 9 scenarios covered | ✅ Names normalized to concrete test methods |
| 6.1 | `tests/Aura.UnitTests/Ingestion/InMemoryCheckpointStoreContractTests.cs`, `tests/Aura.UnitTests/Ingestion/Fakes/InMemoryIngestionCheckpointStore.cs` | Unit | N/A (new) | ✅ Runtime contract tests written first against missing fake store | ✅ Passed after fake store implemented | ✅ independent identities + replace-on-save behaviors | ✅ In-memory store kept minimal with dictionary semantics |
| 6.2 | `tests/Aura.UnitTests/Ingestion/IngestionCheckpointFirstRunWindowTests.cs`, `tests/Aura.UnitTests/Ingestion/Support/IngestionCheckpointCallerHarness.cs` | Unit | N/A (new) | ✅ First-run window tests written before harness existed | ✅ Passed after harness implementation | ✅ missing checkpoint path + existing checkpoint bypass | ✅ Returned `IngestionFetchPlan` value object for explicit assertions |
| 6.3 | `tests/Aura.UnitTests/Ingestion/IngestionCheckpointFirstRunWindowTests.cs`, `tests/Aura.UnitTests/Ingestion/Support/IngestionCheckpointCallerHarness.cs` | Unit | ✅ First-run tests green baseline prior to deterministic clock refactor | ✅ Deterministic-time assertions authored first | ✅ Passed with injected `Func<DateTimeOffset>` | ✅ two fixed timestamps in tests | ✅ Constructor requires deterministic clock delegate |
| 7.1 | `tests/Aura.UnitTests/Ingestion/InMemoryCheckpointStoreContractTests.cs` | Unit | ✅ New file baseline: compile failed before fake implementation | ✅ identity/replacement tests authored first | ✅ Passed after fake store save/get logic | ✅ distinct identity + same identity replacement | ✅ Invariant culture parsing for deterministic timestamps |
| 7.2 | `tests/Aura.UnitTests/Ingestion/InMemoryCheckpointStoreContractTests.cs` | Unit | ✅ Existing contract tests green before adding get scenarios | ✅ get-present/get-missing tests added first | ✅ Passed after store behavior confirmed | ✅ stored value path + unknown identity path | ➖ None needed |
| 7.3 | `tests/Aura.UnitTests/Ingestion/InMemoryCheckpointStoreContractTests.cs` | Unit | ✅ Existing contract tests green before round-trip additions | ✅ value-shape round-trip tests authored first | ✅ Passed with current store semantics | ✅ full value + null cursor preservation | ✅ Explicit assertions on cursor null and timestamp equality |
| 7.4 | `tests/Aura.UnitTests/Ingestion/IngestionCheckpointFirstRunWindowTests.cs` | Unit | ✅ Existing harness tests green before scenario expansion | ✅ first-run and bypass tests authored first | ✅ Passed after harness finalized | ✅ null checkpoint applies UTC-today + existing checkpoint bypass | ➖ None needed |
| 8.1 | `Aura.sln` + focused ingestion suites | Unit + Full suite | ✅ Focused ingestion suites passed before full runner | ✅ coverage gate required by task | ✅ `dotnet test Aura.sln --collect:"XPlat Code Coverage"` passed (364 tests) | ✅ focused + full-run triangulation | ➖ None needed |
| 8.2 | `openspec/changes/W2-H2-T1/verify-report.md` | Documentation | ✅ previous verification baseline was FAIL with 6 untested scenarios | ✅ remediation evidence requirements captured before report edit | ✅ report now maps 9/9 scenarios to passing tests | ✅ matrix aligns each scenario with concrete test | ✅ verdict updated to reflect runtime proof |
| 8.3 | `openspec/changes/W2-H2-T1/tasks.md` | Documentation | ✅ remediation tasks baseline unchecked | ✅ linkage text added with explicit artifact paths | ✅ all remediation checkboxes now checked | ➖ Single linkage behavior | ➖ None needed |

### 9-Scenario Spec-to-Test Ledger
| Requirement | Scenario | Test Name | File | Status |
|-------------|----------|-----------|------|--------|
| Checkpoint Identity | Independent checkpoints per distinct identity | `SaveAndGet_KeepsCheckpointsIndependent_PerDistinctIdentity` | `tests/Aura.UnitTests/Ingestion/InMemoryCheckpointStoreContractTests.cs` | ✅ PASS |
| Checkpoint Identity | Save replaces checkpoint on same identity | `Save_ReplacesCheckpoint_WhenIdentityMatches` | `tests/Aura.UnitTests/Ingestion/InMemoryCheckpointStoreContractTests.cs` | ✅ PASS |
| Checkpoint Value Shape | Full value is stored and returned unchanged | `SaveAndGet_RoundTripFullValue_Unchanged` | `tests/Aura.UnitTests/Ingestion/InMemoryCheckpointStoreContractTests.cs` | ✅ PASS |
| Checkpoint Value Shape | Null fields are preserved | `SaveAndGet_PreservesNullFields` | `tests/Aura.UnitTests/Ingestion/InMemoryCheckpointStoreContractTests.cs` | ✅ PASS |
| Checkpoint Read-Write Operations | Get returns stored checkpoint | `Get_ReturnsStoredCheckpoint_WhenIdentityExists` | `tests/Aura.UnitTests/Ingestion/InMemoryCheckpointStoreContractTests.cs` | ✅ PASS |
| Checkpoint Read-Write Operations | Get returns null for unknown identity | `Get_ReturnsNull_WhenIdentityDoesNotExist` | `tests/Aura.UnitTests/Ingestion/InMemoryCheckpointStoreContractTests.cs` | ✅ PASS |
| First-Run Bounded Initial Window | No checkpoint → caller applies today-only window | `ResolveFetchPlanAsync_AppliesUtcTodayWindow_WhenCheckpointIsMissing` | `tests/Aura.UnitTests/Ingestion/IngestionCheckpointFirstRunWindowTests.cs` | ✅ PASS |
| First-Run Bounded Initial Window | Existing checkpoint → today-only window is not applied | `ResolveFetchPlanAsync_BypassesUtcTodayWindow_WhenCheckpointExists` | `tests/Aura.UnitTests/Ingestion/IngestionCheckpointFirstRunWindowTests.cs` | ✅ PASS |
| Provider Isolation | Contract references only Application or BCL types | `IngestionCheckpointStore_Port_ShouldResideInApplicationPortsNamespace`; `IngestionCheckpointStore_Port_ShouldNotReferenceInfrastructureOrProviderSdkTypes` | `tests/Aura.ArchitectureTests/IngestionArchitectureTests.cs` | ✅ PASS |

### Test Summary
- **Total tests written**: 19 overall for this change scope (11 initial + 8 remediation runtime)
- **Total tests passing**: 364 in full suite (`267 Unit + 55 Integration + 21 Architecture + 21 E2E`)
- **Layers used**: Unit, Architecture
- **Approval tests (refactoring)**: Reused focused ingestion test set for task 4.2
- **Pure functions created**: 0

### Test Execution Evidence
- Focused runtime suites (remediation):
  - `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~Aura.UnitTests.Ingestion.InMemoryCheckpointStoreContractTests|FullyQualifiedName~Aura.UnitTests.Ingestion.IngestionCheckpointFirstRunWindowTests"`
  - Result: **8 passed, 0 failed**
- Full-suite authoritative run:
  - `dotnet test Aura.sln --collect:"XPlat Code Coverage"`
  - Result: **364 passed, 0 failed**

### Files Changed
- `tests/Aura.UnitTests/Ingestion/Fakes/InMemoryIngestionCheckpointStore.cs` (created)
- `tests/Aura.UnitTests/Ingestion/Support/IngestionCheckpointCallerHarness.cs` (created)
- `tests/Aura.UnitTests/Ingestion/InMemoryCheckpointStoreContractTests.cs` (created)
- `tests/Aura.UnitTests/Ingestion/IngestionCheckpointFirstRunWindowTests.cs` (created)
- `openspec/changes/W2-H2-T1/tasks.md` (updated checkboxes)
- `openspec/changes/W2-H2-T1/verify-report.md` (updated remediation verification mapping)
- `openspec/changes/W2-H2-T1/apply-progress.md` (created)

### Deviations from Design
None — implementation matches design.

### Issues Found
None.

### Remaining Tasks
None.

### Workload / PR Boundary
- Mode: single PR
- Current work unit: remediation batch phases 5-8
- Boundary: implemented only previously unchecked tasks 5.1-8.3
- Estimated review budget impact: within low-risk forecast (~170-260 lines)

### Status
25/25 tasks complete. Ready for verify.
