## Exploration: Recover broken test baseline

### Current State
Unit tests are green (644 passing), but the baseline is still broken in integration and E2E:

- Many authenticated integration endpoints return `Unauthorized` instead of `OK`.
- E2E dashboard smoke tests fail because expected `data-testid` markers are missing from rendered HTML.
- Playwright bootstrap cannot reach the UI host at `https://localhost:5001` and fails with connection refused.

The earlier tech-debt items are already gone: there are no `UnitTest1.cs` placeholders, and `Aura.E2E.csproj` has a single `Microsoft.Playwright` reference. `Directory.Build.props` still has `TreatWarningsAsErrors=false`, so warnings are not the current blocker.

### Affected Areas
- `src/Aura.Api/Program.cs` — API auth/middleware pipeline and endpoint mounting.
- `src/Aura.Infrastructure/Adapters/Identity/DependencyInjection.cs` — JWT auth wiring that likely governs the 401s.
- `tests/Aura.IntegrationTests/Auth/AuthorizationFlowTests.cs` — proves mock-token auth is expected to work.
- `tests/Aura.IntegrationTests/Dashboard/*`, `tests/Aura.IntegrationTests/Sync/*`, `tests/Aura.IntegrationTests/GraphConnector/*` — currently returning 401.
- `tests/Aura.E2E/Dashboard/*` — HTML marker assertions are out of sync with rendered output.
- `tests/Aura.E2E/Browser/PlaywrightWebApplicationFactory.cs` — UI host startup and stubbed service wiring.
- `tests/Aura.E2E/Playwright/PlaywrightBootstrapTests.cs` — assumes a reachable UI host and stable shell markers.

### Approaches
1. **Fix auth/startup drift first** — restore the integration auth pipeline and UI host wiring, then re-run the baseline.
   - Pros: likely addresses the broadest failure class; improves confidence quickly.
   - Cons: may still leave UI contract mismatches after auth is fixed.
   - Effort: Medium

2. **Update the failing tests to the current UI contract** — align E2E selectors and expectations with the rendered markup.
   - Pros: fastest way to get red tests green if rendering changes are intended.
   - Cons: risky if it papers over a real regression in the host or API boundary.
   - Effort: Medium

3. **Split baseline recovery into two slices** — one slice for auth/host reliability, one for UI contract stabilization.
   - Pros: clearer verification, smaller review surface, easier rollback.
   - Cons: slower to fully restore the baseline.
   - Effort: Medium

### Recommendation
Start with auth/startup recovery, then stabilize the UI/E2E contracts. The 401s and host startup failure are more structural than selector drift, so they should be fixed before treating HTML expectations as the source of truth.

### Proposal question round
1. Should the recovery target be **full-suite green** or only the currently broken integration/E2E baseline?
2. Are the current `Unauthorized` results acceptable for any endpoint under the intended auth model, or are they regressions that must be restored to mock-token success?
3. Should the Playwright bootstrap remain a **real host** smoke test, or can it be narrowed to a local UI shell check until the host is stable?
4. Are the missing dashboard `data-testid` markers intended to come back, or has the UI contract changed and the tests need to be updated?
5. Is `TreatWarningsAsErrors=false` still intentional for this recovery phase, or should baseline recovery also include build strictness?

### Risks
- Fixing selectors before fixing auth can hide a deeper startup/auth regression.
- The UI smoke tests may be asserting an outdated contract if the rendered components changed recently.
- Playwright bootstrap failures may be environment-sensitive, so a local pass may not prove host reliability.

### Ready for Proposal
Yes — enough is known to draft a proposal. The next step should explicitly separate auth/host recovery from UI contract reconciliation.
