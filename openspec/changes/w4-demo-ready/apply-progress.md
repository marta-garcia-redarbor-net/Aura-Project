# Apply Progress: w4-demo-ready

## Run Metadata

- Date: 2026-07-10
- Mode: Strict TDD
- Delivery: single-pr-default with `size:exception`
- Scope in this batch: Phase 5.2 verification unblock attempts + demo decision-path DI hardening + explicit advisor chat config contract + EF migration startup validation on real dev DB path

## Task Status (Cumulative)

- Completed: 22 / 23
- Pending: 1 / 23
- Pending task: **5.2** Run `dotnet build Aura.sln` and `dotnet test Aura.sln --collect:"XPlat Code Coverage"`; fix failures in the same slice.

## Batch Work Completed

1. Added a regression test proving Demo Mode must not override an already-registered `IDecisionContextRetriever` implementation.
2. Updated Demo Mode DI to use `TryAddScoped` for `IDecisionContextRetriever`, preserving Qdrant-backed retrieval when already configured.
3. Added LLM advisor DI tests for disabled mode (null advisor) and enabled Ollama mode (real advisor + `IChatClient` registration).
4. Added `IChatClient` registration in LLM advisor DI for enabled mode, with safe fallback client when provider config is not usable.
5. Re-ran required build/test command for task 5.2 and captured remaining blockers.
6. Corrected advisor runtime configuration contract so chat model selection is explicit (`LlmAdvisor:ModelId`) and no longer coupled to embedding deployment configuration.
7. Added local/dev and production-oriented config surfaces for advisor runtime and documented manual REAL LLM+Qdrant demo steps.
8. Hardened EF migration integration cleanup to handle SQLite file locks (`ClearAllPools` + WAL/SHM deletion retries) and stabilize test teardown.
9. Verified API startup from clean `src/Aura.Api/aura-ef-test.db` applies baseline + trace migrations and creates required tables (`WorkItems`, `InterruptionDecisions`, `__EFMigrationsHistory`).

## TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 5.2 (demo retrieval override guard) | `tests/Aura.UnitTests/Demo/DemoModeRegistrationTests.cs` | Unit | ✅ baseline demo registration tests executed | ✅ Added failing test `AddDemoMode_WhenEnabled_DoesNotOverrideExistingDecisionContextRetriever` | ✅ Passed after DI change in `DemoModeServiceCollectionExtensions` | ✅ Existing enabled/disabled registration tests + new preserve-registration case | ➖ None needed |
| 5.2 (LLM advisor runtime activation DI) | `tests/Aura.UnitTests/Adapters/LlmAdvisor/DependencyInjectionTests.cs` | Unit | N/A (new file) | ✅ Added disabled/enabled DI expectation tests first | ✅ Passed after `IChatClient` DI registration in LLM advisor dependency injection | ✅ Disabled path + enabled Ollama path | ➖ None needed |
| 5.2 (advisor chat contract correctness) | `tests/Aura.UnitTests/Adapters/LlmAdvisor/DependencyInjectionTests.cs` | Unit | ✅ Existing advisor DI tests green before edits | ✅ Added failing settings-resolution tests first (no embedding model reuse) | ✅ Passed after explicit chat config resolution + `ModelId` requirement | ✅ Missing-model and explicit-model cases both covered | ➖ None needed |
| 5.2 (EF migration test teardown reliability) | `tests/Aura.IntegrationTests/Persistence/EfSchemaInitializerMigrationTests.cs` | Integration | ✅ Existing migration tests present | ✅ Coverage run exposed deterministic teardown failure (`IOException` file lock on temp sqlite DB) | ✅ Passed after adding `SqliteConnection.ClearAllPools()` + retry deletion for DB/WAL/SHM artifacts | ✅ Re-ran focused test filter for both migration scenarios | ➖ None needed |

### Test Summary

- Total tests written/updated: 6
- Targeted tests passing: 12/12 (`DemoModeRegistrationTests` + `LlmAdvisor.DependencyInjectionTests` + `EfSchemaInitializerMigrationTests`)
- Full solution build: ✅ passing
- Full solution coverage run: ❌ failing (see blockers below)
- Approval tests: None (no pure refactor-only task)
- Pure functions created: 0

## Verification Attempts for Task 5.2

- `dotnet build Aura.sln` → **PASS**
- `dotnet test Aura.sln --collect:"XPlat Code Coverage"` → **FAIL**

### Remaining Blockers (Not fully resolved in this slice)

1. **E2E host reachability/timeouts** in Playwright factory (`HostNotReachable`, 5s timeout).
2. **Integration unauthorized regressions** across endpoints expecting authenticated behavior (multiple `Expected: OK, Actual: Unauthorized`).
3. **Existing/parallel UI test failures** unrelated to the targeted DI fixes (`RestrictedAccessView`, Pull Requests page tests).

### EF Startup Migration Validation (Real Dev Path)

- Command: `dotnet run --project src/Aura.Api --no-build` with `ASPNETCORE_ENVIRONMENT=Development` after deleting `src/Aura.Api/aura-ef-test.db`.
- Observed startup logs: migrations `20260710110000_InitialCreateBaseline` and `20260710120000_AddInterruptionDecisionTraceColumns` were applied in order.
- Post-run DB inspection (sqlite):
  - Tables include `WorkItems`, `InterruptionDecisions`, `__EFMigrationsHistory`.
  - Migration history contains both baseline + trace migration IDs.

## Design/Spec Alignment Notes

- Preserving pre-registered `IDecisionContextRetriever` in demo mode aligns with the requirement that semantic retrieval must participate in decision-time flow and degrade safely.
- Registering `IChatClient` for enabled advisor mode addresses the open design question about advisor runtime activation and keeps deterministic fallback behavior intact.

## Files Changed in This Batch

- `src/Aura.Infrastructure/DemoModeServiceCollectionExtensions.cs`
- `src/Aura.Infrastructure/Adapters/LlmAdvisor/DependencyInjection.cs`
- `src/Aura.Infrastructure/Adapters/LlmAdvisor/LlmAdvisorOptions.cs`
- `tests/Aura.UnitTests/Demo/DemoModeRegistrationTests.cs`
- `tests/Aura.UnitTests/Adapters/LlmAdvisor/DependencyInjectionTests.cs`
- `src/Aura.Api/appsettings.Development.json`
- `src/Aura.Api/appsettings.json`
- `.env.example`
- `docs/architecture/triage/00-overview.md`
- `tests/Aura.IntegrationTests/Persistence/EfSchemaInitializerMigrationTests.cs`

## Current Status

**Partial / Blocked on task 5.2** — targeted demo/LLM DI issues were fixed with tests, but full required coverage command still fails due to broader integration/E2E/auth regressions outside the narrowly-fixed paths.
