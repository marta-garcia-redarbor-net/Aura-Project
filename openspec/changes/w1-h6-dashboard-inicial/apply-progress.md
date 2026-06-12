# Apply Progress: W1-H6 Dashboard Inicial

**Change**: w1-h6-dashboard-inicial
**Mode**: Strict TDD
**Status**: 6/12 tasks complete
**Workload Mode**: stacked PR slice
**Current Work Unit**: PR 1 / Unit 1
**Boundary**: API + Application + tests only; no Aura.UI host, Stitch assets, or smoke tests in this batch.

## Completed Tasks

- [x] 1.1 RED: Add failing `tests/Aura.UnitTests/Dashboard/InitialDashboardReaderTests.cs` for populated and empty results from `IInitialDashboardReader`.
- [x] 1.2 GREEN: Create `src/Aura.Application/Models/InitialDashboardDto.cs`, `Ports/IInitialDashboardReader.cs`, `Services/InitialDashboardReader.cs`, and register it in `DependencyInjection.cs`.
- [x] 1.3 REFACTOR: Extract shared card-building/null-guard logic inside `src/Aura.Application/Services/InitialDashboardReader.cs`; keep framework types out of Application.
- [x] 2.1 RED: Add failing `tests/Aura.IntegrationTests/Dashboard/InitialDashboardEndpointTests.cs` for `401`, `200` populated, and `200` empty on `GET /api/dashboard/initial`.
- [x] 2.2 GREEN: Create `src/Aura.Api/Endpoints/DashboardEndpoints.cs` and update `src/Aura.Api/Program.cs` to map the endpoint through `IInitialDashboardReader`.
- [x] 2.3 REFACTOR: Add request/error telemetry in `src/Aura.Api/Endpoints/DashboardEndpoints.cs` and `src/Aura.Api/Program.cs` without moving business rules out of Application.

## TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1 | `tests/Aura.UnitTests/Dashboard/InitialDashboardReaderTests.cs` | Unit | ✅ `DependencyInjectionTests` 2/2 passing | ✅ Written first; failed with missing `IInitialDashboardReader`/`InitialDashboardReader` types | ✅ `InitialDashboardReaderTests` 2/2 passing | ✅ 2 cases (populated + empty) | ✅ Shared null-guard/card building extracted in `InitialDashboardReader` |
| 1.2 | `tests/Aura.UnitTests/Dashboard/InitialDashboardReaderTests.cs` | Unit | ✅ `DependencyInjectionTests` 2/2 passing | ✅ Written first via task 1.1 | ✅ `InitialDashboardReaderTests` 2/2 passing | ✅ 2 cases force DTO + service behavior | ✅ DI registration kept in Application and framework types stayed out |
| 1.3 | `tests/Aura.UnitTests/Dashboard/InitialDashboardReaderTests.cs` | Unit | ✅ `DependencyInjectionTests` 2/2 passing | ✅ Existing red coverage from 1.1 | ✅ `InitialDashboardReaderTests` 2/2 passing after refactor | ✅ Empty/populated paths still covered | ✅ Extracted `Normalize`, `CreateCard`, and card-building helpers |
| 2.1 | `tests/Aura.IntegrationTests/Dashboard/InitialDashboardEndpointTests.cs` | Integration | ✅ `AuthorizationFlowTests` 4/4 passing | ✅ Written first; failed with `404 NotFound` because endpoint was unmapped | ✅ `InitialDashboardEndpointTests` 3/3 passing | ✅ 3 cases (`401`, `200` populated, `200` empty) | ✅ Test helper stubs isolated endpoint behavior |
| 2.2 | `tests/Aura.IntegrationTests/Dashboard/InitialDashboardEndpointTests.cs` | Integration | ✅ `AuthorizationFlowTests` 4/4 passing | ✅ Written first via task 2.1 | ✅ `InitialDashboardEndpointTests` 3/3 passing | ✅ Auth + populated + empty paths verified through HTTP pipeline | ✅ `Program.cs` now registers Application before mapping dashboard endpoints |
| 2.3 | `tests/Aura.IntegrationTests/Dashboard/InitialDashboardEndpointTests.cs` | Integration | ✅ `AuthorizationFlowTests` 4/4 passing | ✅ Existing red coverage from 2.1 | ✅ `InitialDashboardEndpointTests` 3/3 passing and `AuthorizationFlowTests` 4/4 still passing | ✅ Request success/error path instrumentation preserved endpoint behavior | ✅ Added `ActivitySource` tags plus source-generated logging in API only |

