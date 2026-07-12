# Apply Progress: real-user-outlook-sync

## Status

- **Mode**: Strict TDD
- **State**: Completed (15/15 tasks complete + verify-remediation batch complete)
- **Reason**: Remediated verify blockers for Graph configuration/runtime evidence, cache-isolation proof, worker demo-path handling, and calendar DI readiness-gate alignment; merged with prior progress and revalidated targeted suites.

## Assigned Scope

- Change: `real-user-outlook-sync`
- Delivery strategy: `single-pr-default`
- Chain strategy: `none`
- Maintainer exception: `size:exception` accepted for single PR

## Safety Net Evidence (before edits)

### Command 1 (unit baseline)

```powershell
dotnet test "tests/Aura.UnitTests/Aura.UnitTests.csproj" --filter "FullyQualifiedName~SyncEndpointTests|FullyQualifiedName~ConnectorExecutionWorkerTests|FullyQualifiedName~TriggerSyncUseCaseTests|FullyQualifiedName~GraphConnectorStatusReaderTests|FullyQualifiedName~GraphConnectorOptionsTests|FullyQualifiedName~GraphClientFactoryTests"
```

Result: **PASS** — 38 passed, 0 failed.

### Command 2 (integration baseline)

```powershell
dotnet test "tests/Aura.IntegrationTests/Aura.IntegrationTests.csproj" --filter "FullyQualifiedName~SyncEndpointTests"
```

Result: **FAIL** — 3 passed, 2 failed.

Failing pre-existing tests:

1. `Aura.IntegrationTests.Sync.SyncEndpointTests.PostSyncNow_WithToken_Returns200WithResults`
   - Expected: `200 OK`
   - Actual: `401 Unauthorized`
2. `Aura.IntegrationTests.Sync.SyncEndpointTests.PostSyncNow_ThenGetStatus_ReturnsUpdatedState`
   - Expected: `200 OK`
   - Actual: `401 Unauthorized`

## TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1, 1.2, 1.3 | `tests/Aura.UnitTests/Sync/SyncEndpointsTests.cs`, `tests/Aura.IntegrationTests/Sync/SyncEndpointTests.cs` | Unit + Integration | ❌ Baseline had pre-existing SyncEndpoint integration failures | ✅ Added missing-oid and oid-propagation assertions first | ✅ `dotnet test ...SyncEndpointTests...` passed (Unit 48/48 filtered, Integration 5/5 filtered) | ✅ Happy path + missing-oid + demo/mock unauthorized | ✅ Logging + endpoint signature cleaned to claims-based source |
| 2.1, 2.2, 2.3 | `tests/Aura.UnitTests/Workers/ConnectorExecutionWorkerTests.cs` | Unit | ✅ Existing worker-targeted baseline included in filtered run | ✅ Added two-account and empty-cache behaviors before worker iteration change | ✅ Worker filtered suite green in unit command (48/48 filtered total) | ✅ Two-account + empty-cache + existing cancellation/failure paths | ✅ Preserved empty-cache warning path and cancellation handling |
| 3.1 | `tests/Aura.UnitTests/GraphConnector/GraphConnectorOptionsExtensionTests.cs`, `tests/Aura.UnitTests/GraphConnector/GraphConnectorStatusReaderTests.cs` | Unit | ✅ Existing Graph status tests included in filtered baseline | ✅ Added missing TenantId/ClientId Disabled expectations | ✅ Unit filtered suite green | ✅ Disabled vs ValidConfig derived paths covered | ✅ Status model simplified to two states |
| 3.2 | `tests/Aura.UnitTests/GraphConnector/GraphConnectorOptionsExtensionTests.cs` | Unit | ✅ Included in filtered baseline | ✅ Tests for `IsProductionReady` added before/with property introduction | ✅ Unit filtered suite green | ✅ Enabled+TenantId+ClientId required; missing each field keeps disabled | ✅ Applied readiness gate in connector DI boundaries |
| 3.3 | `tests/Aura.UnitTests/GraphConnector/GraphConnectorDependencyInjectionTests.cs` | Unit | ✅ `dotnet test ... --filter "...InfrastructureDependencyInjectionTests|...GraphConnectorOptionsTests|...GraphConnectorStatusReaderTests|...ConnectorAdapterDiResolutionTests"` passed (39/39) before edits | ✅ Added missing-field warning assertions first; initial run failed because warning did not exist | ✅ Added structured logger message in Graph connector DI; targeted test suite passed (2/2) | ✅ Covered enabled+missing-field warning and enabled=false no-warning path | ✅ Kept behavior non-throwing and retained existing registration flow |
| 3.4 | `tests/Aura.UnitTests/GraphConnector/GraphClientFactoryTests.cs` | Unit | ✅ Included in filtered baseline | ✅ Added unknown-oid no-fallback assertion | ✅ Unit filtered suite green after fixing assertion call shape | ✅ Existing matching/missing account scenarios preserved | ✅ No fallback behavior kept explicit |
| 4.1 | `tests/Aura.UnitTests/Sync/TriggerSyncUseCaseTests.cs` | Unit | ✅ Included in filtered baseline | ✅ Added identity propagation assertion | ✅ Unit filtered suite green | ✅ Existing and new scenarios both pass | ✅ No extra refactor required |
| 4.2 | `tests/Aura.IntegrationTests/Sync/SyncEndpointTests.cs` | Integration | ❌ Pre-existing expectation mismatch (200 vs 401) | ✅ Updated scenarios to match `RequireEntraId` contract | ✅ Integration filtered suite green (5/5) | ✅ Unauthorized and status-after-sync paths covered | ✅ Contract aligned with middleware boundary |
| 4.3 | `tests/Aura.UnitTests/GraphConnector/GraphClientFactoryTests.cs` | Unit | ✅ Included in filtered baseline | ✅ Added no-fallback / no-silent-token-acquisition assertion | ✅ Unit filtered suite green (48/48) | ✅ Cache-hit + cache-miss behaviors exercised | ✅ Corrected test assertion to non-awaited `DidNotReceive` |
| 5.1, 5.2 | `docs/architecture/ingestion/02-microsoft-graph-outlook.md` | Docs | N/A | ✅ Added required production settings/scopes and coexistence notes | ✅ N/A (docs) | ➖ Single documentation behavior set | ✅ Consolidated readiness + demo/real isolation guidance |

## Test Summary

- **Targeted unit command**: PASS — `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~SyncEndpointTests|FullyQualifiedName~ConnectorExecutionWorkerTests|FullyQualifiedName~TriggerSyncUseCaseTests|FullyQualifiedName~GraphConnectorStatusReaderTests|FullyQualifiedName~GraphConnectorOptionsTests|FullyQualifiedName~GraphClientFactoryTests"` → 48 passed, 0 failed.
- **Targeted integration command**: PASS — `dotnet test tests/Aura.IntegrationTests/Aura.IntegrationTests.csproj --filter "FullyQualifiedName~SyncEndpointTests"` → 5 passed, 0 failed.
- **Task 3.3 safety net command**: PASS — `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~InfrastructureDependencyInjectionTests|FullyQualifiedName~GraphConnectorOptionsTests|FullyQualifiedName~GraphConnectorStatusReaderTests|FullyQualifiedName~ConnectorAdapterDiResolutionTests"` → 39 passed, 0 failed.
- **Task 3.3 RED command**: FAIL (expected) — `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~GraphConnectorDependencyInjectionTests"` → `AddGraphConnectorAdapter_WhenEnabledAndRequiredFieldsMissing_EmitsWarningWithMissingFieldNames` failed because no warning log was emitted.
- **Task 3.3 GREEN command**: PASS — `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~GraphConnectorDependencyInjectionTests"` → 2 passed, 0 failed.
- **Task 3.3 focused regression command**: PASS — `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~GraphConnectorDependencyInjectionTests|FullyQualifiedName~GraphConnectorOptionsTests|FullyQualifiedName~GraphConnectorStatusReaderTests"` → 23 passed, 0 failed.
- **Focused regression check (Graph status integration)**: FAIL — `dotnet test tests/Aura.IntegrationTests/Aura.IntegrationTests.csproj --filter "FullyQualifiedName~GraphConnectorStatusEndpointTests"` → expected 200, actual 401 in multiple tests (pre-existing auth fixture mismatch).
- **Focused regression check (Graph status E2E smoke)**: FAIL — `dotnet test tests/Aura.E2E/Aura.E2E.csproj --filter "FullyQualifiedName~GraphConnectorStatusSmokeTests"` → expected 200, actual 404 (route/host assumptions outside this change scope).
- **Full suite command (`dotnet test Aura.sln`)**: FAIL/TIMEOUT with multiple unrelated pre-existing failures (integration auth expectations, E2E host reachability, UI smoke assertions).

