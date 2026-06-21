# Apply Progress: W2-H4

## Change
- **Change**: W2-H4
- **Mode**: Strict TDD
- **Delivery**: `exception-ok` with maintainer-approved `size:exception` (single PR)
- **Scope of this batch**: Full implementation of all pending W2-H4 tasks

## Cumulative Task Status

### Phase 1: Foundation and Architecture Guard
- [x] 1.1 Created `OutlookEmailDto` with all required nullable fields in Infrastructure.
- [x] 1.2 Added `OutlookConnectorBoundaryTests` to prevent Outlook namespace leakage into Application/Domain.

### Phase 2: Mapper Multi-Signal Scoring (TDD)
- [x] 2.1 Added mapper RED tests for canonical mapping, missing `ExternalId` skip, missing `Subject` default + metadata.
- [x] 2.2 Added RED scoring tests for `Importance` high/normal/low/absent, sender signal, body signal, all-signals-absent, max-signals-critical.
- [x] 2.3 Implemented `OutlookWorkItemMapper.TryMap` + additive `ResolvePriority` thresholds with `Source="inbox"` and `SourceType=OutlookEmail`.
- [x] 2.4 Implemented deadline-cue scan (subject first, then body fallback) and always-on scoring metadata keys.
- [x] 2.5 Refactored mapper into private helpers for scoring and cue matching while preserving deterministic behavior.

### Phase 3: Adapter, Telemetry, and Wiring (TDD)
- [x] 3.1 Added RED adapter tests for all-valid success, mixed partial failure, default fixture-provider path.
- [x] 3.2 Implemented `OutlookConnectorAdapter` with continue-on-skip, mapped/skipped counters, and typed `ConnectorExecutionResult` mapping.
- [x] 3.3 Added source-generated logging with EventIds 3203 (summary) and 3204 (skipped item).
- [x] 3.4 Registered `OutlookConnectorAdapter` as scoped `IConnectorAdapter` and ensured mapper is DI-resolvable.

### Phase 4: Verification and Documentation
- [x] 4.1 Ran focused Outlook unit + architecture tests.
- [x] 4.2 Ran full gate `dotnet test Aura.sln`.
- [x] 4.3 Updated ingestion architecture overview with Outlook connector mapping and multi-signal scoring behavior.

### Remediation Pass (Post-Verify Warnings)
- [x] R1 Fixed `outlook.deadline.cue` mapping to persist matched-context excerpts (up to 100 chars) instead of token/date-only values, aligned with design contract.
- [x] R2 Added passing runtime coverage for previously uncovered mapper branches: unrecognized `Importance`, deadline body-fallback cue, and deadline date-pattern cue.

## TDD Cycle Evidence

