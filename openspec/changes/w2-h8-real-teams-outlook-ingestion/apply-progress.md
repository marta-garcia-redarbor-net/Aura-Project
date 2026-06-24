# Apply Progress: Real Teams and Outlook Ingestion (PR1 + PR2 + PR3)

**Change**: w2-h8-real-teams-outlook-ingestion
**Mode**: Strict TDD
**Delivery**: feature-branch-chain

---

## Completed Tasks (PR1 — tasks 1.1–1.5, 2.1–2.5)

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

## Completed Tasks (PR2 — tasks 3.1–3.10, 4.1–4.7)

- [x] 3.1 RED: Write tests for `GraphTeamsSourceProvider`
- [x] 3.2 GREEN: Create `GraphTeamsSourceProvider`
- [x] 3.3 RED: Write tests for `GraphOutlookSourceProvider`
- [x] 3.4 GREEN: Create `GraphOutlookSourceProvider`
- [x] 3.5 Extend `TeamsMessageDto` (add `Sender`, `BodyPreview`, `WebUrl`) and `OutlookEmailDto` (add `WebLink`)
- [x] 3.6 RED: Write mapper tests verifying deepLink, snippet, sender land in WorkItem metadata
- [x] 3.7 GREEN: Update `TeamsWorkItemMapper.cs` and `OutlookWorkItemMapper.cs` to map new fields
- [x] 3.8 Modify `TeamsConnectorAdapter.cs` — inject optional `IMessageSourceProvider<TeamsMessageDto>`
- [x] 3.9 Modify `OutlookConnectorAdapter.cs` — same provider injection pattern
- [x] 3.10 Update `Connectors/DependencyInjection.cs` — conditionally register Graph source providers
- [x] 4.1 RED: Write `TriggerSyncUseCase` tests — per-source aggregation, partial degradation, `auth_required` status
- [x] 4.2 GREEN: Create `TriggerSyncUseCase`
- [x] 4.3 Create `InMemorySyncStateStore` implementing `ISyncStateStore`
- [x] 4.4 Create `SyncEndpoints.cs` — `POST /api/sync/now` + `GET /api/sync/status`
- [x] 4.5 Modify `ConnectorExecutionWorker.cs` — iterate all registered `IConnectorAdapter`
- [x] 4.6 Update `Infrastructure/DependencyInjection.cs` — wire ISyncStateStore, TriggerSyncUseCase, IGraphClientFactory
- [x] 4.7 RED/GREEN: Integration test — POST sync/now → GET dashboard/preview returns items

---

## TDD Cycle Evidence (PR1)

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1 | N/A (interface-only) | N/A | N/A (new) | ➖ Interface | ➖ Interface | ➖ Interface | ➖ Interface |
| 1.2 | N/A (interface-only) | N/A | N/A (new) | ➖ Interface | ➖ Interface | ➖ Interface | ➖ Interface |
| 1.3 | `tests/Aura.UnitTests/Sync/SyncResultDtoTests.cs`, `TokenStatusTests.cs` | Unit | N/A (new) | ✅ Written | ✅ Passed | ✅ Multiple record constructions | ➖ None needed |
| 1.4 | `tests/Aura.UnitTests/GraphConnector/GraphConnectorOptionsExtensionTests.cs` | Unit | N/A (new) | ✅ Written | ✅ Passed | ✅ 4 cases (null + set) | ➖ None needed |
| 1.5 | `tests/Aura.UnitTests/Dashboard/InboxItemPreviewDtoExtensionTests.cs` | Unit | N/A (new) | ✅ Written | ✅ Passed | ✅ Multiple nullable init-only cases | ➖ None needed |
| 2.1 | `tests/Aura.UnitTests/WorkItems/SqliteWorkItemStoreTests.cs` | Unit | N/A (new) | ✅ Written | ✅ Passed | ✅ 5 cases | ➖ None needed |
| 2.2 | (same as 2.1) | Unit | N/A (new) | ✅ (from 2.1) | ✅ Passed | ✅ (from 2.1) | ➖ None needed |
| 2.3 | `tests/Aura.UnitTests/GraphConnector/MsalSqliteTokenCacheTests.cs` | Unit | N/A (new) | ✅ Written | ✅ Passed | ✅ 6 cases | ➖ None needed |
| 2.4 | `tests/Aura.UnitTests/GraphConnector/GraphClientFactoryTests.cs` | Unit | N/A (new) | ✅ Written | ✅ 7/7 passed | ✅ 7 cases | ✅ Refactored to IOptions |
| 2.5 | `tests/Aura.IntegrationTests/GraphConnector/GraphConnectorStatusEndpointTests.cs` | Integration | ✅ 13/13 | ✅ DI failure proved RED | ✅ 13/13 passed | ✅ Multiple config scenarios | ✅ UserTokenCache + IOptions |

