# Tasks: W1-H6 Dashboard Inicial

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 650-900 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 API+Application → PR 2 UI host+shell → PR 3 smoke+polish |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Ship dashboard contract and endpoint with tests | PR 1 | Includes unit + integration tests, endpoint telemetry |
| 2 | Add `Aura.UI` host and render shell via HTTP client | PR 2 | Depends on PR 1; no backend assembly runtime access |
| 3 | Add smoke coverage and trim assets/config | PR 3 | Depends on PR 2; proves shell wiring without Playwright |

## Phase 1: Foundation

- [x] 1.1 RED: Add failing `tests/Aura.UnitTests/Dashboard/InitialDashboardReaderTests.cs` for populated and empty results from `IInitialDashboardReader`.
- [x] 1.2 GREEN: Create `src/Aura.Application/Models/InitialDashboardDto.cs`, `Ports/IInitialDashboardReader.cs`, `Services/InitialDashboardReader.cs`, and register it in `DependencyInjection.cs`.
- [x] 1.3 REFACTOR: Extract shared card-building/null-guard logic inside `src/Aura.Application/Services/InitialDashboardReader.cs`; keep framework types out of Application.
- [ ] 1.4 GREEN: Create `src/Aura.UI/Aura.UI.csproj` and `src/Aura.UI/Program.cs`, then add `Aura.UI` to `Aura.sln` with HTTP-only dependencies.

## Phase 2: API Contract Slice

- [x] 2.1 RED: Add failing `tests/Aura.IntegrationTests/Dashboard/InitialDashboardEndpointTests.cs` for `401`, `200` populated, and `200` empty on `GET /api/dashboard/initial`.
- [x] 2.2 GREEN: Create `src/Aura.Api/Endpoints/DashboardEndpoints.cs` and update `src/Aura.Api/Program.cs` to map the endpoint through `IInitialDashboardReader`.
- [x] 2.3 REFACTOR: Add request/error telemetry in `src/Aura.Api/Endpoints/DashboardEndpoints.cs` and `src/Aura.Api/Program.cs` without moving business rules out of Application.

## Phase 3: UI Shell Slice

- [ ] 3.1 RED: Add failing `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs`; update `tests/Aura.E2E/Aura.E2E.csproj` with `Microsoft.AspNetCore.Mvc.Testing` and `Aura.UI` reference.
- [ ] 3.2 GREEN: Create `src/Aura.UI/Components/Layout/MainLayout.razor`, `Sidebar.razor`, `Header.razor`, `Pages/Index.razor`, `Models/InitialDashboardResponse.cs`, and `Services/DashboardApiClient.cs`.
- [ ] 3.3 GREEN: Import minimal Stitch assets into `src/Aura.UI/wwwroot/`, configure API base URL/token forwarding in `src/Aura.UI/Program.cs`, and render loading/empty/error/populated states with stable markers.
- [ ] 3.4 REFACTOR: Split repeated render/state mapping into small UI helpers/components; keep Blazor files presentation-only.

## Phase 4: Verification and Cleanup

- [ ] 4.1 VERIFY: Run `dotnet test Aura.sln` and confirm unit, integration, and smoke coverage satisfy the spec scenarios.
- [ ] 4.2 CLEANUP: Document the HTTP-only boundary and non-Playwright smoke scope in test names/comments where added; remove unused imported Stitch assets.
