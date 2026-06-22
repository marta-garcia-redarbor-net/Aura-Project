# Apply Progress: W2-H5-T2 Morning Summary Ranking Policy Implementation

## Workload / Delivery Context

- Delivery strategy: `ask-always`
- Maintainer-approved mode: `size:exception`
- Review forecast context: `400-line budget risk: High`, chained PR recommended by forecast, but single PR explicitly approved by maintainer.
- Boundary for this apply batch: full W2-H5-T2 implementation (Phases 1-4) in one approved exception batch.

## Task Status

### Phase 1: Foundation (TDD RED)

- [x] 1.1 Create `tests/Aura.UnitTests/Triage/MorningSummaryRankingPolicyTests.cs` with failing tests for explicit order scenarios: Deadline resolves first, then Impact, then Risk.
- [x] 1.2 In the same test file, add failing scenarios for single `PreliminaryScore` usage (post-explicit tie + all-explicit-absent fallback) and forbid split-rule behavior.
- [x] 1.3 Add failing tests for deterministic tie chain (nearest due date → oldest `CreatedAt` → lexical `ExternalId`) and `insufficient-signals` last with empty `RankingExplanation.Contributions`.
- [x] 1.4 Create `tests/Aura.UnitTests/Triage/MorningSummaryComposerTests.cs` with failing tests proving composer returns ordered entries and per-item explanations aligned to ranking output.

### Phase 2: Core Policy Implementation (TDD GREEN)

- [x] 2.1 Modify `src/Aura.Application/Models/RankingFactor.cs` to add `PreliminaryScore` while preserving existing factors.
- [x] 2.2 Create `src/Aura.Application/Models/WorkItemSignalKeys.cs` with constants for `outlook.deadline.cue`, `outlook.deadline.source`, `outlook.scoring.totalScore`, and `teams.priority.raw`.
- [x] 2.3 Create `src/Aura.Application/Ports/IMorningSummaryRankingPolicy.cs` with deterministic `Rank(IReadOnlyList<WorkItem>)` contract returning `IReadOnlyList<RankedWorkItem>`.
- [x] 2.4 Create `src/Aura.Application/UseCases/MorningSummary/MorningSummaryRankingPolicy.cs` implementing spec decision order, single preliminary-score input, tie chain, and structured explanations.

### Phase 3: Composer Wiring & DI (TDD GREEN)

- [x] 3.1 Create `src/Aura.Application/UseCases/MorningSummary/MorningSummaryComposer.cs` that reads via `IWorkItemReader`, calls `IMorningSummaryRankingPolicy`, and returns `MorningSummary` entries.
- [x] 3.2 Update `src/Aura.Application/DependencyInjection.cs` to register `IMorningSummaryRankingPolicy` and `IMorningSummaryComposer` without introducing Infrastructure dependencies.
- [x] 3.3 Update `tests/Aura.UnitTests/Application/DependencyInjectionTests.cs` and `tests/Aura.UnitTests/Triage/MorningSummaryComposerTests.cs` to make DI/composer-path tests pass.

### Phase 4: Architecture Guard & Verification (TDD REFACTOR)

- [x] 4.1 Modify `tests/Aura.ArchitectureTests/MorningSummaryArchitectureTests.cs` to assert `MorningSummaryRankingPolicy` resides in `Aura.Application` and not connectors/Infrastructure.
- [x] 4.2 Add architecture/test assertion that ranking path references no AI prioritization port or implementation.
- [x] 4.3 Run `dotnet test Aura.sln` and record evidence for ranking scenarios, composer wiring, and architecture boundaries in PR verification notes.

## TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1 | `tests/Aura.UnitTests/Triage/MorningSummaryRankingPolicyTests.cs` | Unit | ✅ `12/12` targeted baseline (`DependencyInjectionTests` + `MorningSummaryContractTests`) | ✅ Added deadline/impact/risk ordering tests first (missing production types failed compile) | ✅ Pass via `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~Aura.UnitTests.Triage.MorningSummaryRankingPolicyTests"` | ✅ Multiple explicit-order scenarios included | ✅ Policy extraction and helper structure after green |
| 1.2 | `tests/Aura.UnitTests/Triage/MorningSummaryRankingPolicyTests.cs` | Unit | N/A (same file in active RED cycle) | ✅ Added preliminary-score tie + fallback scenarios before implementation | ✅ Pass in same targeted policy run | ✅ Two contexts covered with one factor (`post-explicit`, `fallback`) | ✅ Kept single `RankingFactor.PreliminaryScore` path |
| 1.3 | `tests/Aura.UnitTests/Triage/MorningSummaryRankingPolicyTests.cs` | Unit | N/A (same file in active RED cycle) | ✅ Added tie-chain and insufficient-signals tests first | ✅ Pass in targeted policy run after tie-chain implementation | ✅ Due-date, created-at, lexical-id, and insufficient branches all covered | ✅ Comparator cleanup + snapshot helper refactor |
| 1.4 | `tests/Aura.UnitTests/Triage/MorningSummaryComposerTests.cs` | Unit | ✅ Existing triage tests baseline included in initial safety net | ✅ Added composer ordering/explanation tests before composer implementation | ✅ Pass via `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~Aura.UnitTests.Triage.MorningSummaryComposerTests"` | ✅ Two behavior scenarios (ordering + explanation alignment) | ✅ Constructor/reader helper cleanup |
| 2.1 | `tests/Aura.UnitTests/Triage/MorningSummaryRankingPolicyTests.cs` | Unit | N/A (new enum value for tests already in RED) | ✅ Tests referenced `RankingFactor.PreliminaryScore` first | ✅ Enum update made tests compile/pass | ➖ Structural enum extension | ➖ None needed |
| 2.2 | `tests/Aura.UnitTests/Triage/MorningSummaryRankingPolicyTests.cs` | Unit | N/A (new constants file) | ✅ Tests referenced missing key constants first | ✅ Added `WorkItemSignalKeys` constants; tests pass | ➖ Structural constants + multiple key usages in scenarios | ➖ None needed |
| 2.3 | `tests/Aura.UnitTests/Triage/MorningSummaryRankingPolicyTests.cs` | Unit | N/A (new port) | ✅ Tests instantiated missing policy namespace/type before contract existed | ✅ Added `IMorningSummaryRankingPolicy`; compilation and tests pass | ➖ Single contract behavior | ➖ None needed |
| 2.4 | `tests/Aura.UnitTests/Triage/MorningSummaryRankingPolicyTests.cs` | Unit | N/A (new implementation) | ✅ Production class absent during RED | ✅ Implemented ranking policy to satisfy all scenario tests | ✅ Additional failing passes used to force risk and tie-chain logic | ✅ Snapshot model and comparer isolated for maintainability |
| 3.1 | `tests/Aura.UnitTests/Triage/MorningSummaryComposerTests.cs` | Unit | N/A (new implementation file) | ✅ Composer tests created before implementation | ✅ Implemented composer and got green in targeted run | ✅ Two composer behaviors validated | ✅ Added optional reader fallback to preserve host DI stability |
| 3.2 | `tests/Aura.UnitTests/Application/DependencyInjectionTests.cs` | Unit | ✅ Baseline included in initial safety net | ✅ Added DI registration assertions first | ✅ DI tests pass after registration updates | ✅ Added resolution scenario without `IWorkItemReader` registration | ✅ Constructor strategy adjusted to keep integration host boot valid |
| 3.3 | `tests/Aura.UnitTests/Application/DependencyInjectionTests.cs`, `tests/Aura.UnitTests/Triage/MorningSummaryComposerTests.cs` | Unit | ✅ Existing files baseline captured | ✅ Updated failing assertions/tests first | ✅ Pass in targeted unit run (`24/24`) | ✅ Multiple DI + composer-path assertions | ✅ Removed transient no-op reader approach; kept port contract test intact |
| 4.1 | `tests/Aura.ArchitectureTests/MorningSummaryArchitectureTests.cs` | Architecture | ✅ Initial architecture safety net `2/2` for morning-summary tests | ✅ Added failing residence assertion first | ✅ Pass in targeted architecture run (`4/4`) | ✅ Port + implementation namespace checks | ✅ Assertion grouping cleanup |
| 4.2 | `tests/Aura.ArchitectureTests/MorningSummaryArchitectureTests.cs` | Architecture | ✅ Same architecture baseline | ✅ Added failing AI-boundary assertions first | ✅ Pass in targeted architecture run (`4/4`) | ✅ Two AI boundary checks (port string + namespace dependency) | ✅ Kept rules aligned with clean-architecture guard |
| 4.3 | `Aura.sln` test suites | Unit/Integration/Architecture/E2E | ✅ Targeted and focused gates run before full suite | ✅ Full-suite gate treated as final verification test requirement | ✅ `dotnet test Aura.sln` => `349/349 Unit`, `55/55 Integration`, `31/31 Architecture`, `21/21 E2E` | ➖ Full-suite verification task | ➖ None needed |

