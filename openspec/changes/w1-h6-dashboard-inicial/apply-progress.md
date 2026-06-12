# Apply Progress: W1-H6 Dashboard Inicial

**Change**: w1-h6-dashboard-inicial
**Mode**: Strict TDD
**Status**: 10/12 tasks complete (+ PR 2 Stitch refinement complete)
**Workload Mode**: stacked PR slice
**Current Work Unit**: PR 2 / Unit 2 refinement
**Boundary**: Aura.UI host + shell slice adapted to real Stitch export; no PR 3 verify/cleanup work beyond what was required to keep this refinement coherent.

## Completed Tasks

- [x] 1.1 RED: Add failing `tests/Aura.UnitTests/Dashboard/InitialDashboardReaderTests.cs` for populated and empty results from `IInitialDashboardReader`.
- [x] 1.2 GREEN: Create `src/Aura.Application/Models/InitialDashboardDto.cs`, `Ports/IInitialDashboardReader.cs`, `Services/InitialDashboardReader.cs`, and register it in `DependencyInjection.cs`.
- [x] 1.3 REFACTOR: Extract shared card-building/null-guard logic inside `src/Aura.Application/Services/InitialDashboardReader.cs`; keep framework types out of Application.
- [x] 2.1 RED: Add failing `tests/Aura.IntegrationTests/Dashboard/InitialDashboardEndpointTests.cs` for `401`, `200` populated, and `200` empty on `GET /api/dashboard/initial`.
- [x] 2.2 GREEN: Create `src/Aura.Api/Endpoints/DashboardEndpoints.cs` and update `src/Aura.Api/Program.cs` to map the endpoint through `IInitialDashboardReader`.
- [x] 2.3 REFACTOR: Add request/error telemetry in `src/Aura.Api/Endpoints/DashboardEndpoints.cs` and `src/Aura.Api/Program.cs` without moving business rules out of Application.
- [x] 1.4 GREEN: Create `src/Aura.UI/Aura.UI.csproj` and `src/Aura.UI/Program.cs`, then add `Aura.UI` to `Aura.sln` with HTTP-only dependencies.
- [x] 3.1 RED: Add failing `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs`; update `tests/Aura.E2E/Aura.E2E.csproj` with `Microsoft.AspNetCore.Mvc.Testing` and `Aura.UI` reference.
- [x] 3.2 GREEN: Create `src/Aura.UI/Components/Layout/MainLayout.razor`, `Sidebar.razor`, `Header.razor`, `Pages/Index.razor`, `Models/InitialDashboardResponse.cs`, and `Services/DashboardApiClient.cs`.
- [x] 3.3 GREEN: Import minimal Stitch assets into `src/Aura.UI/wwwroot/`, configure API base URL/token forwarding in `src/Aura.UI/Program.cs`, and render loading/empty/error/populated states with stable markers.
- [x] 3.4 REFACTOR: Split repeated render/state mapping into small UI helpers/components; keep Blazor files presentation-only.
- [x] **PR 2 REFINEMENT**: Adapt Aura.UI shell to real Stitch export — replace placeholder light-theme CSS with dark-theme tokens, align layout/sidebar/header/cards structure with the exported design, add Google Fonts/Material Symbols links, and extend smoke tests for Stitch alignment.

## TDD Cycle Evidence