## TDD Cycle Evidence (PR2)

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 3.1 | `tests/Aura.UnitTests/GraphConnector/GraphTeamsSourceProviderTests.cs` | Unit | N/A (new) | ✅ Written | ✅ 4/4 passed | ✅ 4 cases (mapping, empty, MsalUi, null-topic) | ➖ None needed |
| 3.2 | (same as 3.1 — test-first) | Unit | N/A (new) | ✅ (from 3.1) | ✅ Passed | ✅ (from 3.1) | ➖ None needed |
| 3.3 | `tests/Aura.UnitTests/GraphConnector/GraphOutlookSourceProviderTests.cs` | Unit | N/A (new) | ✅ Written | ✅ 4/4 passed | ✅ 4 cases (mapping, empty, MsalUi, webLink) | ➖ None needed |
| 3.4 | (same as 3.3 — test-first) | Unit | N/A (new) | ✅ (from 3.3) | ✅ Passed | ✅ (from 3.3) | ➖ None needed |
| 3.5 | (covered by 3.1/3.3/3.6) | Unit | ✅ 403/403 | ✅ DTO extension used by tests | ✅ Passed | ➖ Structural | ➖ None needed |
| 3.6 | `tests/Aura.UnitTests/GraphConnector/TeamsWorkItemMapperNewFieldsTests.cs`, `OutlookWorkItemMapperNewFieldsTests.cs` | Unit | ✅ 403/403 | ✅ Written (4 failed) | ✅ 6/6 passed | ✅ 6 cases (all-fields, null-fields, webUrl-override, webLink, null-webLink, snippet) | ➖ None needed |
| 3.7 | (same as 3.6 — test-first) | Unit | ✅ 403/403 | ✅ (from 3.6) | ✅ Passed | ✅ (from 3.6) | ➖ None needed |
| 3.8 | (existing adapter tests still pass) | Unit | ✅ 403/403 | ✅ Provider injection tested via 3.1 | ✅ 422/422 passed | ➖ Structural modification | ➖ None needed |
| 3.9 | (existing adapter tests still pass) | Unit | ✅ 403/403 | ✅ Provider injection tested via 3.3 | ✅ 422/422 passed | ➖ Structural modification | ➖ None needed |
| 3.10 | (integration tests prove DI wiring) | Integration | ✅ 54/54 | ✅ DI validated | ✅ All passing | ➖ Config-only | ➖ None needed |
| 4.1 | `tests/Aura.UnitTests/Sync/TriggerSyncUseCaseTests.cs` | Unit | N/A (new) | ✅ Written | ✅ 5/5 passed | ✅ 5 cases (all-success, partial-degrade, auth-required, state-update, empty) | ➖ None needed |
| 4.2 | (same as 4.1 — test-first) | Unit | N/A (new) | ✅ (from 4.1) | ✅ Passed | ✅ (from 4.1) | ➖ None needed |
| 4.3 | (tested via TriggerSyncUseCase integration) | Unit | N/A (new) | ✅ Used by 4.7 | ✅ Passed | ➖ Simple store | ➖ None needed |
| 4.4 | `tests/Aura.IntegrationTests/Sync/SyncEndpointTests.cs` | Integration | N/A (new) | ✅ Written | ✅ 5/5 passed | ✅ 5 cases (auth, results, status-after-sync, preview-after-sync, status-no-auth) | ➖ None needed |
| 4.5 | `tests/Aura.UnitTests/Workers/ConnectorExecutionWorkerTests.cs` | Unit | ✅ 1/1 (updated) | ✅ Safety net updated | ✅ 1/1 passed | ➖ Structural | ➖ None needed |
| 4.6 | (integration tests prove wiring) | Integration | ✅ All passing | ✅ DI validated | ✅ All passing | ➖ Config-only | ➖ None needed |
| 4.7 | `tests/Aura.IntegrationTests/Sync/SyncEndpointTests.cs` | Integration | N/A (new) | ✅ Written | ✅ 5/5 passed | ✅ Full sync → preview flow | ➖ None needed |

