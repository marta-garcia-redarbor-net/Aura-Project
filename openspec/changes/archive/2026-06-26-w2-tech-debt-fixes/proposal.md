# Proposal: Week 2 Tech Debt Fixes

## Intent

Resolve 5 MUST-FIX blockers and 2 low-effort SHOULD-FIX items identified in the Week 2 health check. The project scores 8.5/10 architecturally with 637 tests, but these items block Week 3 (Deep Work & PRs) and Week 4 (Closure). The Morning Summary end-to-end flow is broken, duplicate packages cause restore instability, and real credentials are committed.

## Scope

### In Scope
- Remove duplicate Playwright reference in `Aura.E2E.csproj` (keep v1.54.0)
- Delete 4 placeholder `UnitTest1.cs` files across test projects
- Convert `ConnectorExecutionWorker` from one-shot to continuous polling service (currently kills host after single run, taking down all other workers)
- Wire `IMorningSummaryComposer` into `MorningSummarySchedulingWorker` (currently marks emission but never composes)
- Resolve NuGet v10.x packages vs .NET 9 SDK conflict
- Remove real Azure AD credentials from `.env`, use placeholders
- Set `TreatWarningsAsErrors=true` in `Directory.Build.props`
- Add API integration tests for auth middleware (401/200 validation)

### Out of Scope
- Major refactors (e.g., extracting telemetry decorator from `ExecuteConnectorUseCase`)
- New features or architectural changes
- Configurable multi-tenant support for Workers
- Playwright E2E test suite expansion

## Capabilities

> This section is the CONTRACT between proposal and specs phases.

### New Capabilities
None — all changes are fixes to existing capabilities.

### Modified Capabilities
- `connector-execution`: Worker MUST run as continuous polling service with configurable interval (currently one-shot — calls `_lifetime.StopApplication()` in `finally` block, killing all hosted services after single run)
- `morning-summary-scheduling`: Worker MUST invoke `IMorningSummaryComposer.ComposeAsync()` after scheduler determines emission is due (currently missing from flow)
- `api-authentication`: Integration tests MUST validate 401 for unauthenticated and 200 for authenticated requests (currently untested)

## Approach

**Phase 1 — Quick Wins (low risk, ~1h):**
1. Remove line 17 (`Microsoft.Playwright` v1.52.0) from `tests/Aura.E2E/Aura.E2E.csproj`
2. Delete `tests/Aura.UnitTests/UnitTest1.cs`, `tests/Aura.IntegrationTests/UnitTest1.cs`, `tests/Aura.ArchitectureTests/UnitTest1.cs`, `tests/Aura.E2E/UnitTest1.cs`
3. Replace real credentials in `.env` with placeholders (`YOUR_CLIENT_ID`, `YOUR_TENANT_ID`), verify `.gitignore` includes `.env`

**Phase 2 — Critical Worker Fixes (medium risk, ~3h):**

*Fix 2a — ConnectorExecutionWorker one-shot → continuous:*
1. Add `while (!stoppingToken.IsCancellationRequested)` loop with `Task.Delay(PollingInterval)` (same pattern as `MorningSummarySchedulingWorker`)
2. Remove `_lifetime.StopApplication()` from `finally` block — the host lifecycle should be managed by the runtime, not by a worker
3. Add configurable `PollingInterval` (default: 5 minutes for connector sync)
4. Scope resolution per iteration (currently creates one scope and disposes it — must create fresh scope per iteration to avoid stale dependencies)
5. Add unit tests: worker runs multiple iterations, worker handles cancellation gracefully, worker continues after adapter failure

*Fix 2b — MorningSummarySchedulingWorker composer wiring:*
1. Add `IMorningSummaryComposer` dependency to constructor
2. Call `ComposeAsync()` after `_emissionStore.MarkEmittedAsync()` when `isDue = true`
3. Add logging for composition success/failure
4. Write unit tests covering: composer called when due, composer not called when not due, composition failure logged but doesn't break worker loop

