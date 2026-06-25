# Design: Real Calendar Ingestion

## Technical Approach

Calendar adapter implements `IConnectorAdapter` (same pattern as Teams/Outlook), reusing `IGraphClientFactory`, `GraphClientFactory`, MSAL cache, `ConnectorExecutionWorker`, `TriggerSyncUseCase`, and `SyncEndpoints`. Calendar events are NOT WorkItems — they flow through a parallel path: `GraphCalendarEventProvider` → `CalendarEventMapper` → `ICalendarEventStore` → `GetUpcomingMeetingsUseCase` → `UpcomingMeetingsPanel.razor`. The `GetSource` mapping in worker/sync adds `"calendar" → "calendar"`.

## Architecture Decisions

| Decision | Choice | Alternatives | Rationale |
|----------|--------|-------------|-----------|
| Calendar as IConnectorAdapter | Implement `IConnectorAdapter` with own store | Separate worker; separate use case only | Reuses worker iteration, sync endpoints, per-source aggregation. Zero new infrastructure. |
| Port: IMessageSourceProvider\<CalendarEventDto\> | Generic port, same as Teams/Outlook | New `ICalendarEventProvider` port | Maximum reuse — same DI pattern, same adapter wiring, same fixture fallback pattern. |
| Store: ICalendarEventStore | New port in Application layer | Store in IWorkItemBuffer | CalendarEvent is NOT a WorkItem; architecture doc compliance. Buffer drains to WorkItemStore only. |
| Fixture fallback | `Func<IReadOnlyList<CalendarEventDto>>` in adapter constructor | No fixture for calendar | Follows Teams/Outlook pattern exactly. Enables controlled-demo mode. |
| Time zone normalization | UTC in adapter, store original TZ in metadata | Store as-is from Graph | Avoids timezone bugs in alert timing. Original TZ preserved for display. |
| Scope config | Add `Calendars.Read` to `GraphConnectorOptions.Scopes[]` in User Secrets | New config section | Scopes array already supports multiple scopes. Config change, not code. |
| DI registration | New `Calendar/DependencyInjection.cs` called from Connectors DI | Inline in existing DI | Follows Graph/Teams/Outlook sub-registration pattern. Isolated, testable. |

## Data Flow

```
SyncNow / Worker
  → TriggerSyncUseCase iterates IConnectorAdapter
    → CalendarConnectorAdapter.ExecuteAsync
      → GraphCalendarEventProvider.FetchAsync (IMessageSourceProvider<CalendarEventDto>)
        → IGraphClientFactory.CreateClientAsync → GraphServiceClient
        → GET /me/calendarView?startDateTime=...&endDateTime=...
        → Map Graph response → CalendarEventDto list
      → CalendarEventMapper.TryMap(dto) → CalendarEvent domain model
      → ICalendarEventStore.SaveAsync(CalendarEvent)
    ← ConnectorExecutionResult (calendar source)
  ← AggregatedSyncResult (teams, outlook, calendar)

Dashboard Load
  → UpcomingMeetingsPanel.OnInitializedAsync
    → GetUpcomingMeetingsUseCase.ExecuteAsync
      → ICalendarEventStore.GetUpcomingAsync(now, now + 8h)
      → Sort by StartUtc ascending
    ← IReadOnlyList<CalendarEvent>
  → Render: title, start/end, duration, join URL
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Domain/Calendar/CalendarEvent.cs` | Create | Domain model: Id, Title, StartUtc, EndUtc, IsOnlineMeeting, JoinUrl?, Organizer?, Location?, OriginalTimeZone? |
| `src/Aura.Application/Ports/ICalendarEventStore.cs` | Create | Port: `SaveAsync(CalendarEvent)`, `GetUpcomingAsync(from, to)`, `GetAllAsync()` |
| `src/Aura.Application/UseCases/Calendar/GetUpcomingMeetingsUseCase.cs` | Create | Reads from ICalendarEventStore, returns sorted upcoming events |
| `src/Aura.Infrastructure/Adapters/Connectors/Calendar/CalendarEventDto.cs` | Create | DTO: ExternalId, Subject, Start, End, IsOnlineMeeting, JoinUrl, OrganizerName, OrganizerAddress, LocationDisplayName, IsCancelled, OriginalTimeZone |
| `src/Aura.Infrastructure/Adapters/Connectors/Calendar/GraphCalendarEventProvider.cs` | Create | `IMessageSourceProvider<CalendarEventDto>` using `/me/calendarView` via IGraphClientFactory |
| `src/Aura.Infrastructure/Adapters/Connectors/Calendar/CalendarEventMapper.cs` | Create | `TryMap(CalendarEventDto, out CalendarEvent)` — UTC normalization, field mapping |
| `src/Aura.Infrastructure/Adapters/Connectors/Calendar/CalendarConnectorAdapter.cs` | Create | `IConnectorAdapter` named `"calendar"` — same fixture/provider pattern as Teams/Outlook |
| `src/Aura.Infrastructure/Adapters/Connectors/Calendar/InMemoryCalendarEventStore.cs` | Create | `ICalendarEventStore` impl for first slice (no SQLite persistence yet) |
| `src/Aura.Infrastructure/Adapters/Connectors/Calendar/DependencyInjection.cs` | Create | Registers calendar adapter, source provider, store, mapper |
| `src/Aura.UI/Components/Dashboard/UpcomingMeetingsPanel.razor` | Create | Dashboard panel: loading/populated/empty/error states, data-testid attributes |
| `src/Aura.Infrastructure/Adapters/Connectors/DependencyInjection.cs` | Modify | Add `services.AddCalendar(configuration)` call; register `ICalendarEventStore` when Graph enabled |
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/DependencyInjection.cs` | Modify | Register `GraphCalendarEventProvider` when Graph enabled |
| `src/Aura.Application/UseCases/IngestionSync/TriggerSyncUseCase.cs` | Modify | Add `"calendar" => "calendar"` to `GetSource` mapping |
| `src/Aura.Workers/ConnectorExecutionWorker.cs` | Modify | Add `"calendar" => "calendar"` to `GetSource` mapping |
| `src/Aura.UI/Pages/Index.razor` | Modify | Add `<UpcomingMeetingsPanel />` after `<InboxPreviewPanel />` |
| `tests/Aura.UnitTests/Ingestion/Calendar/` | Create | GraphCalendarEventProvider tests, CalendarEventMapper tests, CalendarConnectorAdapter tests |
| `tests/Aura.UnitTests/UseCases/Calendar/` | Create | GetUpcomingMeetingsUseCase tests |
| `tests/Aura.ArchitectureTests/GraphConnectorArchitectureTests.cs` | Modify | Verify CalendarEvent domain model not referenced by WorkItem pipeline |

## Interfaces / Contracts

```csharp
// Domain: src/Aura.Domain/Calendar/CalendarEvent.cs
namespace Aura.Domain.Calendar;
public sealed record CalendarEvent(
    string Id,
    string Title,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    bool IsOnlineMeeting,
    string? JoinUrl = null,
    string? Organizer = null,
    string? Location = null,
    string? OriginalTimeZone = null);