| Task | Test File(s) | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|---|---|---|---|---|---|---|---|
| 1.1 | `tests/Aura.UnitTests/Ingestion/Outlook/OutlookEmailDtoTests.cs` | Unit | N/A (new) | ✅ DTO test written before DTO type existed (compile RED) | ✅ 1/1 passed after DTO creation | ➖ Structural task | ➖ None needed |
| 1.2 | `tests/Aura.ArchitectureTests/OutlookConnectorBoundaryTests.cs` | Architecture | N/A (new) | ✅ Boundary tests added before Outlook adapter wiring | ✅ 2/2 passed | ✅ Application + Domain paths covered | ➖ None needed |
| 2.1 | `tests/Aura.UnitTests/Ingestion/Outlook/OutlookWorkItemMapperTests.cs` | Unit | N/A (new) | ✅ Mapper tests written before mapper existed (compile RED) | ✅ Mapper tests passing after implementation | ✅ Valid + missing required + missing optional mapped | ✅ Helper extraction preserved assertions |
| 2.2 | `tests/Aura.UnitTests/Ingestion/Outlook/OutlookWorkItemMapperTests.cs` | Unit | N/A (new) | ✅ Scoring scenarios authored before logic | ✅ Scenario tests passed with additive thresholds | ✅ Independent + combined signals covered | ✅ Cue/score helpers extracted |
| 2.3 | `tests/Aura.UnitTests/Ingestion/Outlook/OutlookWorkItemMapperTests.cs` | Unit | N/A (new) | ✅ Priority threshold expectations defined first | ✅ `>=6/ >=2/ >=0/ <0` behavior passing | ✅ High/Medium/Low/Critical branches covered | ✅ Constants + helper methods |
| 2.4 | `tests/Aura.UnitTests/Ingestion/Outlook/OutlookWorkItemMapperTests.cs` | Unit | N/A (new) | ✅ Deadline + metadata key tests authored first | ✅ Deadline and scoring metadata assertions pass | ✅ Deadline hit/miss + scoring keys always written | ✅ Scanning isolated in dedicated methods |
| 2.5 | `tests/Aura.UnitTests/Ingestion/Outlook/OutlookWorkItemMapperTests.cs` | Unit | ✅ Focused mapper suite passing before refactor | ✅ Existing tests guarded behavior | ✅ Post-refactor suite still green | ✅ Multiple cue/weight permutations retained | ✅ Internal refactor complete |
| 3.1 | `tests/Aura.UnitTests/Ingestion/Outlook/OutlookConnectorAdapterTests.cs` | Unit | N/A (new) | ✅ Adapter tests written before adapter existed (compile RED) | ✅ 3/3 passing after adapter implementation | ✅ Success + partial failure + default fixtures covered | ✅ Kept adapter thin |
| 3.2 | `tests/Aura.UnitTests/Ingestion/Outlook/OutlookConnectorAdapterTests.cs` | Unit | N/A (new) | ✅ Status/failure assertions existed before implementation | ✅ Continue-on-skip behavior green | ✅ Mixed valid/invalid batch branch covered | ✅ No contract changes |
| 3.3 | `tests/Aura.UnitTests/Ingestion/Outlook/OutlookConnectorAdapterTests.cs` | Unit | ✅ Adapter tests passing before logging changes | ✅ Existing behavior tests guarded logging refactor | ✅ Adapter tests still green | ➖ Logging-specific assertions not required by spec | ✅ Source-generated log methods added |
| 3.4 | `tests/Aura.UnitTests/Infrastructure/InfrastructureDependencyInjectionTests.cs` | Unit | ✅ Existing DI tests passing before edit | ✅ Added failing Outlook DI registration test first | ✅ DI registration + mapper registration fixed; test passed | ✅ Registration presence + runtime resolution validated | ✅ No connector contract changes |
| 4.1 | Focused commands (unit + architecture) | Unit + Architecture | ✅ Prior RED/GREEN steps complete | ✅ N/A (verification task) | ✅ Focused suites green | ➖ Verification task | ✅ Logged outputs |
| 4.2 | `dotnet test Aura.sln` | All | ✅ Focused suites green first | ✅ N/A (verification task) | ✅ Full solution green | ➖ Suite-level | ✅ Regression gate completed |
| 4.3 | `docs/architecture/ingestion/00-overview.md` update | Documentation | ✅ N/A | ✅ Doc update created for new behavior | ✅ File updated with Outlook mapping/scoring details | ➖ Documentation task | ✅ Kept additive change minimal |
| R1 | `tests/Aura.UnitTests/Ingestion/Outlook/OutlookWorkItemMapperTests.cs` | Unit | ✅ 13/13 mapper safety-net passed before edits | ✅ Added context-excerpt assertions first; 3 tests failed on token/date-only behavior | ✅ Mapper updated and suite passed 16/16 | ✅ Subject cue + body fallback + date-pattern contexts covered | ✅ Added `ExtractCueContext` helper with bounded excerpt logic |
| R2 | `tests/Aura.UnitTests/Ingestion/Outlook/OutlookWorkItemMapperTests.cs` | Unit | ✅ Mapper suite green after R1 | ✅ Added unrecognized-importance and focused branch tests before final verification run | ✅ Focused branch run passed 3/3; full mapper suite remained 16/16 | ✅ Verified previously uncovered lines now hit in coverage report | ➖ No extra refactor required beyond R1 helper |

## Test Summary
- **Total tests written**: 23
- **Total tests passing**: 23 (new tests), plus prior full suite green
- **Layers used**: Unit, Architecture
- **Approval tests**: None — no legacy behavior replacement task
- **Pure functions / pure helpers created**: Mapper score/cue helper methods (subject, sender, body, deadline)

## Command Log

1. `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter FullyQualifiedName~Aura.UnitTests.Ingestion.Outlook.OutlookEmailDtoTests`
   - RED result: compile failure (`Outlook` namespace/type missing)
2. `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter FullyQualifiedName~Aura.UnitTests.Ingestion.Outlook.OutlookEmailDtoTests`
   - GREEN result: 1/1 passed
3. `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter FullyQualifiedName~Aura.UnitTests.Ingestion.Outlook.OutlookWorkItemMapperTests`
   - RED result: compile failure (`OutlookWorkItemMapper` missing)
4. `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter FullyQualifiedName~Aura.UnitTests.Ingestion.Outlook.OutlookConnectorAdapterTests`
   - RED result: compile failure (`OutlookConnectorAdapter` missing)
5. `dotnet test tests/Aura.ArchitectureTests/Aura.ArchitectureTests.csproj --filter FullyQualifiedName~Aura.ArchitectureTests.OutlookConnectorBoundaryTests`
   - GREEN result: 2/2 passed
6. `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter FullyQualifiedName~Aura.UnitTests.Ingestion.Outlook.OutlookWorkItemMapperTests|FullyQualifiedName~Aura.UnitTests.Ingestion.Outlook.OutlookConnectorAdapterTests|FullyQualifiedName~Aura.UnitTests.Ingestion.Outlook.OutlookEmailDtoTests`
   - GREEN result: 17/17 passed
7. `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter FullyQualifiedName~Aura.UnitTests.Infrastructure.InfrastructureDependencyInjectionTests.AddAuraInfrastructure_RegistersOutlookConnectorAdapter_AsIConnectorAdapter`
   - RED result: assertion failure (Outlook adapter not registered)