**Phase 3 — Dependency Resolution (medium risk, ~1h):**
1. Audit NuGet packages: `Microsoft.Extensions.AI` 10.6.0, `Microsoft.Extensions.Diagnostics.HealthChecks` 10.0.8, `Microsoft.Extensions.Resilience` 10.6.0
2. Downgrade to v9.x equivalents if available, or verify .NET 9 SDK compatibility
3. Run `dotnet restore` and `dotnet build` to confirm no conflicts

**Phase 4 — Quality Improvements (low risk, ~1h):**
1. Set `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in `Directory.Build.props`
2. Fix any compilation warnings that surface
3. Add integration tests in `Aura.IntegrationTests` verifying auth middleware behavior

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `tests/Aura.E2E/Aura.E2E.csproj` | Modified | Remove duplicate Playwright package reference |
| `tests/*/UnitTest1.cs` (4 files) | Removed | Delete placeholder test files |
| `.env` | Modified | Replace real credentials with placeholders |
| `src/Aura.Workers/ConnectorExecutionWorker.cs` | Modified | Convert from one-shot to continuous polling; remove `_lifetime.StopApplication()` |
| `src/Aura.Workers/MorningSummarySchedulingWorker.cs` | Modified | Wire composer into worker flow |
| `Directory.Build.props` | Modified | Enable TreatWarningsAsErrors |
| `tests/Aura.IntegrationTests` | Modified | Add auth middleware tests |
| `*.csproj` (affected by NuGet downgrade) | Modified | Package version adjustments |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| ConnectorExecutionWorker loop causes token refresh storms | Med | Polling interval configurable; MSAL AcquireTokenSilent handles caching; test with mock token provider |
| ConnectorExecutionWorker scope-per-iteration adds overhead | Low | ServiceScopeFactory is lightweight; scope is disposed each iteration (existing pattern in .NET workers) |
| Composer wiring breaks worker loop | Low | Unit tests cover due/not-due paths; composition failure logged, doesn't crash |
| NuGet downgrade introduces API breaks | Med | Build + full test suite after downgrade; pin exact versions |
| TreatWarningsAsErrors surfaces many warnings | Med | Fix warnings in same change; can revert to `false` if scope explodes |
| Auth tests require mock JWT setup | Low | Reuse existing `mock-login` endpoint from `api-authentication` spec |

## Rollback Plan

- **Phase 1**: Revert csproj, restore UnitTest1.cs from git history, restore `.env` credentials
- **Phase 2**: Revert `MorningSummarySchedulingWorker.cs` to pre-wiring state; composer remains tested independently
- **Phase 3**: Revert NuGet version changes in csproj files
- **Phase 4**: Set `TreatWarningsAsErrors` back to `false`; remove new test files

## Dependencies

- `IMorningSummaryComposer` and `MorningSummaryComposer` already exist in Application layer (verified)
- `IMorningSummaryEmissionStore` already injected in `MorningSummarySchedulingWorker` (verified)
- `ConnectorExecutionWorker` already injects `IServiceScopeFactory` and `IHostApplicationLifetime` — scope-per-iteration and loop conversion are structural changes only
- `IConnectorAdapter` already registered as transient/scoped via `AddAuraInfrastructure` — fresh scope per iteration resolves fresh adapters
- Mock JWT infrastructure exists from `api-authentication` spec

## Success Criteria

- [ ] `dotnet build Aura.sln` completes with zero warnings
- [ ] `dotnet test Aura.sln` passes all 637+ tests (no new failures)
- [ ] No duplicate Playwright references in any csproj
- [ ] No `UnitTest1.cs` files remain in test projects
- [ ] `.env` contains only placeholder credentials
- [ ] `ConnectorExecutionWorker` runs as continuous polling service (no `_lifetime.StopApplication()`)
- [ ] `ConnectorExecutionWorker` creates fresh DI scope per iteration
- [ ] `MorningSummarySchedulingWorker` calls `ComposeAsync()` when due
- [ ] Auth integration tests verify 401 → 200 flow
- [ ] No NuGet restore errors or version conflicts
