# Design: W2-H6 Dashboard Inbox-by-Source and Morning Summary Preview

## Technical Approach

Add a dedicated Application port + service (`IDashboardPreviewReader` / `DashboardPreviewReader`) that projects ranked work items into slim DTOs — no `WorkItem` exits Application. `DashboardEndpoints.cs` calls this port and returns `Results.Ok(dto)` directly (same pattern as `IInitialDashboardReader`). `Aura.UI` gets a new typed `IDashboardPreviewApiClient`, two self-loading Blazor panels (loading/empty/error/populated), mounted inline on `Index.razor`. Integration tests cover the API contract via `WebApplicationFactory<ApiMarker>`; UI smoke tests cover panel states via `WebApplicationFactory<UiMarker>`.

## Architecture Decisions

| Decision | Option A (chosen) | Option B (rejected) | Rationale |
|---|---|---|---|
| Composition boundary | New `IDashboardPreviewReader` in Application.Services | Compose inside endpoint handler | Follows `InitialDashboardReader` pattern exactly; keeps endpoint thin |
| Ranking source | Inject `IWorkItemReader` + `IMorningSummaryRankingPolicy` directly | Call `IMorningSummaryComposer` | Avoids window fabrication; same ports already used by `MorningSummaryComposer` |
| DTO boundary | Application DTOs (`DashboardPreviewDto`) returned directly from port; endpoint returns them verbatim | Separate API-layer mapping class | Consistent with how `InitialDashboardDto`, `SystemStatusDto` flow through today |
| Panel integration | Mount directly in `Index.razor` via two new component tags | New page or model composition object | All existing panels (`SystemStatusPanel`, etc.) mount exactly this way |
| API test layer | `Aura.IntegrationTests` (`WebApplicationFactory<ApiMarker>`) | `Aura.E2E` | Mirrors `InitialDashboardEndpointTests` boundary exactly |

## Data Flow

```
DashboardEndpoints.GetDashboardPreviewAsync
  └── IDashboardPreviewReader.GetAsync()
        ├── IWorkItemReader.ReadForWindowAsync(userId, now-24h, now)
        ├── IMorningSummaryRankingPolicy.Rank(items)
        └── Project: RankedWorkItem[] → DashboardPreviewDto
              ├── InboxSourceGroupDto[]  (grouped by WorkItem.Source)
              └── SummaryPreviewEntryDto[] (top N by rank)

Results.Ok(DashboardPreviewDto) ──→ JSON ──→ DashboardPreviewApiClient
  ├── InboxPreviewPanel.razor        (_data / _loadError / OnInitializedAsync)
  └── MorningSummaryPreviewPanel.razor (_data / _loadError / OnInitializedAsync)
```

## File Changes

