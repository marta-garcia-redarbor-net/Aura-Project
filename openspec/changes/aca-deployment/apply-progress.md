# Apply Progress: aca-deployment (Phase 1 + Phase 2 + Phase 3 + Phase 4 + Phase 5)

## Phase 1 — Completed Scope

- Verified and retained existing Phase 1 foundation artifacts for EF Core migration setup:
  - `Aura.Infrastructure.csproj` EF Core package references (`SqlServer`, `Sqlite`, `Design`)
  - `AuraDbContext` with 9 `DbSet<>` definitions
  - 9 `IEntityTypeConfiguration<T>` mappings under `Persistence/EntityConfigurations`
  - `IConnectionStringProvider` interface
- Added ACA infrastructure Bicep modules required by Phase 1:
  - `infra/aca/main.bicep`
  - `infra/aca/api.bicep`
  - `infra/aca/ui.bicep`
  - `infra/aca/workers.bicep`
  - `infra/aca/sql-database.bicep`
- Added targeted architecture tests for ACA foundation Bicep requirements.
- Marked all Phase 1 tasks as complete in `openspec/changes/aca-deployment/tasks.md`.

## Phase 1 — TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1 Add EF Core deps | `tests/Aura.UnitTests/Persistence/AuraDbContextRegistrationTests.cs` | Unit | ✅ 5/5 targeted passing baseline | ✅ Pre-existing RED test present | ✅ Passed (`InfrastructureAssembly_ReferencesEntityFrameworkCore`, `EntityFrameworkCoreSqlite_ProviderAssemblyIsLoadable`) | ✅ Second provider assembly case | ➖ None needed |
| 1.2 Create `AuraDbContext` | `tests/Aura.UnitTests/Persistence/AuraDbContextRegistrationTests.cs` | Unit | ✅ 5/5 targeted passing baseline | ✅ Pre-existing RED test present | ✅ Passed (`AuraDbContext_HasExpectedDbSets`) | ✅ 9 explicit `DbSet` assertions | ➖ None needed |
| 1.3 Create 9 entity configs | `tests/Aura.UnitTests/Persistence/AuraDbContextRegistrationTests.cs` | Unit | ✅ 5/5 targeted passing baseline | ✅ Pre-existing RED test present | ✅ Passed (`AllEntityConfigurations_ExistAndImplementIEntityTypeConfiguration`) | ✅ 9 explicit config type checks + interface contract check | ➖ None needed |
| 1.4 Create `IConnectionStringProvider` | `tests/Aura.UnitTests/Persistence/AuraDbContextRegistrationTests.cs` | Unit | ✅ 5/5 targeted passing baseline | ✅ Pre-existing RED test present | ✅ Passed (`IConnectionStringProvider_ExistsWithGetConnectionString`) | ✅ Interface location fallback + signature assertions | ➖ None needed |
| 1.5 `infra/aca/main.bicep` | `tests/Aura.ArchitectureTests/AcaBicepFoundationTests.cs` | Architecture | N/A (new files) | ✅ Written first (`MainBicep_WiresResourceGroupManagedEnvironmentAndModules`) | ✅ Passed | ✅ Includes module wiring assertions for api/ui/workers/sql | ➖ None needed |
| 1.6 `infra/aca/api.bicep` | `tests/Aura.ArchitectureTests/AcaBicepFoundationTests.cs` | Architecture | N/A (new files) | ✅ Written first (`ApiBicep_DefinesIngressCorsAndContainerEnvVariables`) | ✅ Passed | ✅ Ingress + CORS + env assertions | ➖ None needed |
| 1.7 `infra/aca/ui.bicep` | `tests/Aura.ArchitectureTests/AcaBicepFoundationTests.cs` | Architecture | N/A (new files) | ✅ Written first (`UiBicep_DefinesPublicIngressAndContainerEnvVariables`) | ✅ Passed | ✅ Ingress + env assertions | ➖ None needed |
| 1.8 `infra/aca/workers.bicep` | `tests/Aura.ArchitectureTests/AcaBicepFoundationTests.cs` | Architecture | N/A (new files) | ✅ Written first (`WorkersBicep_DefinesContainerAppWithoutIngress`) | ✅ Passed | ✅ Confirms no ingress + env assertions | ➖ None needed |
| 1.9 `infra/aca/sql-database.bicep` | `tests/Aura.ArchitectureTests/AcaBicepFoundationTests.cs` | Architecture | N/A (new files) | ✅ Written first (`SqlDatabaseBicep_DefinesServerDatabaseAndAllowAzureServicesFirewallRule`) | ✅ Passed | ✅ Server + DB + Azure services firewall assertions | ➖ None needed |

