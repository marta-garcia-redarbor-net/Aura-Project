# Apply Progress: W2-H6 Dashboard Inbox-by-Source and Morning Summary Preview

## Change
- **Name**: `W2-H6`
- **Mode**: Strict TDD
- **Artifact store**: OpenSpec
- **Workload decision**: `exception-ok` with maintainer-approved `size:exception` (single implementation batch above 400 changed lines)

## Completed Tasks

### Phase 1: Foundation / Contracts
- [x] 1.1 Created `DashboardPreviewDto`, `InboxSourceGroupDto`, `InboxItemPreviewDto`, and `SummaryPreviewEntryDto` in Application models.
- [x] 1.2 Created `IDashboardPreviewReader` Application port.
- [x] 1.3 Registered `IDashboardPreviewReader` as scoped in Application DI while keeping `WorkItem` usage internal to Application/Domain.

### Phase 2: Core API Slice (TDD)
- [x] 2.1 Added integration RED tests for `GET /api/dashboard/preview` (200 populated, 200 empty, 401, 500).
- [x] 2.2 Implemented `DashboardPreviewReader` projection service using `IWorkItemReader`, `IMorningSummaryRankingPolicy`, and `ICurrentUserService`.
- [x] 2.3 Added API endpoint mapping `GET /api/dashboard/preview` with activity tags and source-generated logs.
- [x] 2.4 Refactored/validated response contract to keep only dashboard DTO fields and avoid domain aggregate leakage.

### Phase 3: UI Integration Slice (TDD)
- [x] 3.1 Extended UI smoke tests with `IDashboardPreviewApiClient` stubs for loading/empty/error/populated states.
- [x] 3.2 Added UI response DTOs and preview API client (`GET /api/dashboard/preview`).
- [x] 3.3 Registered preview API client in UI Program with `DevAccessTokenHandler` in development.
- [x] 3.4 Implemented `InboxPreviewPanel` and `MorningSummaryPreviewPanel` with explicit loading/empty/error/populated states and stable `data-testid` markers.
- [x] 3.5 Mounted both preview panels in `Index.razor` after existing dashboard panels, keeping panels presentation-only.

### Phase 4: Verification / Boundary Enforcement
- [x] 4.1 Added architecture rules asserting `Aura.UI.Models` and dashboard endpoint types do not depend on `Aura.Domain`.
- [x] 4.2 Extended preview endpoint integration tests to assert JSON payload contains only dashboard preview fields.
- [x] 4.3 Executed `dotnet test Aura.sln` and `dotnet test Aura.sln --collect:"XPlat Code Coverage"` successfully.

### Corrective Apply Batch: Full-Suite Verify Blocker (2026-06-23)
- [x] Investigated unrelated full-suite failure in `ExecuteConnectorUseCaseTests.ExecuteAsync_Success_EmitsCorrelatedTraceMetricAndInfoLog`.
- [x] Added strict correlation filtering in telemetry assertions (activity tags + correlation id), so the test validates only its own execution and ignores cross-test noise.
- [x] Added deterministic noise-activity guard inside the telemetry tests to prove isolation against extra activities published by the same `ActivitySource`.
- [x] Re-ran required full-suite commands (`dotnet test Aura.sln` and coverage run) and confirmed green status.

## TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1 | `tests/Aura.IntegrationTests/Dashboard/DashboardPreviewEndpointTests.cs` | Integration | N/A (new) | ✅ Compile RED due to missing DTOs | ✅ Tests compile/pass after DTO creation | ➖ Structural | ➖ None needed |
| 1.2 | `tests/Aura.IntegrationTests/Dashboard/DashboardPreviewEndpointTests.cs` | Integration | N/A (new) | ✅ Compile RED due to missing port | ✅ Tests compile/pass after port creation | ➖ Structural | ➖ None needed |
| 1.3 | `tests/Aura.UnitTests/Application/DependencyInjectionTests.cs` | Unit | ✅ 8/8 dashboard/app DI baseline | ✅ Added DI resolution assertions first | ✅ 12/12 targeted unit tests passed | ✅ Descriptor + runtime resolution paths | ✅ Added optional constructor path for safe DI |
| 2.1 | `tests/Aura.IntegrationTests/Dashboard/DashboardPreviewEndpointTests.cs` | Integration | ✅ 6/6 initial dashboard endpoint baseline | ✅ Written first; compile + runtime RED observed | ✅ 4/4 then 5/5 preview integration tests passed | ✅ Populated + empty + unauthorized + failure + shape-only checks | ✅ Consolidated reusable auth/payload helpers |
| 2.2 | `tests/Aura.UnitTests/Dashboard/DashboardPreviewReaderTests.cs` | Unit | N/A (new) | ✅ Reader tests written first | ✅ 2/2 reader tests passed | ✅ Ranked/non-empty + optional-reader-empty branches | ✅ Extracted relative time and action helpers in service |
| 2.3 | `tests/Aura.IntegrationTests/Dashboard/DashboardPreviewEndpointTests.cs` | Integration | ✅ 6/6 baseline | ✅ Endpoint tests failed before route/handler mapping | ✅ Preview endpoint tests passed after handler + mapping | ✅ Success + error telemetry path | ✅ Source-generated log entries aligned with existing endpoint style |
| 2.4 | `tests/Aura.IntegrationTests/Dashboard/DashboardPreviewEndpointTests.cs` | Integration | ✅ Preview tests already green before schema guard | ✅ Added field-shape assertions first | ✅ Shape-only test passed (no domain fields) | ✅ Positive and negative field assertions | ✅ Kept API response contract slim and explicit |
| 3.1 | `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` | E2E (WebApplicationFactory) | ✅ 13/13 dashboard smoke baseline | ✅ New smoke tests authored first | ✅ 4/4 targeted preview state tests passed | ✅ Loading/empty/error/populated paths | ✅ Shared `CreateClient` overload extended for preview client |
| 3.2 | `tests/Aura.UnitTests/Dashboard/DashboardApiClientTests.cs` + E2E preview state tests | Unit + E2E | ✅ Existing UI API client tests passing | ✅ E2E tests failed before preview client/model creation | ✅ E2E preview state tests passed after client + models | ✅ Client usage across all UI states | ✅ Mirrored existing `DashboardApiClient` pattern |
| 3.3 | `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` | E2E | ✅ Existing smoke baseline green | ✅ Preview tests depend on DI wiring and failed prior to registration | ✅ 17/17 full `InitialDashboardSmokeTests` passed | ✅ Dev + non-dev registration paths indirectly covered by host boot | ✅ Reused existing typed HttpClient registration conventions |
| 3.4 | `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` | E2E | ✅ 13/13 baseline | ✅ Panel marker assertions written first | ✅ All preview panel state markers render | ✅ Multiple item/field assertions in populated state | ✅ Kept components presentation-only, no business logic |
| 3.5 | `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` | E2E | ✅ Preview panel tests green before page mount | ✅ Mount-dependent tests ensured Index wiring required | ✅ Mount and rendering verified in full smoke run | ✅ Both panels visible with existing dashboard panels | ✅ Minimal additive changes to `Index.razor` |
| 4.1 | `tests/Aura.ArchitectureTests/DashboardArchitectureTests.cs` | Architecture | ✅ 2/2 architecture baseline | ✅ Added architecture assertions first | ✅ 4/4 dashboard architecture tests passed | ✅ UI model boundary + endpoint boundary | ✅ Reused existing NetArchTest helper style |
| 4.2 | `tests/Aura.IntegrationTests/Dashboard/DashboardPreviewEndpointTests.cs` | Integration | ✅ 4/4 preview endpoint suite baseline | ✅ Added explicit JSON shape assertions first | ✅ 5/5 preview endpoint tests passed | ✅ Confirms required fields + rejects domain/internal fields | ✅ No DTO contract broadening |
| 4.3 | `Aura.sln` test suites | Full suite | ✅ All targeted suites green before full run | ✅ N/A (verification task) | ✅ `dotnet test Aura.sln` and coverage run passed | ➖ Full-suite verification task | ➖ None needed |
| 4.3 corrective | `tests/Aura.UnitTests/Ingestion/ExecuteConnectorUseCaseTests.cs` + `Aura.sln` full suites | Unit + Full suite | ✅ 18/18 `ExecuteConnectorUseCaseTests` baseline | ✅ Strengthened assertions first; local RED confirmed (`Assert.Single` empty on prior filtering) | ✅ 2/2 targeted telemetry tests passed after fix; full `Aura.UnitTests` and solution suites passed | ✅ Success and failure telemetry paths both asserted with identity tags + correlation id filtering; explicit noise activity included | ✅ Extracted common helpers (`GetActivityTag`, `EmitNoiseActivityForConnectorExecutionSource`) to keep assertions readable |

