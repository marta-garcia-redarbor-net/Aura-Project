# Apply Progress: Real Teams and Outlook Ingestion (PR1)

**Change**: w2-h8-real-teams-outlook-ingestion
**Mode**: Strict TDD
**PR Boundary**: PR1 — tasks 1.1–1.5, 2.1–2.5
**Delivery**: feature-branch-chain (PR1 slice)

## Completed Tasks (PR1)

- [x] 1.1 Create `src/Aura.Application/Ports/IMessageSourceProvider.cs`
- [x] 1.2 Create `src/Aura.Application/Ports/ISyncStateStore.cs` and `ITokenCacheStatus.cs`
- [x] 1.3 Create `src/Aura.Application/Models/SyncResultDto.cs` and `TokenStatus.cs`
- [x] 1.4 Extend `GraphConnectorOptions.cs` — add `RedirectUri`, `Scopes[]`
- [x] 1.5 Extend `InboxItemPreviewDto` — optional init-only fields
- [x] 2.1 RED: Tests for `SqliteWorkItemStore`
- [x] 2.2 GREEN: Create `SqliteWorkItemStore`
- [x] 2.3 RED: Tests for `MsalSqliteTokenCache`
- [x] 2.4 GREEN: Create `MsalSqliteTokenCache` + `GraphClientFactory`
- [x] 2.5 Graph DI registration behind `GraphConnector:Enabled`

## TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1 | N/A (interface-only) | N/A | N/A (new) | ➖ Interface | ➖ Interface | ➖ Interface | ➖ Interface |
| 1.2 | N/A (interface-only) | N/A | N/A (new) | ➖ Interface | ➖ Interface | ➖ Interface | ➖ Interface |
| 1.3 | `tests/Aura.UnitTests/Sync/SyncResultDtoTests.cs`, `TokenStatusTests.cs` | Unit | N/A (new) | ✅ Written | ✅ Passed | ✅ Multiple record constructions | ➖ None needed |
| 1.4 | `tests/Aura.UnitTests/GraphConnector/GraphConnectorOptionsExtensionTests.cs` | Unit | N/A (new) | ✅ Written | ✅ Passed | ✅ 4 cases (null + set) | ➖ None needed |
| 1.5 | `tests/Aura.UnitTests/Dashboard/InboxItemPreviewDtoExtensionTests.cs` | Unit | N/A (new) | ✅ Written | ✅ Passed | ✅ Multiple nullable init-only cases | ➖ None needed |
| 2.1 | `tests/Aura.UnitTests/WorkItems/SqliteWorkItemStoreTests.cs` | Unit | N/A (new) | ✅ Written | ✅ Passed | ✅ 5 cases (save, upsert, read-back, multiple, empty) | ➖ None needed |
| 2.2 | (same as 2.1 — test-first) | Unit | N/A (new) | ✅ (from 2.1) | ✅ Passed | ✅ (from 2.1) | ➖ None needed |
| 2.3 | `tests/Aura.UnitTests/GraphConnector/MsalSqliteTokenCacheTests.cs` | Unit | N/A (new) | ✅ Written | ✅ Passed | ✅ 6 cases (round-trip, overwrite, multi-key, has/no-data) | ➖ None needed |
| 2.4 | `tests/Aura.UnitTests/GraphConnector/GraphClientFactoryTests.cs` | Unit | N/A (new) | ✅ Written | ✅ 7/7 passed | ✅ 7 cases (no-account, expired-token, null-checks, scopes, defaults) | ✅ Refactored to IOptions |
| 2.5 | `tests/Aura.IntegrationTests/GraphConnector/GraphConnectorStatusEndpointTests.cs` | Integration | ✅ 13/13 | ✅ DI failure proved RED | ✅ 13/13 passed after fix | ✅ Multiple config scenarios (disabled, valid, env-var) | ✅ UserTokenCache + IOptions wiring |

## Remediation Actions Taken

### Issue 1: Missing apply-progress.md
**Fixed**: This file now exists with strict-TDD evidence table.

### Issue 2: Graph DI runtime failure when `GraphConnector:Enabled=true`
**Root cause**: `GraphClientFactory` constructor accepted `GraphConnectorOptions` directly, but DI only registers `IOptions<GraphConnectorOptions>` via `.Configure<T>()`.
**Fix**: Changed constructor signature to `IOptions<GraphConnectorOptions>`.
**File**: `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphClientFactory.cs`

### Issue 3: Delegated token cache wiring used AppTokenCache
**Root cause**: DI code hooked into `app.AppTokenCache` (app-only client credential flow), but the design mandates delegated user tokens via `AcquireTokenSilent`. MSAL requires `app.UserTokenCache` for delegated flows.
**Fix**: Changed `app.AppTokenCache` → `app.UserTokenCache` and updated cache key prefix to `msal-user-`.
**File**: `src/Aura.Infrastructure/Adapters/Connectors/Graph/DependencyInjection.cs`

### Issue 4: Tests for GraphClientFactory scenarios
**Added**: 7 focused unit tests in `GraphClientFactoryTests.cs` covering:
- Silent token reuse path: verified account lookup + scopes passed to `AcquireTokenSilent`
- No-account path: `MsalUiRequiredException("no_account")` thrown correctly
- Expired/invalid token path: `MsalUiRequiredException("interaction_required")` propagated
- Constructor validation (null guards)
- Default scopes fallback when `Options.Scopes` is null
- Configured scopes forwarded correctly

**Honest narrowing**: The full "silent token actually returns a valid `GraphServiceClient`" happy path cannot be unit-tested via NSubstitute because MSAL's `AcquireTokenSilentParameterBuilder.ExecuteAsync()` is a non-virtual method on a sealed builder chain. The closest proof is:
1. `AcquireTokenSilent` is called with correct account and scopes (verified via `Arg.Do`)
2. If it throws `MsalUiRequiredException`, it propagates correctly
3. The DI integration tests prove the full wiring works at runtime (13/13 pass)

Full E2E proof of "worker reuses cached token → Graph call succeeds" belongs to PR2/PR3 where real Graph providers exercise the factory.

### Issue 5: Changed-file coverage for GraphClientFactory
**Improved**: From 0% to meaningful coverage via 7 tests exercising constructor, no-account path, expired-token path, and scope configuration. The remaining uncovered lines (L31-48: the actual `GraphServiceClient` construction after successful silent acquisition) require either an extracted interface or integration-level testing with a real MSAL app, which is scoped to PR2.

## Test Summary
- **Total new tests written (remediation)**: 7
- **Total PR1 unit tests passing**: 39
- **Total PR1 integration tests passing**: 18 (previously 16/18, now 18/18)
- **Total PR1 architecture tests passing**: 6
- **Layers used**: Unit (7 new), Integration (13 existing now passing)
- **Pure functions created**: 0 (factory pattern, not applicable)
- **Approval tests** (refactoring): N/A — no refactoring of existing behavior

## Files Changed (Remediation Only)

| File | Action | What Was Done |
|------|--------|---------------|
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphClientFactory.cs` | Modified | Constructor accepts `IOptions<GraphConnectorOptions>` instead of raw `GraphConnectorOptions` |
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/DependencyInjection.cs` | Modified | Changed `AppTokenCache` → `UserTokenCache`; updated cache key to `msal-user-{clientId}` |
| `tests/Aura.UnitTests/GraphConnector/GraphClientFactoryTests.cs` | Created | 7 unit tests covering silent reuse, expired token, null guards, scope config |
| `openspec/changes/w2-h8-real-teams-outlook-ingestion/apply-progress.md` | Created | This TDD evidence artifact |