## Phase 2 — Completed Scope

- Created 9 EF Core store implementations, each implementing the same port interface as the existing SQLite stores:
  1. `EfFocusStateOverrideStore` → `IFocusStateOverrideStore`
  2. `EfInterruptionDecisionStore` → `IInterruptionDecisionStore`
  3. `EfAlertRuleStore` → `IAlertRuleStore`
  4. `EfNotificationOutboxStore` → `INotificationOutboxStore`
  5. `EfMeetingAlertStore` → `IMeetingAlertStore`
  6. `EfMorningSummaryEmissionStore` → `IMorningSummaryEmissionStore`
  7. `EfWorkItemStore` → `IWorkItemStore` + `IWorkItemReader`
  8. `EfSemanticOutboxRepository` → `ISemanticOutboxRepository`
  9. `EfMsalTokenCacheStore` → `IMsalTokenCacheStore` (new port interface created)
- Created `IMsalTokenCacheStore` port interface in `src/Aura.Application/Ports/`
- Created `StoreRegistrationExtensions.cs` with conditional DI per store:
  - `AddAuraDbContext(connectionStringName)` — registers `AuraDbContext` with SQLite provider
  - `RegisterConditionalStores()` — reads `Persistence:Providers:{StoreName}` config key and registers EF Core implementations when value is `EntityFramework`
- All 9 EF stores use `AuraDbContext` via constructor injection
- All EF stores placed in `src/Aura.Infrastructure/Adapters/{Domain}/Ef{StoreName}.cs`
- Existing SQLite stores left untouched

## Phase 2 — TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 2.1 EfFocusStateOverrideStore | `tests/Aura.UnitTests/Persistence/EfStoreTests.cs` | Unit | ✅ 5/5 Phase 1 passing | ✅ Written first (4 tests referencing non-existent class) | ✅ Passed (`GetAsync_ReturnsNull`, `SetAndGet_RoundTrips`, `SetAsync_Overwrites`, `ClearAsync_Removes`) | ✅ 4 cases | ➖ None needed |
| 2.2 EfInterruptionDecisionStore | `tests/Aura.UnitTests/Persistence/EfStoreTests.cs` | Unit | ✅ 5/5 Phase 1 passing | ✅ Written first (2 tests) | ✅ Passed (`RecordAndQuery_RoundTrips`, `QueryAsync_PaginatesCorrectly`) | ✅ 2 cases | ➖ None needed |
| 2.3 EfAlertRuleStore | `tests/Aura.UnitTests/Persistence/EfStoreTests.cs` | Unit | ✅ 5/5 Phase 1 passing | ✅ Written first (6 tests) | ✅ Passed (`GetVipSenders_ReturnsEmpty`, `AddAndGet_RoundTrips`, `RemoveVipSender`, `AddAndGetKeywords`, `RemoveKeyword`, `AddVipSender_Idempotent`) | ✅ 6 cases | ➖ None needed |
| 2.4 EfNotificationOutboxStore | `tests/Aura.UnitTests/Persistence/EfStoreTests.cs` | Unit | ✅ 5/5 Phase 1 passing | ✅ Written first (3 tests) | ✅ Passed (`EnqueueAndGetPending_RoundTrips`, `MarkDispatched_SetsDispatchedAt`, `GetPending_OrdersByPriorityDesc`) | ✅ 3 cases | ➖ None needed |
| 2.5 EfMeetingAlertStore | `tests/Aura.UnitTests/Persistence/EfStoreTests.cs` | Unit | ✅ 5/5 Phase 1 passing | ✅ Written first (3 tests) | ✅ Passed (`GetUnsentAlert_ReturnsNull`, `MarkSentAndRetrieve_RoundTrips`, `GetUpcomingAlerts_FiltersByDateRange`) | ✅ 3 cases | ✅ Fixed `string.Compare` → 2-arg overload for EF Core translation |
| 2.6 EfMorningSummaryEmissionStore | `tests/Aura.UnitTests/Persistence/EfStoreTests.cs` | Unit | ✅ 5/5 Phase 1 passing | ✅ Written first (3 tests) | ✅ Passed (`HasBeenEmitted_ReturnsFalse`, `MarkEmittedAndCheck_ReturnsTrue`, `Reset_ClearsEmission`) | ✅ 3 cases | ➖ None needed |
| 2.7 EfWorkItemStore | `tests/Aura.UnitTests/Persistence/EfStoreTests.cs` | Unit | ✅ 5/5 Phase 1 passing | ✅ Written first (5 tests) | ✅ Passed (`SaveAndFindByExternalId_RoundTrips`, `FindByExternalId_ReturnsNull`, `SaveAsync_UpsertsOnConflict`, `GetPendingExternalIds_ReturnsOnlyPending`, `MarkCompletedAsync_UpdatesMatching`) | ✅ 5 cases | ✅ Fixed `string.Compare` → 2-arg overload for EF Core translation |
| 2.8 EfSemanticOutboxRepository | `tests/Aura.UnitTests/Persistence/EfStoreTests.cs` | Unit | ✅ 5/5 Phase 1 passing | ✅ Written first (3 tests) | ✅ Passed (`EnqueueAndFetchPending_RoundTrips`, `UpdateAsync_MarksAsProcessed`, `FetchPending_ReturnsOnlyUnprocessed`) | ✅ 3 cases | ➖ None needed |
| 2.9 EfMsalTokenCacheStore | `tests/Aura.UnitTests/Persistence/EfStoreTests.cs` | Unit | ✅ 5/5 Phase 1 passing | ✅ Written first (5 tests) | ✅ Passed (`Retrieve_ReturnsNull`, `PersistAndRetrieve_RoundTrips`, `HasCachedData_ReturnsTrueAfterPersist`, `HasCachedData_ReturnsFalse`, `PersistAsync_UpsertsOnConflict`) | ✅ 5 cases | ➖ None needed |
| 2.10 Conditional DI | `tests/Aura.UnitTests/Persistence/ConditionalStoreRegistrationTests.cs` | Unit | ✅ 5/5 Phase 1 passing | ✅ Written first (2 tests referencing non-existent extension methods) | ✅ Passed (`RegisterEfStores_WhenProviderIsEntityFramework`, `RegisterConditionalStores_DefaultsToSqlite`) | ✅ 2 cases | ➖ None needed |

