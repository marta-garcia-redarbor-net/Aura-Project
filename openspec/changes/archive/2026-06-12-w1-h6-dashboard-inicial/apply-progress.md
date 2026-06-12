# Apply Progress: W1-H6 Dashboard Inicial

**Change**: w1-h6-dashboard-inicial
**Mode**: Strict TDD
**Status**: 22/22 tasks complete (all phases including both verify fix batches)
**Workload Mode**: stacked PR fix slice
**Current Work Unit**: PR 5 / Verify warning fix batch (coverage + header + count)
**Boundary**: Add integration tests for endpoint error/cancellation paths to improve `DashboardEndpoints.cs` coverage, add E2E test + layout-owned implementation for header user summary to match design note, and fix stale summary count. No new features.

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
- [x] 4.1 VERIFY: Run `dotnet test Aura.sln` — full suite 241/241 passing (Unit 193, Integration 27, E2E 6, Architecture 15). All spec scenarios satisfied.
- [x] 4.2 CLEANUP: Added XML doc comments documenting HTTP-only boundary and non-Playwright smoke scope on `InitialDashboardSmokeTests` and `InitialDashboardEndpointTests`; removed unused `src/Aura.UI/wwwroot/stitch-export/` directory (index.html + screenshot.png — design tokens already extracted to `stitch-dashboard.css`).
- [x] 5.1 Unit tests for `ForwardedAccessTokenHandler` — 4 tests covering bearer forwarding, missing header, empty header, null context.
- [x] 5.2 Unit tests for `DashboardApiClient` — 6 tests covering successful deserialization, empty cards, 500 error, 401 error, null JSON body, and request path verification.
- [x] 5.3 E2E runtime path test — proves real `DashboardApiClient` executes via stubbed HTTP primary handler; populated state rendered from real client code.
- [x] 5.4 E2E loading→populated transition test — single delayed request proves Blazor streaming renders loading marker first, then populated content in the same HTTP response.
- [x] 6.1 Integration test for `DashboardEndpoints.cs` exception path — `IInitialDashboardReader` throws, endpoint returns 500 Problem.
- [x] 6.2 Integration test for `DashboardEndpoints.cs` cancellation path — a cancelled HTTP request propagates its request token into `IInitialDashboardReader`, and the current host behavior for an already-cancelled reader task remains documented separately.
- [x] 6.3 E2E test for `Header.razor` user summary — populated dashboard renders `data-testid="dashboard-header-user"` with user display name.
- [x] 6.4 Adjust `MainLayout.razor` to own the dashboard load, pass user summary into `Header.razor`, and avoid duplicate header fetches.
- [x] 6.5 Fix stale `apply-progress.md` summary count and reconcile with tasks.md.

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
| PR2-R1 (Stitch alignment) | `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` | E2E smoke via `WebApplicationFactory` | ✅ 4/4 `InitialDashboardSmokeTests` passing before changes | ✅ Added `GetRootRendersStitchAlignedDarkThemeShell` (failed: `class="dark"` missing, nav items missing) and extended `GetRootWithDashboardCardsRendersPopulatedState` with `data-testid="dashboard-card-status"` (failed: marker missing) | ✅ 5/5 `InitialDashboardSmokeTests` passing after CSS rewrite, App.razor dark class + font links, sidebar/header/cards structure alignment | ✅ New test covers 5 distinct assertions (dark class, Google Fonts link, Dashboard nav, Health nav, Aura Core brand); card test triangulates status marker with 2 cards | ✅ CSS rewritten with extracted tokens from Stitch Tailwind config and the remaining header inline style was later replaced with a semantic CSS class; layout uses semantic BEM classes |