// Port: src/Aura.Application/Ports/ICalendarEventStore.cs
namespace Aura.Application.Ports;
public interface ICalendarEventStore
{
    Task SaveAsync(CalendarEvent calendarEvent, CancellationToken ct);
    Task SaveBatchAsync(IReadOnlyList<CalendarEvent> events, CancellationToken ct);
    Task<IReadOnlyList<CalendarEvent>> GetUpcomingAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
}

// DTO: src/Aura.Infrastructure/Adapters/Connectors/Calendar/CalendarEventDto.cs
namespace Aura.Infrastructure.Adapters.Connectors.Calendar;
internal sealed class CalendarEventDto
{
    public string? ExternalId { get; init; }
    public string? Subject { get; init; }
    public DateTimeOffset? Start { get; init; }
    public DateTimeOffset? End { get; init; }
    public bool IsOnlineMeeting { get; init; }
    public string? JoinUrl { get; init; }
    public string? OrganizerName { get; init; }
    public string? OrganizerAddress { get; init; }
    public string? LocationDisplayName { get; init; }
    public bool IsCancelled { get; init; }
    public string? OriginalTimeZone { get; init; }
}

// Source Provider: IMessageSourceProvider<CalendarEventDto>
// (reuses existing generic port from W2-H8)
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | `GraphCalendarEventProvider` | Mock `IGraphClientFactory` + `HttpMessageHandler` stub; verify DTO mapping from Graph response shape |
| Unit | `CalendarEventMapper` | Verify UTC normalization, null field handling, cancelled event filtering |
| Unit | `CalendarConnectorAdapter` | Mock `IMessageSourceProvider<CalendarEventDto>`; verify fixture fallback when provider absent; verify `ICalendarEventStore.SaveAsync` called |
| Unit | `GetUpcomingMeetingsUseCase` | Mock `ICalendarEventStore`; verify sort by StartUtc ascending, time window filtering, empty result |
| Unit | `TriggerSyncUseCase` calendar source | Mock adapter list with calendar adapter; verify per-source aggregation includes calendar |
| Integration | Sync now → calendar events visible | WebApplicationFactory; POST /api/sync/now → GET upcoming meetings returns events |
| Architecture | CalendarEvent isolation | NetArchTest: `Aura.Domain.Calendar` not referenced by `Aura.Application.UseCases.IngestionSync`; CalendarEvent not stored in WorkItemStore |

## Migration / Rollout

No data migration required. Rollout sequence:

1. **Domain + Port** — `CalendarEvent`, `ICalendarEventStore` — no runtime change
2. **Graph provider + adapter** — `GraphCalendarEventProvider`, `CalendarConnectorAdapter`, DI registration — behind `GraphConnector:Enabled` flag
3. **Config** — Add `Calendars.Read` to `GraphConnectorOptions.Scopes[]` in User Secrets
4. **Use case + store** — `GetUpcomingMeetingsUseCase`, `InMemoryCalendarEventStore`
5. **UI** — `UpcomingMeetingsPanel.razor` in `Index.razor`
6. **Worker + sync mappings** — Add `"calendar"` to `GetSource` in worker and sync use case

Rollback: Remove `<UpcomingMeetingsPanel />` from `Index.razor` → remove calendar DI registration → remove `Calendar/` directories.

## Open Questions

- [ ] Whether `InMemoryCalendarEventStore` is sufficient for first slice or SQLite persistence is needed (proposal says no persistent storage in first slice)
- [ ] Exact `startDateTime`/`endDateTime` window for `/me/calendarView` — proposal says today + 8h configurable, but the TriggerSyncUseCase passes `WindowStart`/`WindowEnd` from its own 1-hour window. Calendar adapter may need its own window calculation.