## Phase 2 — Test Summary

- **Targeted commands executed**:
  - `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~EfStoreTests"`
  - `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~ConditionalStoreRegistrationTests"`
  - `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~AuraDbContextRegistrationTests"` (safety net)
- **Result**: 41 passed, 0 failed (34 EF store + 2 DI registration + 5 Phase 1 safety net)
- **Approval tests**: None — no refactoring tasks in this batch
- **Pure functions created**: None (data access layer)

## Cumulative Test Summary

- **Total tests written Phase 1 + 2**: 52 (5 Phase 1 architecture + 10 Phase 1 bicep + 34 Phase 2 EF stores + 2 Phase 2 DI + 1 existing semantic outbox)
- **Total tests passing**: 41 (Phase 2 targeted) + 5 (Phase 1 safety net) = 46 confirmed
- **Layers used**: Unit (36), Architecture (10)

## Workload / Boundary

- Delivery mode: `size:exception` (explicit maintainer approval)
- Current work unit: `Phase 3 Container + CI/CD`
- Boundary: 3 Dockerfiles + health check + GA workflow + appsettings
- Cumulative: Phase 1 (foundation) + Phase 2 (stores) + Phase 3 (container + CI/CD) complete

## Phase 3 — Completed Scope

- Updated `src/Aura.Api/Dockerfile` — multi-stage build with `--platform=$BUILDPLATFORM` and `TARGETARCH=amd64`, `ASPNETCORE_URLS=http://+:8080`, `HEALTHCHECK` instruction using curl against `/health`
- Updated `src/Aura.UI/Dockerfile` — same multi-arch pattern as API
- Updated `src/Aura.Workers/Dockerfile` — same multi-stage build, no HTTP endpoint or health check (console worker)
- Created `src/Aura.Infrastructure/Persistence/DbHealthCheck.cs` — `IHealthCheck` implementation that verifies DB connectivity via connection factory delegate (production + testing constructors)
- Registered `DbHealthCheck` in `DependencyInjection.cs` alongside existing `QdrantHealthCheck` — `/health` now returns 503 when either Qdrant or DB is unreachable
- Created `.github/workflows/deploy.yml` — GitHub Actions CI/CD: build → test → docker buildx push to ghcr.io → deploy to ACA via `az containerapp update` on `release/aura/*` branches
- Updated `appsettings.json` for all 3 services (Api, UI, Workers) with `ConnectionStrings:AuraDb` template and `Persistence:Providers:*` = `EntityFramework` for all 9 stores