### PR 3 verify + cleanup batch

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 4.1 (VERIFY) | Full suite | All | ✅ 241/241 passing (baseline from prior batches) | ➖ N/A — verification task, not new production code | ✅ `dotnet test Aura.sln` 241/241 all passing: Unit 193, Integration 27, E2E 6, Architecture 15 | ➖ N/A — verification confirms existing triangulation coverage | ➖ N/A — no code changes in verify step |
| 4.2 (CLEANUP) | Full suite post-cleanup | All | ✅ 241/241 passing before cleanup changes | ➖ N/A — structural task (doc comments + asset removal, no logic) | ✅ 241/241 still passing after adding XML doc comments and removing `stitch-export/` directory | ➖ Triangulation skipped: purely structural (documentation + file deletion, zero logic) | ✅ Removed unused Stitch export assets; added XML doc comments for boundary documentation |

### PR 4 verify fix batch (coverage gaps)

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 5.1 | `tests/Aura.UnitTests/Dashboard/ForwardedAccessTokenHandlerTests.cs` | Unit | ✅ 10/10 dashboard tests passing | ✅ Written; tests reference existing `ForwardedAccessTokenHandler` production code for untested paths | ✅ 4/4 passing — bearer forwarding, missing header, empty header, null context all verified | ✅ 4 cases cover all code branches: bearer present, header absent, empty string, null `HttpContext` | ➖ None needed — production code unchanged |
| 5.2 | `tests/Aura.UnitTests/Dashboard/DashboardApiClientTests.cs` | Unit | ✅ 10/10 dashboard tests passing | ✅ Written; tests exercise `DashboardApiClient` via stub `HttpMessageHandler` | ✅ 6/6 passing — deserialization, empty cards, 500, 401, null body, request path all verified | ✅ 6 cases covering happy path (populated + empty), error codes (500, 401), edge case (null JSON), and path assertion | ➖ None needed — production code unchanged |
| 5.3 | `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` | E2E smoke via `WebApplicationFactory` | ✅ 241/241 full suite passing | ✅ Written; `GetRootWithRealDashboardApiClientRendersPopulatedStateFromApiResponse` keeps real `DashboardApiClient` registered, only swaps primary handler | ✅ 7/7 E2E passing — real client code deserializes canned JSON and renders populated state | ✅ Asserts specific user name ("Runtime Path User"), card title, and card value from the stub API response — not a smoke test | ➖ None needed — production code unchanged |
| 5.4 | `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` | E2E smoke via `WebApplicationFactory` | ✅ 241/241 full suite passing | ✅ Written; `GetRootWithDelayedResponseShowsLoadingThenPopulatedInSameFlow` uses delayed stub to prove streaming transition | ✅ 8/8 E2E passing — same HTTP response body contains both `dashboard-state-loading` and `dashboard-state-populated` markers plus populated content | ✅ Asserts loading marker AND populated marker AND specific user/card content in the same response — proves transition, not just presence | ➖ None needed — production code unchanged |