8. `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter FullyQualifiedName~Aura.UnitTests.Infrastructure.InfrastructureDependencyInjectionTests.AddAuraInfrastructure_RegistersOutlookConnectorAdapter_AsIConnectorAdapter`
   - RED result: runtime failure (`OutlookWorkItemMapper` not resolvable)
9. `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter FullyQualifiedName~Aura.UnitTests.Infrastructure.InfrastructureDependencyInjectionTests.AddAuraInfrastructure_RegistersOutlookConnectorAdapter_AsIConnectorAdapter`
   - GREEN result: 1/1 passed
10. `graphify update .`
   - Result: graph rebuilt successfully
11. `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter OutlookWorkItemMapperTests|OutlookConnectorAdapterTests`
   - Result: 16/16 passed
12. `dotnet test tests/Aura.ArchitectureTests/Aura.ArchitectureTests.csproj --filter OutlookConnectorBoundaryTests`
   - Result: 2/2 passed
13. `dotnet test Aura.sln`
    - Result: full suite passed (Unit 326, Architecture 27, Integration 55, E2E 21)
14. `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter FullyQualifiedName~Aura.UnitTests.Ingestion.Outlook.OutlookWorkItemMapperTests`
    - Safety Net result: 13/13 passed before remediation edits
15. `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter FullyQualifiedName~Aura.UnitTests.Ingestion.Outlook.OutlookWorkItemMapperTests`
    - RED result: 3 failures (`TryMap_SubjectDeadlineCue_WritesDeadlineMetadata`, `TryMap_SubjectWithoutDeadline_BodyDeadlineCue_FallsBackToBodyWithContextExcerpt`, `TryMap_DeadlineDatePattern_StoresContextExcerptInsteadOfDateTokenOnly`) proving cue-context contract mismatch
16. `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter FullyQualifiedName~Aura.UnitTests.Ingestion.Outlook.OutlookWorkItemMapperTests`
    - GREEN result: 16/16 passed after cue-context implementation
17. `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~TryMap_UnrecognizedImportance_WithSenderSignal_UsesRemainingSignals|FullyQualifiedName~TryMap_SubjectWithoutDeadline_BodyDeadlineCue_FallsBackToBodyWithContextExcerpt|FullyQualifiedName~TryMap_DeadlineDatePattern_StoresContextExcerptInsteadOfDateTokenOnly"`
    - GREEN result: 3/3 passed (targeted branch coverage)
18. `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter OutlookWorkItemMapperTests`
    - GREEN result: 16/16 passed
19. `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --collect:"XPlat Code Coverage" --filter OutlookWorkItemMapperTests`
    - GREEN result: 16/16 passed; coverage report generated at `tests/Aura.UnitTests/TestResults/9a85a70e-03c4-4752-9220-ec33706bd658/coverage.cobertura.xml`
20. `graphify update .`
    - Result: graph rebuilt successfully after remediation

## Remediation Coverage Evidence
- `coverage.cobertura.xml` for `OutlookWorkItemMapper.cs`: line-rate `0.9885`, branch-rate `0.95`.
- Previously uncovered warning lines now hit:
  - `L141` (unrecognized importance): `hits="1"`
  - `L190-L193` (body fallback deadline metadata writes): `hits="2"`
  - `L213-L215` (date-pattern branch): `L213 hits="26"` + `L214-L215 hits="1"`

## Key Files Changed
- `src/Aura.Infrastructure/Adapters/Connectors/Outlook/OutlookEmailDto.cs`
- `src/Aura.Infrastructure/Adapters/Connectors/Outlook/OutlookWorkItemMapper.cs`
- `src/Aura.Infrastructure/Adapters/Connectors/Outlook/OutlookConnectorAdapter.cs`
- `src/Aura.Infrastructure/Adapters/Connectors/DependencyInjection.cs`
- `src/Aura.Infrastructure/Adapters/WorkItems/DependencyInjection.cs`
- `tests/Aura.UnitTests/Ingestion/Outlook/OutlookEmailDtoTests.cs`
- `tests/Aura.UnitTests/Ingestion/Outlook/OutlookWorkItemMapperTests.cs`
- `tests/Aura.UnitTests/Ingestion/Outlook/OutlookConnectorAdapterTests.cs`
- `tests/Aura.ArchitectureTests/OutlookConnectorBoundaryTests.cs`
- `tests/Aura.UnitTests/Infrastructure/InfrastructureDependencyInjectionTests.cs`
- `docs/architecture/ingestion/00-overview.md`
- `openspec/changes/W2-H4/tasks.md`

## Verification Notes
- All W2-H4 tasks are checked in `tasks.md`.
- Outlook/Graph-related types remain inside Infrastructure; architecture boundary tests pass.
- No contract change to `IConnectorAdapter`, `WorkItem`, or persistence ports.

## Rollback Notes
- Revert W2-H4 commit(s) to remove Outlook DTO/mapper/adapter, DI registrations, tests, and documentation updates.
- No schema migration or external state mutation introduced in this slice.
