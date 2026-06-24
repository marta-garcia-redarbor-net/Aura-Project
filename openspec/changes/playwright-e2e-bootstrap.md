# Change: playwright-e2e-bootstrap

**Status**: proposal  
**Change name**: playwright-e2e-bootstrap  
**Target branch**: feat/playwright-e2e-bootstrap  
**Created**: 2026-06-24

---

## Intent

Bootstrap Playwright in Aura to prepare for browser-based end-to-end testing. This slice focuses on **tooling readiness and minimal deterministic validation** without waiting for Teams, Outlook, and Calendar real integrations to stabilize.

---

## Problem

Currently, `tests/Aura.E2E` contains only host-level smoke tests using `WebApplicationFactory`. These tests validate HTTP-rendered HTML and state, but do NOT catch:
- real browser rendering issues
- JavaScript/Blazor hydration problems
- CSS/layout visual regressions
- actual UI interactive behavior under controlled conditions

Delaying Playwright until integrations are production-ready means discovering browser integration problems late and incurring high setup cost when urgency is high.

---

## Scope

### In
- Configure Playwright in `tests/Aura.E2E` project
- Add one minimal browser E2E test for dashboard root route (`/`)
- Test validates shell visibility + loading → stable state transition
- Use controlled/mocked data to ensure determinism
- Capture failure artifacts (screenshots, traces)
- Preserve existing host-level smoke tests

### Out
- Deep journeys covering Teams, Outlook, Calendar flows (deferred until contracts stabilize)
- Multi-screen navigation flows
- Real external connector integration in browser tests
- Playwright configuration for CI/production (defer to W4-H2)

---

## Approach

1. **Add Playwright NuGet packages** to `tests/Aura.E2E.csproj`
2. **Create Playwright browser test class** under `tests/Aura.E2E/Browser/DashboardRootBrowserTests.cs`
3. **Set up minimal Playwright config** (local dev only)
4. **Implement one test case**:
   - Launch browser → `GET /`
   - Wait for shell to render (via stable `data-testid` selectors already present)
   - Wait for loading state to transition
   - Assert shell markers visible
   - Assert no console errors
   - Capture trace/screenshot on failure
5. **Keep host-level smoke tests unchanged** — they remain the fast feedback loop

---

## Success Criteria

- [ ] Playwright is installed and runnable via xUnit in `tests/Aura.E2E`
- [ ] One browser test passes against local dev UI with controlled data
- [ ] Test is stable (no flakes over 5 runs)
- [ ] Failure artifacts (trace/screenshot) are captured on error
- [ ] Existing smoke tests remain unchanged and passing
- [ ] Documentation notes when/how to run browser tests

---

## Risks

| Risk | Mitigation |
|------|-----------|
| Browser test flakiness from day one | Use controlled data only; avoid external deps; use stable selectors |
| Playwright setup complexity in .NET | Use Microsoft.Playwright NuGet; keep config minimal |
| Maintenance burden for one test | One test is low cost; increase coverage only after journeys stabilize |
| CI overhead if left unmanaged | This slice does NOT add to CI yet; verify locally only |

---

## Decision Tradeoffs

**Why NOT wait for integrations to be ready?**
- Setup cost is front-loaded; better to pay it early and incrementally add depth
- Catches browser-specific issues earlier
- Provides signal that tooling and selectors are stable

**Why NOT add deep journeys now?**
- Teams/Outlook/Calendar contracts are still evolving
- Without stable fixtures, tests become brittle and expensive to maintain
- Better to bootstrap, validate shell, then add business logic once data is deterministic

---

## Next Steps

1. **Spec phase**: Detail Playwright config, test structure, and xUnit integration
2. **Design phase**: Decide on test fixture/seeding strategy and artifact capture
3. **Tasks phase**: Break into implementable work units (NuGet, config, test, docs)
4. **Apply phase**: Execute implementation in worktree
5. **Verify phase**: Confirm all tests pass and artifacts are captured
6. **Archive phase**: Close change and prepare for W4-H2 deeper coverage

---

## Related

- **StoryBacklog W4-H2**: Consolidate suite Playwright (planned for week 4)
- **Current E2E state**: `tests/Aura.E2E` has host-level smoke tests only
- **Worktree**: `C:\Users\marta.garcia\source\repos\Aura-playwright` on branch `feat/playwright-e2e-bootstrap`
