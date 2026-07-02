# Proposal: Recover Broken Test Baseline

## Intent

The full test suite is not green. Unit tests pass (644), but authenticated
integration endpoints return `401`, E2E dashboard smoke tests fail on missing
`data-testid` markers, and Playwright cannot reach the UI host at
`https://localhost:5001`. Recent login/auth and layout refactors were
intentional, so working code is the source of truth. We must restore green
without regressing that intended behavior — and stop this class of breakage
from recurring.

## Scope

### In Scope
- Restore integration auth so mock-token requests return `OK` (fix genuine drift, not the contract).
- Repair Playwright host startup/wiring so the UI shell is reachable.
- Reconcile E2E selectors/markers with the current rendered UI where the change was intentional.
- Add forward guardrails so future refactors preserve or consciously migrate test contracts.

### Out of Scope
- Enabling `TreatWarningsAsErrors=true` (not the current blocker).
- New product features or UI redesign.
- Reverting intentional auth/layout refactors.

## Capabilities

### New Capabilities
- `test-baseline-guardrails`: Rules requiring future refactors to preserve the mock-auth integration contract and stable E2E selectors, or migrate them within the same change.

### Modified Capabilities
- None. Recovery realigns code and tests with existing `api-authentication`, `test-cleanup`, and dashboard specs; no requirement text changes.

## Approach

Two-slice vertical recovery (backlog-slicer). **Slice 1 — structural reliability:**
diagnose and fix the 401s and Playwright host startup as genuine regressions
(auth wiring, middleware order, host binding). **Slice 2 — contract
reconciliation:** where markup/selectors drifted from intended UI, adapt tests
to working code; only restore a marker if its absence is an unintended
regression. Decision rule per failure: *regression → fix code; intentional
refactor fallout → adapt test.* Clean Architecture boundaries stay intact (auth
via Infrastructure adapters, no domain leakage).

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Api/Program.cs` | Modified | Auth/middleware pipeline order |
| `src/Aura.Infrastructure/Adapters/Identity/DependencyInjection.cs` | Modified | JWT wiring behind 401s |
| `tests/Aura.IntegrationTests/**` | Modified | Restore mock-token success |
| `tests/Aura.E2E/**` | Modified | Host startup + selector reconciliation |
| `openspec/specs/test-baseline-guardrails/` | New | Future-refactor guardrails |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Adapting selectors hides a real regression | Med | Fix Slice 1 first; classify each failure |
| Playwright pass is environment-sensitive | Med | Assert host reachability explicitly |
| Guardrails add process friction | Low | Keep rules minimal and verifiable |

## Rollback Plan

Changes are test-and-wiring scoped. Revert per slice via its PR; no data or
schema migrations. Reverting restores the prior (red) baseline safely.

## Dependencies

- Local UI host runnable for Playwright bootstrap.

## Success Criteria

- [ ] Full suite green: unit, integration, E2E.
- [ ] Mock-token integration requests return `OK`.
- [ ] Playwright reaches the UI host and passes smoke checks.
- [ ] `test-baseline-guardrails` spec defines contract-preservation rules.
- [ ] No intentional auth/layout refactor is reverted.
