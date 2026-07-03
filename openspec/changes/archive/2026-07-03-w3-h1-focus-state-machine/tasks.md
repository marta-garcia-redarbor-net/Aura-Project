# Tasks: W3-H1 — Focus State Machine

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~250–350 |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | ask-always (user ok: "solo frena si hay dudas") |
| Chain strategy | single-pr |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: single-pr
400-line budget risk: Low

## Phase 1: Domain Foundation (FocusStateType + FocusState)

- [x] 1.1 Create `src/Aura.Domain/FocusState/FocusStateType.cs` — enum with `DeepWork`, `WindowOfOpportunity`, `Away`, `Recovery` (namespace `Aura.Domain.FocusState`, no xml-doc required beyond summary). Test: `FocusStateType_ContainsExactlyFourValues` verifies enum members.

- [x] 1.2 Create `src/Aura.Domain/FocusState/FocusState.cs` — sealed class following `WorkItem` pattern:
  - Starts in `WindowOfOpportunity` (private setter `CurrentState`)
  - 4 transition methods: `TryEnterDeepWork()`, `GoToWindowOfOpportunity()`, `GoToAway()`, `GoToRecovery()`
  - Each method guards with `InvalidOperationException` for disallowed transitions
  - Test: initial state, all 6 valid transitions, all invalid transitions per state

## Phase 2: Port & Resolver (Infrastructure Wiring)

- [x] 2.1 Create `src/Aura.Application/Ports/IFocusStateResolver.cs` — port interface with `Task<FocusState> ResolveAsync(string userId, CancellationToken ct)` (namespace `Aura.Application.Ports`, no infra deps per arch test). Test: `IFocusStateResolver_ExposesResolveAsync_MatchingSignature`, `IFocusStateResolver_HasNoInfrastructureDependency`.

- [x] 2.2 Create `src/Aura.Application/Services/FocusStateResolver.cs` — stub returns `FocusState` starting in `WindowOfOpportunity` for any userId. Pure function, no side effects. Test: `ResolveAsync_AnyUserId_ReturnsWindowOfOpportunity`, `ResolveAsync_SameInputs_SameState`.

- [x] 2.3 Modify `src/Aura.Application/DependencyInjection.cs` — add `services.AddScoped<IFocusStateResolver, FocusStateResolver>()` following existing pattern. Test: verify registration compiles and resolves.

## Phase 3: Tests (RED → GREEN → REFACTOR)

- [x] 3.1 Create `tests/Aura.UnitTests/Triage/FocusStateMachineTests.cs` — test class with:
  - `[Fact]` for initial state (`WindowOfOpportunity`)
  - `[Fact]` for each of the 6 valid transitions (state changes as expected)
  - `[Fact]` for each invalid path from each state (throws `InvalidOperationException`, state unchanged)
  - Test naming: `Method_Scenario_Expected` (e.g. `GoToAway_FromWindowOfOpportunity_ChangesState`)
  - No mocking framework — manual stubs, follow `MorningSummaryRankingPolicyTests.cs` pattern

- [x] 3.2 Add resolver tests to same file:
  - `ResolveAsync_AnyUserId_ReturnsWindowOfOpportunity` — verifies default stub
  - `ResolveAsync_SameInputs_SameState` — verifies determinism (2 calls, same result)
  - Architecture-style test: `IFocusStateResolver_HasNoInfrastructureDependency` — reflection check on namespace references

## Phase 4: Documentation

- [x] 4.1 Update `docs/architecture/triage/03-focus-state-machine.md` — change status from "Deferred / Out of Scope" to "Implemented", add section summarizing the 4 states, 6 transitions, the `IFocusStateResolver` port.