### PR 5 verify warning fix batch (coverage + header + count)

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 6.1 | `tests/Aura.IntegrationTests/Dashboard/InitialDashboardEndpointTests.cs` | Integration | ✅ 10/10 dashboard tests passing (7 E2E + 3 integration) | ✅ Written; tests reference existing endpoint error/cancellation paths (L50-60) not previously covered | ✅ 6/6 integration passing — `ThrowingInitialDashboardReader` triggers 500, request cancellation propagates the HTTP request token into the reader, and an already-cancelled reader task remains documented through the current WebApplicationFactory/TestServer response | ✅ 3 cases: general exception → 500 Problem, request-token cancellation propagation → reader observes cancelled token, already-cancelled reader task → current test-host response observation | ➖ None needed — production code unchanged |
| 6.2 | `tests/Aura.IntegrationTests/Dashboard/InitialDashboardEndpointTests.cs` | Integration | ✅ (same safety net as 6.1) | ✅ (same RED as 6.1 — tests written together for the endpoint cancellation slice) | ✅ 6/6 integration passing — cancellation proof now distinguishes real request-token propagation from the separate already-cancelled task observation | ✅ Real request cancellation is proven at the reader boundary; host behavior for a pre-cancelled reader task is documented separately | ➖ None needed |
| 6.3 | `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` | E2E smoke via `WebApplicationFactory` | ✅ 253/253 full suite passing | ✅ Written; `GetRootWithPopulatedDashboardRendersUserSummaryInHeader` asserts `data-testid="dashboard-header-user"` — failed with "Sub-string not found" | ✅ 9/9 E2E passing after adding `[StreamRendering]`, layout-owned dashboard state, and the header user summary element | ✅ Asserts specific `data-testid` marker AND specific user name content ("Header User") from the stub response | ✅ Added CSS class `dashboard-header__user` with Stitch-aligned styling |
| 6.4 | (implementation task, tested by 6.3) | — | ✅ 253/253 full suite passing before layout/header adjustment | ➖ N/A — implementation task driven by 6.3 RED test | ✅ All 256/256 tests passing after `MainLayout.razor` owns the dashboard load and `Header.razor` consumes the shared user summary | ➖ N/A — implementation task | ✅ Clean separation: header consumes shared layout state; source inspection shows `MainLayout.razor` persists prerendered state, but no dedicated test proves interactive-handoff duplicate-fetch avoidance |
| 6.5 | (artifact fix — no test needed) | — | ✅ N/A | ➖ N/A — artifact/count reconciliation | ✅ Summary count updated from 16/16 to 22/22 | ➖ Triangulation skipped: structural artifact fix, zero logic | ➖ N/A |

## Test Summary

- **Total tests written**: 26 (10 prior batches + 12 verify fix batch + 4 warning fix batch)
- **Total tests passing**: 257 full suite (`UnitTests` 203 + `IntegrationTests` 30 + `E2E` 9 + `ArchitectureTests` 15)
- **Layers used**: Unit (203), Integration (30), E2E (9), Architecture (15)
- **Approval tests** (refactoring): Safety net only — existing smoke tests served as approval baseline before refactoring UI markup
- **Pure functions created**: 4 (prior batches) + 0 (fix batches — tests and minimal UI adjustment only)

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

### PR 2 Stitch refinement batch

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

### PR 3 verify + cleanup batch

| File | Action | What Was Done |
|------|--------|---------------|
| `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` | Modified | Added XML doc summary documenting HTTP-only smoke scope and non-Playwright boundary |
| `tests/Aura.IntegrationTests/Dashboard/InitialDashboardEndpointTests.cs` | Modified | Added XML doc summary documenting HTTP contract scope and API-only boundary |
| `src/Aura.UI/wwwroot/stitch-export/dashboard-operativa/index.html` | Deleted | Reference-only Stitch export HTML; design tokens already extracted to `stitch-dashboard.css` |
| `src/Aura.UI/wwwroot/stitch-export/dashboard-operativa/screenshot.png` | Deleted | Reference-only Stitch export screenshot; no longer needed |
| `openspec/changes/archive/2026-06-12-w1-h6-dashboard-inicial/tasks.md` | Modified | Marked tasks 4.1 and 4.2 as complete |

### PR 4 verify fix batch (coverage gaps)

| File | Action | What Was Done |
|------|--------|---------------|
| `tests/Aura.UnitTests/Dashboard/ForwardedAccessTokenHandlerTests.cs` | Created | 4 unit tests for bearer forwarding, missing/empty header, null context |
| `tests/Aura.UnitTests/Dashboard/DashboardApiClientTests.cs` | Created | 6 unit tests for HTTP deserialization, error codes, null body, path verification |
| `tests/Aura.UnitTests/Aura.UnitTests.csproj` | Modified | Added `Aura.UI` project reference for testing UI services |
| `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` | Modified | Added real runtime path test and loading→populated transition test + `StubApiPrimaryHandler` |
| `openspec/changes/archive/2026-06-12-w1-h6-dashboard-inicial/tasks.md` | Modified | Added Phase 5 with fix batch tasks 5.1-5.4, all marked complete |