## Test Summary
- **Total tests written/extended for W2-H6**: 15
  - Unit: 4 (reader + DI coverage)
  - Integration: 5 (`DashboardPreviewEndpointTests`)
  - E2E smoke: 4 (`InitialDashboardSmokeTests` preview states)
  - Architecture: 2 (`DashboardArchitectureTests`)
- **Targeted test runs**:
  - Unit targeted: 12/12 passing
  - Integration targeted: 5/5 passing
  - E2E dashboard smoke: 17/17 passing
  - Architecture dashboard: 4/4 passing
- **Full suite**:
  - `dotnet test Aura.sln` passed (Unit 371, Integration 61, E2E 25, Architecture 33)
  - `dotnet test Aura.sln --collect:"XPlat Code Coverage"` passed; coverage artifacts generated for all test projects
- **Corrective verification rerun**:
  - `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~Aura.UnitTests.Ingestion.ExecuteConnectorUseCaseTests.ExecuteAsync_Success_EmitsCorrelatedTraceMetricAndInfoLog|FullyQualifiedName~Aura.UnitTests.Ingestion.ExecuteConnectorUseCaseTests.ExecuteAsync_Failure_EmitsCorrelatedTraceMetricZeroAndErrorLog"` passed (2/2)
  - `dotnet test Aura.sln` passed (Unit 371, Integration 61, E2E 25, Architecture 33)
  - `dotnet test Aura.sln --collect:"XPlat Code Coverage"` passed (all suites green, coverage artifacts emitted)

## Files Changed

### Created
- `src/Aura.Application/Models/DashboardPreviewDto.cs`
- `src/Aura.Application/Ports/IDashboardPreviewReader.cs`
- `src/Aura.Application/Services/DashboardPreviewReader.cs`
- `src/Aura.UI/Models/DashboardPreviewResponse.cs`
- `src/Aura.UI/Services/IDashboardPreviewApiClient.cs`
- `src/Aura.UI/Services/DashboardPreviewApiClient.cs`
- `src/Aura.UI/Components/Dashboard/InboxPreviewPanel.razor`
- `src/Aura.UI/Components/Dashboard/MorningSummaryPreviewPanel.razor`
- `tests/Aura.IntegrationTests/Dashboard/DashboardPreviewEndpointTests.cs`
- `tests/Aura.UnitTests/Dashboard/DashboardPreviewReaderTests.cs`

### Modified
- `tests/Aura.UnitTests/Ingestion/ExecuteConnectorUseCaseTests.cs`
- `src/Aura.Application/DependencyInjection.cs`
- `src/Aura.Api/Endpoints/DashboardEndpoints.cs`
- `src/Aura.UI/Program.cs`
- `src/Aura.UI/Pages/Index.razor`
- `tests/Aura.UnitTests/Application/DependencyInjectionTests.cs`
- `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs`
- `tests/Aura.ArchitectureTests/DashboardArchitectureTests.cs`
- `openspec/changes/W2-H6/tasks.md`

## Status
- **15/15 tasks complete**
- **Workload guard satisfied**: implemented under maintainer-approved `size:exception` in one batch
- **Corrective blocker resolved**: required full-suite commands are green again
- **Ready for `sdd-verify`**
