# Tasks: W2-H5-T1 Morning Summary Contracts

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~150–220 (small records, 3 thin ports, 2 test files) |
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
| 1 | Morning Summary contracts (ports + DTOs + contract/architecture tests) | PR 1 | Single cohesive contract slice; tests included; targets main |

## Phase 1: DTO Contracts (Foundation)

- [x] 1.1 Create `src/Aura.Application/Models/RankingFactor.cs` enum (`Impact`, `Deadline`, `Risk`).
- [x] 1.2 Create `src/Aura.Application/Models/RankingExplanation.cs` + `RankingFactorContribution` record (factor, value, rationale).
- [x] 1.3 Create `src/Aura.Application/Models/MorningSummaryWindow.cs` (window date, timezone id, local time, UTC instant).
- [x] 1.4 Create `src/Aura.Application/Models/RankedWorkItem.cs` (rank, `WorkItem`, score, explanation).
- [x] 1.5 Create `src/Aura.Application/Models/MorningSummary.cs` (user, window, generated UTC, ordered entries).
- [x] 1.6 Create `MorningSummaryRequest.cs`, `MorningSummaryScheduleContext.cs`, `MorningSummaryQuery.cs` records.

## Phase 2: Ports

- [x] 2.1 Create `src/Aura.Application/Ports/IWorkItemReader.cs` (`ReadForWindowAsync(MorningSummaryQuery, CancellationToken)`) — no adapter.
- [x] 2.2 Create `src/Aura.Application/Ports/IMorningSummaryScheduler.cs` (`ResolveWindow`, `IsWindowDue`).
- [x] 2.3 Create `src/Aura.Application/Ports/IMorningSummaryComposer.cs` (`ComposeAsync(MorningSummaryRequest, CancellationToken)`).

## Phase 3: Testing (TDD — write RED first, before Phases 1–2 compile)

- [x] 3.1 RED: `tests/Aura.UnitTests/Triage/MorningSummaryContractTests.cs` — assert `MorningSummary` exposes ordered, non-null entries; each entry exposes rank/item/score/explanation (spec: Summary Payload Shape).
- [x] 3.2 RED: assert `RankingExplanation` lists factor contributions and Impact/Deadline/Risk are representable (spec: Ranking Explanation Shape).
- [x] 3.3 RED: assert empty work-item set yields a valid summary with empty, non-null entries (spec: Empty Window Handling).
- [x] 3.4 RED: define a test-only fake `IMorningSummaryComposer` returning a valid payload; assert it satisfies the port (spec: Composer Port).
- [x] 3.5 RED: `tests/Aura.ArchitectureTests/MorningSummaryArchitectureTests.cs` — ports carry no `Aura.Infrastructure`/UI/SDK dependency, mirroring `DashboardArchitectureTests` (spec: Contract Purity).
- [x] 3.6 GREEN: implement Phases 1–2 until 3.1–3.5 pass via `dotnet test Aura.sln`.
- [x] 3.7 REFACTOR: align naming/XML-doc with existing ports; re-run `dotnet test Aura.sln`.

## Phase 4: Verification

- [x] 4.1 Run `dotnet build Aura.sln` — confirm no new analyzer warnings on the contract files.
- [x] 4.2 Run `dotnet test Aura.sln` — all contract + architecture tests green.
- [x] 4.3 Confirm no adapter implements `IWorkItemReader` and nothing is registered in DI (Decision A).