## Test Summary

- **Total tests written**: 5
- **Total tests passing**: 9 relevant tests (`InitialDashboardReaderTests` 2 + `InitialDashboardEndpointTests` 3 + `AuthorizationFlowTests` 4)
- **Layers used**: Unit (2), Integration (7), E2E (0)
- **Approval tests** (refactoring): None — behavior changes were covered by the red/green task tests
- **Pure functions created**: 3 (`Normalize`, `CreateCard`, `CreateCards`/shared card-building path)

## Files Changed

| File | Action | What Was Done |
|------|--------|---------------|
| `src/Aura.Application/Models/InitialDashboardDto.cs` | Created | Added the dashboard DTO contract for API/UI transport |
| `src/Aura.Application/Ports/IInitialDashboardReader.cs` | Created | Added the Application port for the initial dashboard capability |
| `src/Aura.Application/Services/InitialDashboardReader.cs` | Created | Implemented dashboard composition from `ICurrentUserService` with shared null/card helpers |
| `src/Aura.Application/DependencyInjection.cs` | Modified | Registered `IInitialDashboardReader` in Application DI |
| `src/Aura.Api/Endpoints/DashboardEndpoints.cs` | Created | Added `GET /api/dashboard/initial` plus request/error telemetry |
| `src/Aura.Api/Program.cs` | Modified | Registered Application services, mapped dashboard endpoints, and added dashboard request middleware telemetry |
| `tests/Aura.UnitTests/Dashboard/InitialDashboardReaderTests.cs` | Created | Added populated and empty unit tests for the Application service |
| `tests/Aura.IntegrationTests/Dashboard/InitialDashboardEndpointTests.cs` | Created | Added unauthorized, populated, and empty endpoint contract tests |
| `openspec/changes/w1-h6-dashboard-inicial/tasks.md` | Modified | Marked only PR 1 completed tasks as done |

## Deviations from Design

None — implementation matches design for the PR 1 backend slice.

## Issues Found

- A transient Windows file-lock on `Aura.Api.dll`/`Aura.Domain.dll` interrupted two test runs; rerunning the targeted commands succeeded without code changes.

## Remaining Tasks

- [ ] 1.4 GREEN: Create `src/Aura.UI/Aura.UI.csproj` and `src/Aura.UI/Program.cs`, then add `Aura.UI` to `Aura.sln` with HTTP-only dependencies.
- [ ] 3.1 RED: Add failing `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs`; update `tests/Aura.E2E/Aura.E2E.csproj` with `Microsoft.AspNetCore.Mvc.Testing` and `Aura.UI` reference.
- [ ] 3.2 GREEN: Create `src/Aura.UI/Components/Layout/MainLayout.razor`, `Sidebar.razor`, `Header.razor`, `Pages/Index.razor`, `Models/InitialDashboardResponse.cs`, and `Services/DashboardApiClient.cs`.
- [ ] 3.3 GREEN: Import minimal Stitch assets into `src/Aura.UI/wwwroot/`, configure API base URL/token forwarding in `src/Aura.UI/Program.cs`, and render loading/empty/error/populated states with stable markers.
- [ ] 3.4 REFACTOR: Split repeated render/state mapping into small UI helpers/components; keep Blazor files presentation-only.
- [ ] 4.1 VERIFY: Run `dotnet test Aura.sln` and confirm unit, integration, and smoke coverage satisfy the spec scenarios.
- [ ] 4.2 CLEANUP: Document the HTTP-only boundary and non-Playwright smoke scope in test names/comments where added; remove unused imported Stitch assets.