### PR 5 verify warning fix batch (coverage + header + count)

| File | Action | What Was Done |
|------|--------|---------------|
| `tests/Aura.IntegrationTests/Dashboard/InitialDashboardEndpointTests.cs` | Modified | Added endpoint exception coverage, proof that HTTP request cancellation propagates the request token into `IInitialDashboardReader`, and a separate observation of the current TestServer response to an already-cancelled reader task |
| `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` | Modified | Added `GetRootWithPopulatedDashboardRendersUserSummaryInHeader` E2E test for header user summary |
| `src/Aura.UI/Components/Layout/MainLayout.razor` | Modified | Owns the shared dashboard load, persists prerendered dashboard state in code, and passes user summary into `Header.razor` |
| `src/Aura.UI/Components/Layout/Header.razor` | Modified | Switched to a passed user summary parameter so the header reuses the layout-owned dashboard payload without a second API call |
| `src/Aura.UI/wwwroot/css/stitch-dashboard.css` | Modified | Added `.dashboard-header__user` CSS class with Stitch-aligned styling |
| `openspec/changes/archive/2026-06-12-w1-h6-dashboard-inicial/tasks.md` | Modified | Added Phase 6 with warning fix batch tasks 6.1-6.5, all marked complete |
| `openspec/changes/archive/2026-06-12-w1-h6-dashboard-inicial/apply-progress.md` | Modified | Fixed stale summary count (16→22), added PR 5 batch evidence and files |

## Deviations from Design

- **Header parameters removed**: The design described `Header Title="..." Subtitle="..."` parameters. These were replaced with hardcoded Stitch-aligned content (brand "Aura", dev nav) because the real Stitch export uses a fixed application header, not a per-page parameterized one. The page-level title/subtitle moved to `Index.razor` as a `dashboard-page-header` section, matching the Stitch export structure. **Fix batch update**: `MainLayout.razor` now loads the dashboard payload once, persists that state in code, and passes `UserDisplayName` into `Header.razor`; the header user summary alignment is runtime-covered, but interactive-handoff duplicate-fetch avoidance was source-inspected rather than directly test-proven.
- **Google Fonts CDN dependency**: The Stitch export uses Inter, JetBrains Mono, and Material Symbols Outlined via Google Fonts CDN. This is kept as-is for pragmatic parity with the export. For production, these should be self-hosted. Documented as a known trade-off.
- **No Tailwind CDN**: The Stitch export uses a Tailwind CDN script. We extracted the design tokens into CSS custom properties and wrote semantic CSS classes instead. This avoids a runtime Tailwind dependency.

## Issues Found

- **Prior batches**: A transient Windows file-lock on `Aura.Api.dll`/`Aura.Domain.dll` interrupted two PR 1 test runs; rerunning the targeted commands succeeded without code changes.
- **Prior batches**: The repository lacked a committed Stitch export — now resolved with the real export download.
- **Build cache issue**: A transient `MSB3492` file-lock error on `Aura.Workers.AssemblyInfoInputs.cache` required a `dotnet clean` before rebuild; no code change needed.
- **PR 3**: No new issues found. All 241 tests pass cleanly after cleanup.
- **PR 4 fix batch**: No issues. All 253 tests pass. No production code changes were needed — tests confirmed existing code works correctly.
- **Fix batch**: The header user summary now reuses the layout-loaded dashboard state, and source inspection shows the prerendered state is persisted in `MainLayout.razor`; no dedicated test directly proves interactive-handoff duplicate-fetch avoidance.
- **Final surgical fix batch**: `MainLayout.razor` now emits local UI logging/activity telemetry when the initial dashboard load is cancelled or fails, and the remaining header inline style was replaced with a semantic CSS class.

## Remaining Tasks

None — all 22 tasks complete (12 original + 5 PR 4 fix batch + 5 PR 5 warning fix batch). Archived record updated to match the final fix state.
