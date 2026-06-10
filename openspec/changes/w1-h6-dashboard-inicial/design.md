# Design: W1-H6 Dashboard Inicial

## Technical Approach

Create a separate `src/Aura.UI/` Blazor Server host and keep all business/data access behind `Aura.Api`. `Aura.Api` will add one dashboard endpoint that composes an initial response from Application services and current-user context. `Aura.UI` will render the Stitch-derived shell first, then call the API through a typed `HttpClient` and map the response into loading, empty, error, and populated states.

## Architecture Decisions

| Decision | Options | Choice | Rationale |
|---|---|---|---|
| UI host boundary | Host UI inside `Aura.Api` / separate `Aura.UI` | Separate `Aura.UI` | Matches `docs/ai/04-ui-incremental-strategy.md` and prevents component-level access to Application/Infrastructure runtime services. |
| Dashboard data contract | Reuse `/api/auth/me` only / add dedicated dashboard endpoint | Add `GET /api/dashboard/initial` | Keeps UI state driven by one API contract instead of spreading composition into the UI. |
| UI contract sharing | Project-reference API/Application DTOs / local UI transport models | Local UI transport models + HTTP client | Preserves the host boundary; `Aura.UI` depends on HTTP JSON, not backend assemblies. |
| Smoke verification | Claim Playwright / use host-level HTTP smoke | Host-level smoke in `tests/Aura.E2E` | Repository has xUnit only; `WebApplicationFactory` can prove the shell is wired without pretending browser tooling exists. |

## Data Flow

`Browser -> Aura.UI (Blazor Server page/component) -> typed HttpClient -> Aura.Api /api/dashboard/initial -> Application service -> ICurrentUserService`

The page renders shell markup immediately. After first render, a UI state container requests dashboard data. API returns a small DTO with user greeting and initial cards. UI maps:
- request pending -> loading
- 200 with empty cards -> empty
- non-success/exception -> error
- 200 with cards -> populated

## File Changes

| File | Action | Description |
|---|---|---|
| `Aura.sln` | Modify | Add `Aura.UI` under `src` and keep test projects wired. |
| `src/Aura.UI/Aura.UI.csproj` | Create | New Blazor Server host; no references to Domain/Application/Infrastructure. |
| `src/Aura.UI/Program.cs` | Create | Register Razor components, typed dashboard API client, auth token forwarding, and `Program` marker for tests. |
| `src/Aura.UI/Components/Layout/MainLayout.razor` | Create | Stitch-derived dashboard shell. |
| `src/Aura.UI/Components/Layout/Sidebar.razor` | Create | Sidebar navigation fragment. |
| `src/Aura.UI/Components/Layout/Header.razor` | Create | Header fragment with user summary. |
| `src/Aura.UI/Pages/Index.razor` | Create | Default dashboard route with loading/empty/error/populated states. |
| `src/Aura.UI/Services/DashboardApiClient.cs` | Create | HTTP-only client for `Aura.Api`. |
| `src/Aura.UI/Models/InitialDashboardResponse.cs` | Create | UI transport contract matching API JSON. |
| `src/Aura.UI/wwwroot/...` | Create | Imported Stitch CSS/fonts/icons trimmed to required assets. |
| `src/Aura.Api/Program.cs` | Modify | Register Application services and map dashboard endpoints. |
| `src/Aura.Api/Endpoints/DashboardEndpoints.cs` | Create | `GET /api/dashboard/initial` endpoint. |
| `src/Aura.Application/Models/InitialDashboardDto.cs` | Create | API response DTO. |
| `src/Aura.Application/Ports/IInitialDashboardReader.cs` | Create | Capability-named port for composing initial dashboard data. |
| `src/Aura.Application/Services/InitialDashboardReader.cs` | Create | Use-case service using `ICurrentUserService`. |
| `src/Aura.Application/DependencyInjection.cs` | Modify | Register `IInitialDashboardReader`. |
| `tests/Aura.IntegrationTests/Dashboard/InitialDashboardEndpointTests.cs` | Create | API contract/state tests with `WebApplicationFactory<ApiMarker>`. |
| `tests/Aura.E2E/Aura.E2E.csproj` | Modify | Add `Microsoft.AspNetCore.Mvc.Testing` and `Aura.UI` reference. |
| `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` | Create | GET `/` from `Aura.UI` and assert shell/state markers. |

## Interfaces / Contracts

```csharp
public sealed record InitialDashboardDto(string UserDisplayName, IReadOnlyList<DashboardCardDto> Cards);
public sealed record DashboardCardDto(string Title, string Value, string Status);
public interface IInitialDashboardReader { Task<InitialDashboardDto> GetAsync(CancellationToken ct); }
```

API route: `GET /api/dashboard/initial` -> `200 OK` with `InitialDashboardDto`, `401` when auth is required and missing, `5xx` for unexpected failures.

## Testing Strategy

| Layer | What to Test | Approach |
|---|---|---|
| Unit | `InitialDashboardReader` composition and empty/populated mapping | xUnit in `tests/Aura.UnitTests`, stub `ICurrentUserService`. |
| Integration | `/api/dashboard/initial` auth + response contract | `WebApplicationFactory<ApiMarker>` following existing `AuthorizationFlowTests` pattern. |
| E2E | UI host scaffold only | `WebApplicationFactory` against `Aura.UI`, assert shell HTML and state markers; no Playwright claim yet. |

## Migration / Rollout

No migration required. Roll out as a thin vertical slice: add `Aura.UI`, ship shell plus one API-backed payload, and keep richer widgets for later changes.

## Open Questions

- [ ] Confirm the final Stitch export path/file names so `wwwroot` imports stay minimal and reproducible.