## Test Execution Log

1. Safety net before modifications:
   - `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~Aura.UnitTests.Application.DependencyInjectionTests|FullyQualifiedName~Aura.UnitTests.Triage.MorningSummaryContractTests"`
   - `dotnet test tests/Aura.ArchitectureTests/Aura.ArchitectureTests.csproj --filter "FullyQualifiedName~Aura.ArchitectureTests.MorningSummaryArchitectureTests"`
2. RED gate proof (expected fail due to missing production code/types):
   - `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~Aura.UnitTests.Triage.MorningSummaryRankingPolicyTests|FullyQualifiedName~Aura.UnitTests.Triage.MorningSummaryComposerTests"`
3. GREEN/TRIANGULATE loops (targeted):
   - Repeated targeted unit runs for ranking and composer tests.
   - Targeted run for DI + triage + architecture groups.
4. Full verification:
   - `dotnet test Aura.sln` (all suites green).

## Test Summary

- **Total tests written**: 15 new tests
  - `MorningSummaryRankingPolicyTests`: 7
  - `MorningSummaryComposerTests`: 2
  - `DependencyInjectionTests` additions: 3
  - `MorningSummaryArchitectureTests` additions: 3
- **Total tests passing (final full run)**:
  - Unit: 349/349
  - Integration: 55/55
  - Architecture: 31/31
  - E2E: 21/21
- **Layers used**: Unit + Architecture (plus full-suite integration/e2e verification gate)
- **Approval tests (refactoring)**: None — no legacy behavior-preserving refactor task requested
- **Pure functions created**: Ranking-policy signal parsing/scoring helpers are side-effect free

## Files Changed

- `src/Aura.Application/Models/RankingFactor.cs` (modified)
- `src/Aura.Application/Models/WorkItemSignalKeys.cs` (created)
- `src/Aura.Application/Ports/IMorningSummaryRankingPolicy.cs` (created)
- `src/Aura.Application/UseCases/MorningSummary/MorningSummaryRankingPolicy.cs` (created)
- `src/Aura.Application/UseCases/MorningSummary/MorningSummaryComposer.cs` (created)
- `src/Aura.Application/DependencyInjection.cs` (modified)
- `tests/Aura.UnitTests/Triage/MorningSummaryRankingPolicyTests.cs` (created)
- `tests/Aura.UnitTests/Triage/MorningSummaryComposerTests.cs` (created)
- `tests/Aura.UnitTests/Application/DependencyInjectionTests.cs` (modified)
- `tests/Aura.ArchitectureTests/MorningSummaryArchitectureTests.cs` (modified)
- `openspec/changes/W2-H5-T2/tasks.md` (modified, all tasks marked complete)

## Notes / Deviations

- To keep `IWorkItemReader` contract test (`MorningSummaryContractTests`) valid while preserving host DI stability, `MorningSummaryComposer` includes a ranking-policy-only constructor and a constructor with optional reader path. When no `IWorkItemReader` is available, composer deterministically returns an empty ranked list.
- `WorkItem.CreatedAt` is immutable and system-generated; tie-chain deterministic test uses reflection to set `CreatedAt` for controlled ordering assertions.