## TDD Cycle Evidence (PR2 Remediation Round 2)

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 3.8 (prove) | `tests/Aura.UnitTests/Ingestion/Teams/TeamsConnectorAdapterTests.cs` | Unit | ✅ 6/6 | ✅ Written (provider branch tests) | ✅ 10/10 passed | ✅ 2 cases (uses-provider, maps-metadata) | ➖ None needed |
| 3.9 (prove) | `tests/Aura.UnitTests/Ingestion/Outlook/OutlookConnectorAdapterTests.cs` | Unit | ✅ 6/6 | ✅ Written (provider branch tests) | ✅ 10/10 passed | ✅ 2 cases (uses-provider, maps-metadata) | ➖ None needed |
| 3.10 (prove) | `tests/Aura.UnitTests/GraphConnector/ConnectorAdapterDiResolutionTests.cs` | Unit | N/A (new) | ✅ Written | ✅ 3/3 passed | ✅ 3 cases (teams-with, outlook-with, teams-without) | ➖ None needed |
| 4.7 (fix+prove) | `tests/Aura.UnitTests/Dashboard/DashboardPreviewReaderTests.cs` | Unit | ✅ 2/2 | ✅ Written (2 tests FAILED before fix) | ✅ 4/4 passed after fix | ✅ 2 cases (with-metadata, empty-metadata) | ✅ Extracted pure helpers |
| 4.7 (persist) | `tests/Aura.UnitTests/Sync/TriggerSyncUseCaseTests.cs` | Unit | ✅ 5/5 | ✅ Written (CS1729 compilation RED) | ✅ 6/6 passed | ➖ Single drain path | ➖ None needed |
| 4.7 (integration) | `tests/Aura.IntegrationTests/Sync/SyncEndpointTests.cs` | Integration | ✅ 4/5 (replaced 1 test) | ✅ Written (FAILED before infra fix) | ✅ 5/5 passed | ✅ JSON field-level assertions | ➖ None needed |

---

## PR2 Remediation Round 2 (Verify-Driven)

### Issue 5: Adapter provider-injection branch not runtime-proven (3.8, 3.9)
**Root cause**: Existing adapter unit tests only exercised the fixture-fallback path. No test proved the `_sourceProvider is not null` branch.
**Fix**: Added 2 focused tests per adapter (4 total): one proving provider is used instead of fixtures, one proving metadata fields map correctly through the provider path.
**Files**:
- `tests/Aura.UnitTests/Ingestion/Teams/TeamsConnectorAdapterTests.cs` — +2 tests
- `tests/Aura.UnitTests/Ingestion/Outlook/OutlookConnectorAdapterTests.cs` — +2 tests

### Issue 6: DashboardPreviewReader not populating new fields (4.7)
**Root cause**: `DashboardPreviewReader.GetAsync()` constructed `InboxItemPreviewDto` with only the 5 positional constructor arguments, never setting `Sender`, `Snippet`, `DeepLink`, `PriorityHint`, or `SyncState` from WorkItem.Metadata.
**Fix**: Added init-only property population from source-prefixed metadata keys (e.g., `teams.sender`, `outlook.deepLink`). `PriorityHint` is always set from `WorkItemPriority.ToString()`. `SyncState` is set to `"synced"` when any sync-originated metadata is present.
**Files**:
- `src/Aura.Application/Services/DashboardPreviewReader.cs` — added `ExtractMetadata()`, `HasSyncedMetadata()`, and init-only property population
- `tests/Aura.UnitTests/Dashboard/DashboardPreviewReaderTests.cs` — +2 tests (with metadata, empty metadata)