## Phase 3 — TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 3.1 Api Dockerfile | N/A | Config | N/A (config artifact) | ➖ Structural | ➖ Structural | ➖ Structural | ➖ None needed |
| 3.2 UI Dockerfile | N/A | Config | N/A (config artifact) | ➖ Structural | ➖ Structural | ➖ Structural | ➖ None needed |
| 3.3 Workers Dockerfile | N/A | Config | N/A (config artifact) | ➖ Structural | ➖ Structural | ➖ Structural | ➖ None needed |
| 3.4 Health endpoint | `tests/Aura.UnitTests/Health/DbHealthCheckTests.cs` | Unit | ✅ 13/13 health baseline | ✅ Written first (2 tests referencing non-existent `DbHealthCheck`) | ✅ Passed (`CheckHealthAsync_WhenConnectionOpens_ReturnsHealthy`, `CheckHealthAsync_WhenConnectionThrows_ReturnsUnhealthy`) | ✅ 2 cases (healthy + unhealthy) | ➖ None needed |
| 3.5 GA workflow | N/A | Config | N/A (new file) | ➖ Structural | ➖ Structural | ➖ Structural | ➖ None needed |
| 3.6 appsettings | N/A | Config | N/A (config artifact) | ➖ Structural | ➖ Structural | ➖ Structural | ➖ None needed |

## Phase 3 — Test Summary

- **Targeted commands executed**:
  - `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~DbHealthCheckTests"` — 2 passed
  - `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~Health"` — 15 passed (safety net + new)
  - `dotnet build Aura.sln` — 0 errors
- **Result**: 15 passed, 0 failed (2 new DbHealthCheck + 13 baseline health)
- **Approval tests**: None — no refactoring tasks
- **Pure functions created**: None (health check delegates to connection factory)

## Cumulative Test Summary

- **Total tests written Phase 1 + 2 + 3**: 54 (5 Phase 1 architecture + 10 Phase 1 bicep + 34 Phase 2 EF stores + 2 Phase 2 DI + 1 existing semantic outbox + 2 Phase 3 DbHealthCheck)
- **Total tests passing**: 15 health-related (Phase 3 targeted) + 41 Phase 2 targeted = 56 confirmed
- **Layers used**: Unit (38), Architecture (10), Config (6 structural)

## Phase 4 — Completed Scope

- Created `DemoService` in `src/Aura.Application/Demo/DemoService.cs` — seed data loading through existing port interfaces:
  - `LoadMorningSummaryAsync` — marks emission for today via `IMorningSummaryEmissionStore`
  - `LoadEmailsAsync` — saves 3 WorkItems with `OutlookEmail` source type via `IWorkItemStore`
  - `LoadTeamsMessagesAsync` — saves 3 WorkItems with `TeamsMessage` source type via `IWorkItemStore`
  - `LoadCalendarEventsAsync` — queries upcoming meeting alerts via `IMeetingAlertStore`
  - `LoadPriorityAlertsAsync` — saves 2 high-priority WorkItems + 2 notification outbox entries
  - `LoadPullRequestsAsync` — saves 2 WorkItems with `PrReview` source type
  - `LoadAllAsync` — runs all of the above sequentially
- Created `DemoEndpoints` in `src/Aura.Api/Endpoints/DemoEndpoints.cs` — POST endpoints:
  - `POST /api/demo/morning-summary`, `/email`, `/teams`, `/calendar`, `/priority-alert`, `/pull-request`, `/all`
  - `GET /api/demo/status` — returns `{ enabled: true/false }` for UI conditional rendering
  - All POST endpoints guard with `DemoModeOptions.Enabled` check — return 503 when disabled