### Prior batches (PR 1 + PR 2 initial)

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1 | `tests/Aura.UnitTests/Dashboard/InitialDashboardReaderTests.cs` | Unit | ✅ `DependencyInjectionTests` 2/2 passing | ✅ Written first; failed with missing `IInitialDashboardReader`/`InitialDashboardReader` types | ✅ `InitialDashboardReaderTests` 2/2 passing | ✅ 2 cases (populated + empty) | ✅ Shared null-guard/card building extracted in `InitialDashboardReader` |
| 1.2 | `tests/Aura.UnitTests/Dashboard/InitialDashboardReaderTests.cs` | Unit | ✅ `DependencyInjectionTests` 2/2 passing | ✅ Written first via task 1.1 | ✅ `InitialDashboardReaderTests` 2/2 passing | ✅ 2 cases force DTO + service behavior | ✅ DI registration kept in Application and framework types stayed out |
| 1.3 | `tests/Aura.UnitTests/Dashboard/InitialDashboardReaderTests.cs` | Unit | ✅ `DependencyInjectionTests` 2/2 passing | ✅ Existing red coverage from 1.1 | ✅ `InitialDashboardReaderTests` 2/2 passing after refactor | ✅ Empty/populated paths still covered | ✅ Extracted `Normalize`, `CreateCard`, and card-building helpers |
| 2.1 | `tests/Aura.IntegrationTests/Dashboard/InitialDashboardEndpointTests.cs` | Integration | ✅ `AuthorizationFlowTests` 4/4 passing | ✅ Written first; failed with `404 NotFound` because endpoint was unmapped | ✅ `InitialDashboardEndpointTests` 3/3 passing | ✅ 3 cases (`401`, `200` populated, `200` empty) | ✅ Test helper stubs isolated endpoint behavior |
| 2.2 | `tests/Aura.IntegrationTests/Dashboard/InitialDashboardEndpointTests.cs` | Integration | ✅ `AuthorizationFlowTests` 4/4 passing | ✅ Written first via task 2.1 | ✅ `InitialDashboardEndpointTests` 3/3 passing | ✅ Auth + populated + empty paths verified through HTTP pipeline | ✅ `Program.cs` now registers Application before mapping dashboard endpoints |
| 2.3 | `tests/Aura.IntegrationTests/Dashboard/InitialDashboardEndpointTests.cs` | Integration | ✅ `AuthorizationFlowTests` 4/4 passing | ✅ Existing red coverage from 2.1 | ✅ `InitialDashboardEndpointTests` 3/3 passing and `AuthorizationFlowTests` 4/4 still passing | ✅ Request success/error path instrumentation preserved endpoint behavior | ✅ Added `ActivitySource` tags plus source-generated logging in API only |
| 1.4 | `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` | E2E smoke via `WebApplicationFactory` | ✅ `UnitTest1` 1/1 passing | ✅ Written first; failed because `Aura.UI`/`UiMarker` and the project reference did not exist | ✅ `InitialDashboardSmokeTests` 4/4 passing and `dotnet sln Aura.sln list` shows `src/Aura.UI/Aura.UI.csproj` | ✅ Same smoke file covers loading, empty, error, and populated shell paths | ➖ Triangulation was already forcing the real host wiring; no extra refactor needed beyond the shared client/setup extraction |
| 3.1 | `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` | E2E smoke via `WebApplicationFactory` | ✅ `UnitTest1` 1/1 passing | ✅ Written first; failed with missing `Aura.UI` types/project | ✅ `InitialDashboardSmokeTests` 4/4 passing | ✅ 4 cases (loading, empty, error, populated) | ✅ Shared factory/client setup extracted in the test fixture |
| 3.2 | `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` | E2E smoke via `WebApplicationFactory` | ✅ `UnitTest1` 1/1 passing | ✅ Written first via task 3.1 | ✅ `InitialDashboardSmokeTests` 4/4 passing after adding layout, page, models, and HTTP client | ✅ Shell markers plus populated rendering exercised through the host | ✅ Presentation split into layout + dashboard components |
| 3.3 | `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` | E2E smoke via `WebApplicationFactory` | ✅ `UnitTest1` 1/1 passing | ✅ Written first via task 3.1 | ✅ `InitialDashboardSmokeTests` 4/4 passing after base URL/token handler and `wwwroot/css/stitch-dashboard.css` were wired | ✅ Loading, empty, error, and populated states all render stable markers | ✅ HTTP-only client/handler kept API access out of components |
| 3.4 | `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` | E2E smoke via `WebApplicationFactory` | ✅ `UnitTest1` 1/1 passing | ✅ Existing red coverage from 3.1 | ✅ `InitialDashboardSmokeTests` 4/4 passing after refactor | ✅ Different response shapes still pass through `DashboardViewStateMapper` and small presentation components | ✅ Extracted `DashboardViewStateMapper`, `DashboardStatePanel`, and `DashboardCards` to keep `Index.razor` presentation-only |

### PR 2 Stitch refinement batch

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| PR2-R1 (Stitch alignment) | `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` | E2E smoke via `WebApplicationFactory` | ✅ 4/4 `InitialDashboardSmokeTests` passing before changes | ✅ Added `GetRootRendersStitchAlignedDarkThemeShell` (failed: `class="dark"` missing, nav items missing) and extended `GetRootWithDashboardCardsRendersPopulatedState` with `data-testid="dashboard-card-status"` (failed: marker missing) | ✅ 5/5 `InitialDashboardSmokeTests` passing after CSS rewrite, App.razor dark class + font links, sidebar/header/cards structure alignment | ✅ New test covers 5 distinct assertions (dark class, Google Fonts link, Dashboard nav, Health nav, Aura Core brand); card test triangulates status marker with 2 cards | ✅ CSS rewritten with extracted tokens from Stitch Tailwind config; no inline styles left; layout uses semantic BEM classes |

## Test Summary

- **Total tests written**: 11 (9 prior + 2 new assertions in this batch)
- **Total tests passing**: 241 full suite (`UnitTests` 193 + `IntegrationTests` 27 + `E2E` 6 + `ArchitectureTests` 15)
- **Layers used**: Unit (193), Integration (27), E2E (6), Architecture (15)
- **Approval tests** (refactoring): Safety net only — 4/4 existing smoke tests served as approval baseline before refactoring UI markup
- **Pure functions created**: 4 (prior batches) + 0 (this batch — CSS/markup only)

## Files Changed