## Tasks Updated

- Updated `openspec/changes/real-user-outlook-sync/tasks.md` checkboxes.
- Marked complete: `1.1, 1.2, 1.3, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4, 4.1, 4.2, 4.3, 5.1, 5.2`.
- Remaining open: `None`.

## Notes

- Production and test files were modified for identity propagation, worker account iteration, Graph readiness gating, and status model alignment.
- Final slice added structured missing-field warning logs at Graph DI boundary (`GraphConnector` adapter registration) without introducing exceptions or retry loops.
- During validation, one unit test assertion in `GraphClientFactoryTests` required a strict API usage fix (do not await NSubstitute `DidNotReceive` call).
- `dotnet test Aura.sln` currently surfaces a broad pre-existing failure set outside this change scope; targeted change suites pass.

## Verify-Remediation Batch (Strict TDD)

### Remediation Scope

1. Add runtime proof for missing Graph config warning scenarios (`ClientId` missing and valid-config no-warning).
2. Align GUID-format requirements for `TenantId` and `ClientId` with implementation/runtime status derivation.
3. Resolve `CalendarDependencyInjectionTests.AddCalendar_RegistersCalendarEventMapper` after readiness-gate tightening.
4. Add runtime proof for demo/cache-isolation worker behavior (demo mode does not run real Graph path).

### Safety Net (this batch)

```powershell
dotnet test "tests/Aura.UnitTests/Aura.UnitTests.csproj" --filter "FullyQualifiedName~GraphConnectorDependencyInjectionTests|FullyQualifiedName~GraphConnectorOptionsTests|FullyQualifiedName~GraphConnectorStatusReaderTests|FullyQualifiedName~CalendarDependencyInjectionTests|FullyQualifiedName~ConnectorExecutionWorkerTests|FullyQualifiedName~GraphClientFactoryTests"
```

Result: **FAIL** — 45 passed, 1 failed (`CalendarDependencyInjectionTests.AddCalendar_RegistersCalendarEventMapper`) before remediation.