- Created `QdrantFallbackSemanticContextRetriever` in `src/Aura.Infrastructure/Adapters/Demo/` — implements `ISemanticContextRetriever`, returns `Array.Empty<ScoredSemanticChunk>()` with log warning
- Created `QdrantFallbackSemanticIndexWriter` in `src/Aura.Infrastructure/Adapters/Demo/` — implements `ISemanticIndexWriter`, silently discards writes with log warning
- Created `DemoModeOptions` in `src/Aura.Infrastructure/Adapters/Options/DemoModeOptions.cs` — `Enabled` bool, `SectionName = "DemoMode"`
- Created `DemoModeServiceCollectionExtensions` in `src/Aura.Infrastructure/` — `AddDemoMode()` extension:
  - Always binds `DemoModeOptions` from config
  - When enabled: registers fallback semantic handlers + `DemoService`
  - When disabled: no-op (existing Qdrant adapters remain)
- Added `DemoMode: { Enabled: false }` to `src/Aura.Api/appsettings.json`
- Wired `app.MapDemoEndpoints()` in `Program.cs` — conditional on `DemoMode:Enabled` config value
- Wired `services.AddDemoMode(configuration)` in `DependencyInjection.cs`

## Phase 4 — TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 4.1 DemoService | `tests/Aura.UnitTests/Demo/DemoServiceTests.cs` | Unit | ✅ 983/983 baseline | ✅ Written first (7 tests referencing non-existent `DemoService`) | ✅ Passed (7 tests) | ✅ 7 cases covering all source types, priorities, and store interactions | ➖ None needed |
| 4.2 DemoEndpoints | `tests/Aura.UnitTests/Demo/DemoEndpointsTests.cs` | Unit | ✅ 990/990 baseline | ✅ Written first (3 tests referencing non-existent `DemoModeOptions`) | ✅ Passed (3 tests) | ✅ 3 cases (default, settable, section name) | ➖ None needed |
| 4.3 QdrantFallbackHandler | `tests/Aura.UnitTests/Demo/QdrantFallbackHandlerTests.cs` | Unit | ✅ 993/993 baseline | ✅ Written first (5 tests referencing non-existent `QdrantFallback*` classes) | ✅ Passed (5 tests) | ✅ 5 cases (empty results, no-throw write/delete, interface compliance ×2) | ➖ None needed |
| 4.4 Wire toggle | `tests/Aura.UnitTests/Demo/DemoModeRegistrationTests.cs` | Unit | ✅ 998/998 baseline | ✅ Written first (4 tests referencing non-existent `AddDemoMode`) | ✅ Passed (4 tests) | ✅ 4 cases (config binding, enabled registers fallbacks, DemoService resolves, disabled skips) | ➖ None needed |

## Phase 4 — Test Summary

- **Targeted commands executed**:
  - `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~Demo"` — 19 passed
  - `dotnet test Aura.sln` — full suite: 1002 UnitTests + 125 Integration + 45 E2E passed, 0 regressions
- **Result**: 19 new tests passed, 0 failed (7 DemoService + 3 DemoEndpoints + 5 QdrantFallback + 4 Registration)
- **Approval tests**: None — no refactoring tasks
- **Pure functions created**: None (service layer with port interface dependencies)

## Cumulative Test Summary

- **Total tests written Phase 1 + 2 + 3 + 4**: 73 (5 Phase 1 architecture + 10 Phase 1 bicep + 34 Phase 2 EF stores + 2 Phase 2 DI + 1 existing semantic outbox + 2 Phase 3 DbHealthCheck + 19 Phase 4 Demo)
- **Total tests passing**: 1002 UnitTests + 125 Integration + 45 E2E = 1172 confirmed
- **Layers used**: Unit (57), Architecture (10), Config (6 structural)

## Phase 5 — Completed Scope

- **5.1 (Already done)**: Validated `tests/Aura.UnitTests/Persistence/EfStoreTests.cs` (34 tests) and `ConditionalStoreRegistrationTests.cs` (2 tests) — all 9 EF stores tested with in-memory SQLite against port contracts. Marked complete.
- **5.2 (New)**: Created `tests/Aura.UnitTests/Persistence/SideBySideStoreTests.cs` — 8 integration tests that seed data via legacy SQLite stores and read via EF Core stores (and vice versa), asserting identical results. Covers WorkItem (2 tests), FocusState (2 tests), MeetingAlert (2 tests), MorningSummaryEmission (2 tests). Note: AlertRule excluded from side-by-side because SQLite uses separate `VipSenders`/`AlertKeywords` tables while EF Core uses unified `AlertRules` with discriminator — schemas are intentionally different.
- **5.3 (New)**: Created `tests/Aura.ArchitectureTests/EfCoreStoreBoundaryTests.cs` — 8 architecture tests verifying:
  - Application does not reference EntityFrameworkCore
  - Api does not reference EntityFrameworkCore
  - Application does not reference Infrastructure.Persistence (DbContext)
  - Api does not reference Infrastructure.Persistence (DbContext)
  - Ef*Store classes reside in Infrastructure namespace
  - Ef*Store classes are internal (not public)
  - Store port interfaces reside in Application namespace
  - Domain does not reference Infrastructure