| File | Action | Description |
|---|---|---|
| `src/Aura.Application/Ports/IDashboardPreviewReader.cs` | Create | Port returning `DashboardPreviewDto` |
| `src/Aura.Application/Models/DashboardPreviewDto.cs` | Create | `DashboardPreviewDto`, `InboxSourceGroupDto`, `InboxItemPreviewDto`, `SummaryPreviewEntryDto` |
| `src/Aura.Application/Services/DashboardPreviewReader.cs` | Create | Injects `IWorkItemReader` + `IMorningSummaryRankingPolicy`; projects slim DTOs; groups by source |
| `src/Aura.Application/DependencyInjection.cs` | Modify | Register `IDashboardPreviewReader → DashboardPreviewReader` as scoped |
| `src/Aura.Api/Endpoints/DashboardEndpoints.cs` | Modify | Add `group.MapGet("/preview", GetDashboardPreviewAsync)`; handler + ActivitySource tags + `Log` partial entries |
| `src/Aura.UI/Models/DashboardPreviewResponse.cs` | Create | UI-side mirrors: `DashboardPreviewResponse`, `InboxSourceGroupResponse`, `InboxItemPreviewResponse`, `SummaryPreviewEntryResponse` |
| `src/Aura.UI/Services/IDashboardPreviewApiClient.cs` | Create | `Task<DashboardPreviewResponse> GetPreviewAsync(CancellationToken)` |
| `src/Aura.UI/Services/DashboardPreviewApiClient.cs` | Create | Typed `HttpClient`; `GET /api/dashboard/preview`; follows `DashboardApiClient` pattern |
| `src/Aura.UI/Program.cs` | Modify | Register `IDashboardPreviewApiClient / DashboardPreviewApiClient` with `DevAccessTokenHandler` in dev |
| `src/Aura.UI/Components/Dashboard/InboxPreviewPanel.razor` | Create | Injects `IDashboardPreviewApiClient`; loading/empty/error/populated; `data-testid` on all states |
| `src/Aura.UI/Components/Dashboard/MorningSummaryPreviewPanel.razor` | Create | Same pattern; renders `SummaryEntries` |
| `src/Aura.UI/Pages/Index.razor` | Modify | Add `<InboxPreviewPanel />` and `<MorningSummaryPreviewPanel />` after existing panels |
| `tests/Aura.IntegrationTests/Dashboard/DashboardPreviewEndpointTests.cs` | Create | `WebApplicationFactory<ApiMarker>`; stubs `IDashboardPreviewReader`; verifies HTTP 200, auth 401, JSON shape, 500 on throw |
| `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` | Modify | Add stub `IDashboardPreviewApiClient`; add tests for inbox/summary panel loading, empty, error, populated HTML markers |
| `tests/Aura.ArchitectureTests/DashboardArchitectureTests.cs` | Modify | Add assertion: `Aura.UI.Models` and endpoint types must not reference `Aura.Domain` |

## Interfaces / Contracts

```csharp
// Application.Ports
public interface IDashboardPreviewReader
{
    Task<DashboardPreviewDto> GetAsync(CancellationToken cancellationToken);
}

// Application.Models
public sealed record DashboardPreviewDto(
    IReadOnlyList<InboxSourceGroupDto> InboxGroups,
    IReadOnlyList<SummaryPreviewEntryDto> SummaryEntries);

public sealed record InboxSourceGroupDto(string Source, IReadOnlyList<InboxItemPreviewDto> Items);

public sealed record InboxItemPreviewDto(
    string Title, string Source, string RelativeTimestamp, double Score, string SuggestedAction);

public sealed record SummaryPreviewEntryDto(int Rank, string Title, string Source, double Score);

// UI.Services
public interface IDashboardPreviewApiClient
{
    Task<DashboardPreviewResponse> GetPreviewAsync(CancellationToken cancellationToken);
}
```

`DashboardPreviewReader` builds `MorningSummaryQuery(userId, utcNow.AddHours(-24), utcNow)` using `ICurrentUserService`. Groups `RankedWorkItem` by `Item.Source` for `InboxGroups`; passes all entries in rank order for `SummaryEntries`. `RelativeTimestamp` formatted as a human-readable string (e.g., `"2h ago"`) inside the reader.

## Testing Strategy

| Layer | What to Test | Approach |
|---|---|---|
| Integration (`Aura.IntegrationTests`) | `GET /api/dashboard/preview` HTTP contract; auth 401; 200 with slim DTO shape; 500 on reader throw | `WebApplicationFactory<ApiMarker>` + stub `IDashboardPreviewReader` (mirrors `InitialDashboardEndpointTests`) |
| E2E smoke (`Aura.E2E`) | Panel HTML states: loading marker, empty marker, error message, populated items | `WebApplicationFactory<UiMarker>` + stub `IDashboardPreviewApiClient`; assert `data-testid` markers in HTML |
| Architecture (`Aura.ArchitectureTests`) | `Aura.UI.Models` + API endpoint types have no `Aura.Domain` reference | NetArchTest `.ShouldNot().HaveDependencyOn("Aura.Domain")` |

## Migration / Rollout

No migration required. No schema, persistence, or feature-flag changes. Rollback: revert endpoint registration, DTOs, panels, and `Index.razor` mounts in one change.

## Open Questions

- None.