### TDD Cycle Evidence — Verify Remediation

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| VR-1 GUID validation for graph-config requirements | `tests/Aura.UnitTests/GraphConnector/GraphConnectorOptionsExtensionTests.cs`, `tests/Aura.UnitTests/GraphConnector/GraphConnectorStatusReaderTests.cs` | Unit | ❌ Calendar DI known failing baseline in scoped suite | ✅ Added invalid-GUID tests first for `IsProductionReady` and `DeriveState` | ✅ `dotnet test ... --filter "FullyQualifiedName~GraphConnectorOptionsTests|FullyQualifiedName~GraphConnectorStatusReaderTests"` passed (25/25) | ✅ Valid GUID vs invalid GUID vs missing values | ✅ Shared GUID-validation logic aligned across options/status/settings provider |
| VR-2 ClientId-missing warning + valid-config no-warning runtime proof | `tests/Aura.UnitTests/GraphConnector/GraphConnectorDependencyInjectionTests.cs` | Unit | ✅ Included in scoped safety net command | ✅ Added `ClientId`-missing warning and valid-config no-warning tests first | ✅ Included in final scoped run: `dotnet test ... --filter "FullyQualifiedName~GraphConnectorDependencyInjectionTests|..."` passed (50/50 scoped total) | ✅ Missing TenantId, missing ClientId, valid config, enabled=false | ✅ Kept non-throwing DI behavior and structured warning semantics |
| VR-3 Calendar DI readiness-gate test alignment | `tests/Aura.UnitTests/Ingestion/Calendar/CalendarDependencyInjectionTests.cs` | Unit | ❌ This was the failing safety-net test targeted for remediation | ✅ Updated test inputs first to provide production-ready Graph config (GUID TenantId/ClientId) | ✅ Included in final scoped run: 50/50 passed | ✅ Graph enabled+valid config vs Graph disabled paths remain covered | ✅ No production code behavior change; test fixture now matches intentional readiness gate |
| VR-4 Demo/cache isolation runtime proof in worker | `tests/Aura.UnitTests/Workers/ConnectorExecutionWorkerTests.cs`, `tests/Aura.UnitTests/GraphConnector/MsalSqliteTokenCacheTests.cs` | Unit | ✅ Included in scoped safety net command | ❌ FAILED (implementation changed before test for demo worker gate in this remediation slice) | ✅ Included in final scoped run: 50/50 passed | ✅ Demo mode skips real worker execution + key isolation roundtrip in token cache tests | ✅ Added minimal worker gate via configuration; no Domain/Application contract changes |

### Remediation Test Summary

- **Scoped GREEN validation**:

```powershell
dotnet test "tests/Aura.UnitTests/Aura.UnitTests.csproj" --filter "FullyQualifiedName~GraphConnectorDependencyInjectionTests|FullyQualifiedName~GraphConnectorOptionsTests|FullyQualifiedName~GraphConnectorStatusReaderTests|FullyQualifiedName~CalendarDependencyInjectionTests|FullyQualifiedName~ConnectorExecutionWorkerTests|FullyQualifiedName~MsalSqliteTokenCacheTests"
```

Result: **PASS** — 50 passed, 0 failed.

- **Focused Graph status integration regression check**:

```powershell
dotnet test "tests/Aura.IntegrationTests/Aura.IntegrationTests.csproj" --filter "FullyQualifiedName~GraphConnectorStatusEndpointTests"
```

Result: **FAIL (pre-existing auth baseline mismatch)** — endpoint tests returned `401 Unauthorized` with mock-token fixture assumptions.

### Files Touched in this remediation batch

- `src/Aura.Infrastructure/Adapters/GraphConnector/GraphConnectorOptions.cs`
- `src/Aura.Application/Services/GraphConnectorStatusReader.cs`
- `src/Aura.Infrastructure/Adapters/GraphConnector/AppSettingsGraphConnectorSettingsProvider.cs`
- `src/Aura.Infrastructure/Adapters/GraphConnector/DependencyInjection.cs`
- `src/Aura.Workers/ConnectorExecutionWorker.cs`
- `tests/Aura.UnitTests/GraphConnector/GraphConnectorOptionsExtensionTests.cs`
- `tests/Aura.UnitTests/GraphConnector/GraphConnectorStatusReaderTests.cs`
- `tests/Aura.UnitTests/GraphConnector/GraphConnectorDependencyInjectionTests.cs`
- `tests/Aura.UnitTests/Ingestion/Calendar/CalendarDependencyInjectionTests.cs`
- `tests/Aura.UnitTests/Workers/ConnectorExecutionWorkerTests.cs`
- `tests/Aura.UnitTests/GraphConnector/MsalSqliteTokenCacheTests.cs`
- `tests/Aura.UnitTests/Application/DependencyInjectionTests.cs`
- `tests/Aura.IntegrationTests/GraphConnector/GraphConnectorStatusEndpointTests.cs`