### Issue 7: No persistence path from TriggerSyncUseCase → store → DashboardPreviewReader (4.7)
**Root cause**: `TriggerSyncUseCase` called adapters (which write to `IWorkItemBuffer`) but never drained/persisted items to `IWorkItemStore`. Additionally, there was no `IWorkItemReader` implementation to read items back for the dashboard preview.
**Fix**:
1. Added optional `IWorkItemBuffer` + `IWorkItemStore` to `TriggerSyncUseCase` via constructor overload (backward-compatible).
2. Added `PersistBufferedItemsAsync()` after all adapters complete — drains buffer and persists each item.
3. Made `SqliteWorkItemStore` also implement `IWorkItemReader` with `ReadForWindowAsync()` using CapturedAtUtc window.
4. Registered `IWorkItemReader` from the same `SqliteWorkItemStore` singleton in DI.
5. Updated `TriggerSyncUseCase` DI registration to inject buffer and store.
**Files**:
- `src/Aura.Application/UseCases/IngestionSync/TriggerSyncUseCase.cs` — new constructor overload + `PersistBufferedItemsAsync`
- `src/Aura.Infrastructure/Adapters/WorkItems/SqliteWorkItemStore.cs` — implements `IWorkItemReader`, added `ReadForWindowAsync`
- `src/Aura.Infrastructure/Adapters/WorkItems/DependencyInjection.cs` — registers `IWorkItemReader`
- `src/Aura.Infrastructure/DependencyInjection.cs` — factory-based `TriggerSyncUseCase` registration with buffer+store
- `tests/Aura.UnitTests/Sync/TriggerSyncUseCaseTests.cs` — +1 test proving drain+persist

### Issue 8: Integration test for sync→preview didn't assert new fields (4.7)
**Root cause**: `PostSyncNow_ThenGetDashboardPreview_ReturnsItems` only checked `Assert.Contains("InboxGroups", content)` — a weak string-shape assertion.
**Fix**: Replaced with `PostSyncNow_ThenGetDashboardPreview_ReturnsItemsWithSyncedFields` that deserializes JSON, asserts `inboxGroups` is non-empty, and verifies at least one preview item carries `priorityHint`.
**File**: `tests/Aura.IntegrationTests/Sync/SyncEndpointTests.cs` — replaced test with field-level assertions

### Issue 9: DI resolution proof for provider registration path (3.10)
**Root cause**: No PR2-scoped test proved that when `IMessageSourceProvider<T>` is registered, adapters actually receive and use it via DI.
**Fix**: Added `ConnectorAdapterDiResolutionTests` — 3 tests: Teams+Outlook with provider registered (verifies `FetchAsync` is called), Teams without provider (verifies fixture fallback).
**File**: `tests/Aura.UnitTests/GraphConnector/ConnectorAdapterDiResolutionTests.cs` — 3 new tests

---

## PR2 Remediation Actions (Round 1 — preserved)

### Issue 1: IGraphClientFactory not registered as interface
**Root cause**: `GraphClientFactory` was registered as concrete type only in DI. Source providers depend on `IGraphClientFactory` interface.
**Fix**: Added `services.AddSingleton<IGraphClientFactory>(sp => sp.GetRequiredService<GraphClientFactory>())` in Graph DI.
**File**: `src/Aura.Infrastructure/Adapters/Connectors/Graph/DependencyInjection.cs`

### Issue 2: NSubstitute cannot proxy internal interfaces without DynamicProxyGenAssembly2
**Root cause**: `IGraphClientFactory` is internal; Castle DynamicProxy needs InternalsVisibleTo.
**Fix**: Added `<InternalsVisibleTo Include="DynamicProxyGenAssembly2" />` to Infrastructure csproj.
**File**: `src/Aura.Infrastructure/Aura.Infrastructure.csproj`