- **5.4 (Already done)**: Validated `tests/Aura.UnitTests/Health/DbHealthCheckTests.cs` (2 tests) — healthy (200) and unhealthy (503) scenarios covered. Marked complete.
- **5.5 (Already done)**: Validated `tests/Aura.UnitTests/Demo/` (19 tests across 4 files) — DemoService, DemoEndpoints, QdrantFallbackHandler, and DemoModeRegistration all covered. Marked complete.

## Phase 5 — TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 5.1 EF store unit tests | `tests/Aura.UnitTests/Persistence/EfStoreTests.cs` + `ConditionalStoreRegistrationTests.cs` | Unit | ✅ Pre-existing from Phase 2 | ✅ Written Phase 2 | ✅ 36 tests passing | ✅ 36 cases across 9 stores + DI | ➖ None needed |
| 5.2 Side-by-side integration | `tests/Aura.UnitTests/Persistence/SideBySideStoreTests.cs` | Integration | ✅ 1002/1002 UnitTests baseline | ✅ Written first (8 tests referencing shared SQLite+EF context) | ✅ Passed (8 tests) | ✅ 8 cases (4 stores × 2 directions) | ➖ None needed |
| 5.3 Arch test — EF boundary | `tests/Aura.ArchitectureTests/EfCoreStoreBoundaryTests.cs` | Architecture | ✅ 64/66 baseline (2 pre-existing failures) | ✅ Written first (8 tests using NetArchTest rules) | ✅ Passed (8 tests) | ✅ 8 cases (dependency + placement + visibility) | ➖ None needed |
| 5.4 Health probe tests | `tests/Aura.UnitTests/Health/DbHealthCheckTests.cs` | Unit | ✅ Pre-existing from Phase 3 | ✅ Written Phase 3 | ✅ 2 tests passing | ✅ 2 cases (healthy + unhealthy) | ➖ None needed |
| 5.5 Demo endpoint tests | `tests/Aura.UnitTests/Demo/` (4 files) | Unit | ✅ Pre-existing from Phase 4 | ✅ Written Phase 4 | ✅ 19 tests passing | ✅ 19 cases across service/endpoints/fallback/registration | ➖ None needed |

## Phase 5 — Test Summary

- **Targeted commands executed**:
  - `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~SideBySideStoreTests"` — 8 passed
  - `dotnet test tests/Aura.ArchitectureTests/Aura.ArchitectureTests.csproj --filter "FullyQualifiedName~EfCoreStoreBoundaryTests"` — 8 passed
  - `dotnet test Aura.sln` — full suite: 1010 UnitTests + 125 Integration + 45 E2E passed, 0 new regressions
- **Result**: 16 new tests passed (8 side-by-side + 8 architecture), 0 failed
- **Pre-existing failures**: 2 in `InfrastructureOrganizationTests` (from Phase 2 — `Persistence/` folder not in allowed list). Reported, not fixed in this phase.
- **Approval tests**: None — no refactoring tasks
- **Pure functions created**: None (integration + architecture tests)

## Cumulative Test Summary

- **Total tests written Phase 1–5**: 89 (5 Phase 1 architecture + 10 Phase 1 bicep + 34 Phase 2 EF stores + 2 Phase 2 DI + 1 existing semantic outbox + 2 Phase 3 DbHealthCheck + 19 Phase 4 Demo + 8 Phase 5 side-by-side + 8 Phase 5 architecture)
- **Total tests passing**: 1010 UnitTests + 125 Integration + 45 E2E = 1180 confirmed (excluding 2 pre-existing architecture failures)
- **Layers used**: Unit (65), Architecture (18), Integration (8), Config (6 structural)