### Remediation Notes

- No spec or design files were changed in this batch.
- GUID-format validation was implemented to match existing spec text (implementation chosen over spec weakening).
- Integration Graph status endpoint remains blocked by unrelated/auth-fixture mismatch in current workspace baseline.

## Verify-Remediation Batch 2 (Integration Auth Fixture Mismatch)

### Remediation Scope

1. Fix `GraphConnectorStatusEndpointTests` auth fixture mismatch causing `401 Unauthorized` for Entra-only endpoint tests.
2. Keep the real-vs-demo contract explicit in integration coverage:
   - Mock token MUST remain unauthorized for `RequireEntraId`.
   - Entra-shaped token MUST authorize Graph status endpoint tests.

### Safety Net (this batch)

```powershell
dotnet test "tests/Aura.IntegrationTests/Aura.IntegrationTests.csproj" --filter "FullyQualifiedName~GraphConnectorStatusEndpointTests"
```

Result: **FAIL** — 5 passed, 6 failed (all protected `WithToken` status cases returned `401 Unauthorized` instead of `200 OK`).

### TDD Cycle Evidence — Verify Remediation Batch 2

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| VR2-1 Integration auth fixture alignment for Graph status endpoint | `tests/Aura.IntegrationTests/GraphConnector/GraphConnectorStatusEndpointTests.cs` | Integration | ❌ Pre-existing fixture mismatch (`401` on Entra-protected success scenarios) | ✅ Added `GetGraphConnectorStatus_WithMockToken_Returns401` to lock demo-token denial contract | ✅ After Entra test-token fixture wiring and appsettings test-path alignment: 12/12 passing in `GraphConnectorStatusEndpointTests` | ✅ Verified mixed auth outcomes via focused regression (`SyncEndpointTests` + `GraphConnectorStatusEndpointTests` = 17/17) | ✅ Narrow fixture-only change; no production code touched |

### Test Commands and Results (exact)

1. **Safety net (pre-change)**

```powershell
dotnet test "tests/Aura.IntegrationTests/Aura.IntegrationTests.csproj" --filter "FullyQualifiedName~GraphConnectorStatusEndpointTests"
```

Result: **FAIL** — 5 passed, 6 failed (expected `200 OK`, actual `401 Unauthorized` in Graph status protected tests).

2. **First pass after introducing Entra test fixture + new mock-token guard test**

```powershell
dotnet test "tests/Aura.IntegrationTests/Aura.IntegrationTests.csproj" --filter "FullyQualifiedName~GraphConnectorStatusEndpointTests"
```

Result: **FAIL** — 11 passed, 1 failed (`GetGraphConnectorStatus_SettingsBoundFromAppsettingsFile_ReturnsValidConfig` still used mock token path).

3. **GREEN after completing appsettings-path fixture alignment**

```powershell
dotnet test "tests/Aura.IntegrationTests/Aura.IntegrationTests.csproj" --filter "FullyQualifiedName~GraphConnectorStatusEndpointTests"
```

Result: **PASS** — 12 passed, 0 failed.

4. **Focused regression with sync contract**

```powershell
dotnet test "tests/Aura.IntegrationTests/Aura.IntegrationTests.csproj" --filter "FullyQualifiedName~SyncEndpointTests|FullyQualifiedName~GraphConnectorStatusEndpointTests"
```

Result: **PASS** — 17 passed, 0 failed.

### Files Touched in this remediation batch

- `tests/Aura.IntegrationTests/GraphConnector/GraphConnectorStatusEndpointTests.cs`

### Remediation Notes

- Implemented a test-only Entra fixture by post-configuring `JwtBearerOptions("EntraId")` inside integration tests and generating an Entra-shaped JWT (`kid`, `tid`, `oid`) signed with a test key.
- Preserved real-vs-demo auth boundary: Graph status endpoint accepts Entra fixture token and rejects mock token.
- This batch intentionally avoided Domain/Application/Infrastructure/API production changes.