### Issue 3: Worker test broke due to multi-connector iteration
**Root cause**: Worker now iterates `IEnumerable<IConnectorAdapter>` from DI instead of hardcoded identity.
**Fix**: Updated test to register `IConnectorAdapter` in service collection and use `Arg.Any<CheckpointIdentity>()` matcher.
**File**: `tests/Aura.UnitTests/Workers/ConnectorExecutionWorkerTests.cs`

### Issue 4: Application layer must not reference Microsoft.Identity.Client
**Root cause**: TriggerSyncUseCase initially caught `MsalUiRequiredException` directly.
**Fix**: Uses string-based auth-required detection via failure reason patterns. MSAL stays in Infrastructure.
**File**: `src/Aura.Application/UseCases/IngestionSync/TriggerSyncUseCase.cs`

---

## PR1 Remediation Actions (preserved from previous batch)

### Issue 1: Missing apply-progress.md
**Fixed**: This file now exists with strict-TDD evidence table.

### Issue 2: Graph DI runtime failure when `GraphConnector:Enabled=true`
**Root cause**: `GraphClientFactory` constructor accepted `GraphConnectorOptions` directly, but DI only registers `IOptions<GraphConnectorOptions>`.
**Fix**: Changed constructor signature to `IOptions<GraphConnectorOptions>`.

### Issue 3: Delegated token cache wiring used AppTokenCache
**Fix**: Changed `app.AppTokenCache` → `app.UserTokenCache` and updated cache key prefix.

### Issue 4: Tests for GraphClientFactory scenarios
**Added**: 7 focused unit tests covering silent reuse, expired token, null guards, scope config.

### Issue 5: Changed-file coverage for GraphClientFactory
**Improved**: From 0% to meaningful coverage via 7 tests.

---

## Completed Tasks (PR3 — tasks 5.1–5.4, 6.1)

- [x] 5.1 Update `src/Aura.UI/Models/DashboardPreviewResponse.cs` — mirror optional fields
- [x] 5.2 Modify `InboxPreviewPanel.razor` — render sender, snippet, deepLink, syncState with `data-testid` attributes
- [x] 5.3 Create `SyncStatusPanel.razor` — sync-now button, per-source progress/result, last-sync timestamp, re-auth prompt
- [x] 5.4 Verify explicit empty-state UX: sync succeeds with zero items → UI says so, no demo fallback
- [x] 6.1 Add NetArchTest rule: `Microsoft.Graph` types MUST NOT be referenced from Application or Domain

## TDD Cycle Evidence (PR3)

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 5.1 | `tests/Aura.UnitTests/Dashboard/DashboardPreviewResponseOptionalFieldsTests.cs` | Unit | ✅ 42/42 | ✅ Written (5 tests) | ✅ 5/5 passed | ✅ 5 cases (serialize nulls, deserialize, no-apostrophe, round-trip, defaults) | ➖ None needed |
| 5.2 | `tests/Aura.E2E/Dashboard/InboxPreviewPanelFieldsSmokeTests.cs` | E2E | ✅ 25/25 | ✅ Written (5 tests) | ✅ 5/5 passed | ✅ 5 cases (populated, null-omission, empty-state, error-state, multi-source) | ➖ None needed |
| 5.3 | `tests/Aura.E2E/Dashboard/SyncStatusPanelSmokeTests.cs` | E2E | ✅ 28/28 | ✅ Written (3 tests) | ✅ 3/3 passed | ✅ 3 cases (renders, progress-divs, timestamp) | ➖ None needed |
| 5.4 | `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` (existing) | E2E | ✅ 25/25 | ✅ Pre-existing test proves empty-state | ✅ Passes | ✅ Explicit message + no demo fallback | ➖ None needed |
| 6.1 | `tests/Aura.ArchitectureTests/GraphConnectorArchitectureTests.cs` | Architecture | ✅ 31/31 | ✅ Written (7 tests, fixed namespace) | ✅ 7/7 passed | ✅ 7 rules (Graph+MIA for Domain/Workers/Api/UI/App) | ➖ None needed |
| 6.2 | `tests/Aura.E2E/Playwright/PlaywrightBootstrapTests.cs` | E2E (scaffold) | ✅ 33/33 | ✅ Written (3 tests) | ✅ Compiles (browsers not installed — scaffold only) | ✅ 3 cases (shell, inbox, sync) | ➖ Scaffold |
| 6.3 | (selectors already in 5.2) | E2E | ✅ Already done | ✅ data-testid attrs in InboxPreviewPanel + SyncStatusPanel | ✅ Verified | ✅ All new fields have testids | ➖ None needed |