### Prior batches (PR 1 + PR 2 initial)

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
| `Aura.sln` | Modified | Added `Aura.UI` to the solution for the PR 2 host slice |
| `src/Aura.UI/Aura.UI.csproj` | Created | Added the separate Blazor Server UI host with HTTP-only dependencies |
| `src/Aura.UI/Program.cs` | Created | Registered Razor components, the typed dashboard HTTP client, and token forwarding |
| `src/Aura.UI/Components/Routes.razor` | Created | Added router wiring with `MainLayout` as the default shell |
| `src/Aura.UI/Models/InitialDashboardResponse.cs` | Created | Added local UI transport contracts matching the API JSON shape |
| `src/Aura.UI/Models/DashboardViewState.cs` | Created | Added small UI state mapping helpers to keep Blazor files presentation-only |
| `src/Aura.UI/Services/IDashboardApiClient.cs` | Created | Added the HTTP-only dashboard client contract for the UI host |
| `src/Aura.UI/Services/DashboardApiClient.cs` | Created | Added the typed HTTP client for `GET /api/dashboard/initial` |
| `src/Aura.UI/Services/ForwardedAccessTokenHandler.cs` | Created | Added Authorization header forwarding from the UI request to Aura.Api |
| `src/Aura.UI/_Imports.razor` | Created | Added shared component namespaces and render-mode imports |
| `src/Aura.UI/appsettings.json` | Created | Added Aura.Api base URL configuration for the UI host |
| `src/Aura.UI/Properties/launchSettings.json` | Created | Added local launch profile for Aura.UI |
| `tests/Aura.E2E/Aura.E2E.csproj` | Modified | Added MVC testing package and swapped the project reference from API to Aura.UI |

### PR 2 Stitch refinement batch (this apply)

| File | Action | What Was Done |
|------|--------|---------------|
| `src/Aura.UI/Components/App.razor` | Modified | Added `class="dark"` on `<html>`, Google Fonts preconnect + Inter/JetBrains Mono/Material Symbols stylesheet links |
| `src/Aura.UI/wwwroot/css/stitch-dashboard.css` | Rewritten | Replaced placeholder light-theme CSS with dark-theme design tokens extracted from Stitch export Tailwind config; BEM semantic classes for sidebar, header, cards, panels |
| `src/Aura.UI/Components/Layout/MainLayout.razor` | Modified | Adapted to Stitch layout: fixed header + fixed sidebar + scrollable main content; removed old Title/Subtitle parameter passing |
| `src/Aura.UI/Components/Layout/Sidebar.razor` | Modified | Replaced placeholder nav items with Stitch-aligned items (Dashboard, Health, Modules, Tasks, Logs) with Material Symbols icons; added brand section with "Aura Core" title and version |
| `src/Aura.UI/Components/Layout/Header.razor` | Modified | Replaced parameterized header with Stitch-aligned fixed top bar: brand "Aura", development nav, terminal/settings icon buttons; removed `Title`/`Subtitle` `[Parameter]`s |
| `src/Aura.UI/Components/Dashboard/DashboardCards.razor` | Modified | Added Stitch-aligned card structure with header/value/status layout; added `data-testid="dashboard-card-status"` marker with per-status CSS modifier |
| `src/Aura.UI/Components/Dashboard/DashboardStatePanel.razor` | Modified | No structural changes — preserved identical markup and all data-testid markers |
| `src/Aura.UI/Pages/Index.razor` | Modified | Added Stitch-aligned page header ("System Status" title, subtitle, "Live Sync" badge) above state panel and cards |
| `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` | Modified | Added `GetRootRendersStitchAlignedDarkThemeShell` test (dark class, fonts, nav items, brand); extended populated test with `dashboard-card-status` marker assertion |

## Deviations from Design

- **Header parameters removed**: The design described `Header Title="..." Subtitle="..."` parameters. These were replaced with hardcoded Stitch-aligned content (brand "Aura", dev nav) because the real Stitch export uses a fixed application header, not a per-page parameterized one. The page-level title/subtitle moved to `Index.razor` as a `dashboard-page-header` section, matching the Stitch export structure.
- **Google Fonts CDN dependency**: The Stitch export uses Inter, JetBrains Mono, and Material Symbols Outlined via Google Fonts CDN. This is kept as-is for pragmatic parity with the export. For production, these should be self-hosted. Documented as a known trade-off.
- **No Tailwind CDN**: The Stitch export uses a Tailwind CDN script. We extracted the design tokens into CSS custom properties and wrote semantic CSS classes instead. This avoids a runtime Tailwind dependency.

## Issues Found

- **Prior batches**: A transient Windows file-lock on `Aura.Api.dll`/`Aura.Domain.dll` interrupted two PR 1 test runs; rerunning the targeted commands succeeded without code changes.
- **Prior batches**: The repository lacked a committed Stitch export — now resolved with the real export at `src/Aura.UI/wwwroot/stitch-export/dashboard-operativa/`.
- **Build cache issue**: A transient `MSB3492` file-lock error on `Aura.Workers.AssemblyInfoInputs.cache` required a `dotnet clean` before rebuild; no code change needed.

## Remaining Tasks

- [ ] 4.1 VERIFY: Run `dotnet test Aura.sln` and confirm unit, integration, and smoke coverage satisfy the spec scenarios.
- [ ] 4.2 CLEANUP: Document the HTTP-only boundary and non-Playwright smoke scope in test names/comments where added; remove unused imported Stitch assets.