---

## Test Summary (Cumulative)
- **Total unit tests passing**: 437 (baseline 403 + 19 PR2 initial + 10 remediation + 5 PR3)
- **Total integration tests passing**: 41 PR2-relevant; 7 Qdrant-dependent tests skipped (pre-existing, Docker-only)
- **Total architecture tests passing**: 38 (baseline 31 + 7 PR3)
- **Total E2E tests passing**: 33 (baseline 25 + 8 PR3)
- **PR3 new tests written**: 5 unit + 8 E2E + 7 architecture = 20
- **Layers used**: Unit (5 new), E2E (8 new), Architecture (7 new)
- **Approval tests** (refactoring): N/A — adapter modification tested via existing + new test coverage

## Files Changed (PR2 — including remediation round 2)

| File | Action | What Was Done |
|------|--------|---------------|
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/IGraphClientFactory.cs` | Created | Interface for GraphServiceClient creation, enables unit testing |
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphClientFactory.cs` | Modified | Now implements `IGraphClientFactory` |
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphTeamsSourceProvider.cs` | Created | `IMessageSourceProvider<TeamsMessageDto>` via Graph /me/chats |
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphOutlookSourceProvider.cs` | Created | `IMessageSourceProvider<OutlookEmailDto>` via Graph /me/messages |
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/InMemorySyncStateStore.cs` | Created | In-memory `ISyncStateStore` implementation |
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/DependencyInjection.cs` | Modified | Register `IGraphClientFactory` interface binding |
| `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsMessageDto.cs` | Modified | Added `Sender`, `BodyPreview`, `WebUrl` fields |
| `src/Aura.Infrastructure/Adapters/Connectors/Outlook/OutlookEmailDto.cs` | Modified | Added `WebLink` field |
| `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsWorkItemMapper.cs` | Modified | Maps `teams.sender`, `teams.snippet`, `teams.deepLink` to metadata |
| `src/Aura.Infrastructure/Adapters/Connectors/Outlook/OutlookWorkItemMapper.cs` | Modified | Maps `outlook.deepLink`, `outlook.snippet` to metadata |
| `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsConnectorAdapter.cs` | Modified | Accepts optional `IMessageSourceProvider<TeamsMessageDto>?`; async |
| `src/Aura.Infrastructure/Adapters/Connectors/Outlook/OutlookConnectorAdapter.cs` | Modified | Accepts optional `IMessageSourceProvider<OutlookEmailDto>?`; async |
| `src/Aura.Infrastructure/Adapters/Connectors/DependencyInjection.cs` | Modified | Conditionally registers Graph source providers when enabled |
| `src/Aura.Infrastructure/Adapters/WorkItems/SqliteWorkItemStore.cs` | Modified | Now implements `IWorkItemReader` with `ReadForWindowAsync` |
| `src/Aura.Infrastructure/Adapters/WorkItems/DependencyInjection.cs` | Modified | Registers `IWorkItemReader` from `SqliteWorkItemStore` singleton |
| `src/Aura.Infrastructure/DependencyInjection.cs` | Modified | Factory-based TriggerSyncUseCase with buffer+store injection |
| `src/Aura.Infrastructure/Aura.Infrastructure.csproj` | Modified | Added `DynamicProxyGenAssembly2` InternalsVisibleTo |
| `src/Aura.Application/Services/DashboardPreviewReader.cs` | Modified | Populates Sender, Snippet, DeepLink, PriorityHint, SyncState from metadata |
| `src/Aura.Application/UseCases/IngestionSync/TriggerSyncUseCase.cs` | Modified | Drains buffer + persists items after sync; new constructor overload |
| `src/Aura.Api/Endpoints/SyncEndpoints.cs` | Created | POST /api/sync/now + GET /api/sync/status |
| `src/Aura.Api/Program.cs` | Modified | Added `app.MapSyncEndpoints()` |
| `src/Aura.Workers/ConnectorExecutionWorker.cs` | Modified | Multi-connector iteration instead of hardcoded identity |
| `tests/Aura.UnitTests/GraphConnector/GraphTeamsSourceProviderTests.cs` | Created | 4 unit tests for Graph Teams provider |
| `tests/Aura.UnitTests/GraphConnector/GraphOutlookSourceProviderTests.cs` | Created | 4 unit tests for Graph Outlook provider |
| `tests/Aura.UnitTests/GraphConnector/TeamsWorkItemMapperNewFieldsTests.cs` | Created | 3 tests for new Teams metadata mapping |
| `tests/Aura.UnitTests/GraphConnector/OutlookWorkItemMapperNewFieldsTests.cs` | Created | 3 tests for new Outlook metadata mapping |
| `tests/Aura.UnitTests/GraphConnector/ConnectorAdapterDiResolutionTests.cs` | Created | 3 DI resolution proof tests for provider injection path |
| `tests/Aura.UnitTests/Sync/TriggerSyncUseCaseTests.cs` | Modified | +1 test for buffer drain+persist |
| `tests/Aura.UnitTests/Ingestion/Teams/TeamsConnectorAdapterTests.cs` | Modified | +2 tests for provider-injection branch |
| `tests/Aura.UnitTests/Ingestion/Outlook/OutlookConnectorAdapterTests.cs` | Modified | +2 tests for provider-injection branch |
| `tests/Aura.UnitTests/Dashboard/DashboardPreviewReaderTests.cs` | Modified | +2 tests for new field population |
| `tests/Aura.UnitTests/Workers/ConnectorExecutionWorkerTests.cs` | Modified | Updated for multi-connector pattern |
| `tests/Aura.IntegrationTests/Sync/SyncEndpointTests.cs` | Modified | Replaced weak assertion with field-level JSON verification |
| `src/Aura.UI/Models/DashboardPreviewResponse.cs` | Modified | Added optional init-only fields to `InboxItemPreviewResponse` |
| `src/Aura.UI/Components/Dashboard/InboxPreviewPanel.razor` | Modified | Renders new fields conditionally with data-testid attributes |
| `src/Aura.UI/Components/Dashboard/SyncStatusPanel.razor` | Created | New component for sync-now UI with per-source progress |
| `src/Aura.UI/Pages/Index.razor` | Modified | Added `<SyncStatusPanel />` between InboxPreviewPanel and MorningSummaryPreviewPanel |
| `tests/Aura.UnitTests/Dashboard/DashboardPreviewResponseOptionalFieldsTests.cs` | Created | 5 unit tests for UI model optional fields |
| `tests/Aura.E2E/Dashboard/InboxPreviewPanelFieldsSmokeTests.cs` | Created | 5 E2E smoke tests for new fields |
| `tests/Aura.E2E/Dashboard/SyncStatusPanelSmokeTests.cs` | Created | 3 E2E smoke tests for sync panel |
| `tests/Aura.ArchitectureTests/GraphConnectorArchitectureTests.cs` | Modified | Added 7 new NetArchTest rules for Graph SDK isolation |
| `tests/Aura.E2E/Aura.E2E.csproj` | Modified | Added Microsoft.Playwright package reference |
| `tests/Aura.E2E/Playwright/PlaywrightBootstrapTests.cs` | Created | Playwright scaffold — 3 bootstrap tests (shell, inbox panel, sync panel) |
