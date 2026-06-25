# Tasks: Real Calendar Ingestion

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 800–1,100 |
| 400-line budget risk | Medium |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | ask-on-risk |
| Chain strategy | size-exception |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: size-exception
400-line budget risk: Medium

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Domain + Ports + Infrastructure + Use Case + UI + Tests | PR 1 | Single PR; all phases self-contained; under 2,000-line budget |

## Phase 1: Domain + Ports (Foundation)

- [x] 1.1 Create `src/Aura.Domain/Calendar/CalendarEvent.cs` — sealed record with Id, Title, StartUtc, EndUtc, IsOnlineMeeting, JoinUrl?, Organizer?, Location?, OriginalTimeZone?
- [x] 1.2 Create `src/Aura.Application/Ports/ICalendarEventStore.cs` — SaveAsync, SaveBatchAsync, GetUpcomingAsync(from, to, ct)
- [x] 1.3 RED: Write tests for `CalendarEventMapper` in `tests/Aura.UnitTests/Ingestion/Calendar/CalendarEventMapperTests.cs` — UTC normalization, null fields, cancelled filtering
- [x] 1.4 GREEN: Create `src/Aura.Infrastructure/Adapters/Connectors/Calendar/CalendarEventMapper.cs` — TryMap(CalendarEventDto, out CalendarEvent)
- [x] 1.5 Create `src/Aura.Infrastructure/Adapters/Connectors/Calendar/CalendarEventDto.cs` — DTO with ExternalId, Subject, Start, End, IsOnlineMeeting, JoinUrl, OrganizerName, OrganizerAddress, LocationDisplayName, IsCancelled, OriginalTimeZone

## Phase 2: Graph Provider + Adapter (Core)

- [x] 2.1 RED: Write tests for `GraphCalendarEventProvider` in `tests/Aura.UnitTests/Ingestion/Calendar/GraphCalendarEventProviderTests.cs` — mock IGraphClientFactory + HttpMessageHandler, verify DTO mapping from `/me/calendarView` response shape
- [x] 2.2 GREEN: Create `src/Aura.Infrastructure/Adapters/Connectors/Calendar/GraphCalendarEventProvider.cs` — `IMessageSourceProvider<CalendarEventDto>` using IGraphClientFactory
- [x] 2.3 RED: Write tests for `CalendarConnectorAdapter` in `tests/Aura.UnitTests/Ingestion/Calendar/CalendarConnectorAdapterTests.cs` — fixture fallback, provider delegation, ICalendarEventStore.SaveAsync called
- [x] 2.4 GREEN: Create `src/Aura.Infrastructure/Adapters/Connectors/Calendar/CalendarConnectorAdapter.cs` — IConnectorAdapter named "calendar", fixture fallback pattern
- [x] 2.5 Create `src/Aura.Infrastructure/Adapters/Connectors/Calendar/InMemoryCalendarEventStore.cs` — ICalendarEventStore impl (in-memory, no SQLite)
- [x] 2.6 Create `src/Aura.Infrastructure/Adapters/Connectors/Calendar/DependencyInjection.cs` — register adapter, source provider, store, mapper behind GraphConnector:Enabled

## Phase 3: Use Case + UI (Integration)

- [x] 3.1 RED: Write tests for `GetUpcomingMeetingsUseCase` in `tests/Aura.UnitTests/UseCases/Calendar/GetUpcomingMeetingsUseCaseTests.cs` — sort by StartUtc ascending, time window filtering, empty result
- [x] 3.2 GREEN: Create `src/Aura.Application/UseCases/Calendar/GetUpcomingMeetingsUseCase.cs` — reads from ICalendarEventStore, returns sorted upcoming events
- [x] 3.3 Create `src/Aura.UI/Components/Dashboard/UpcomingMeetingsPanel.razor` — loading/populated/empty/error states, data-testid attributes, presentation-only
- [x] 3.4 Modify `src/Aura.Infrastructure/Adapters/Connectors/DependencyInjection.cs` — add `services.AddCalendar(configuration)` call
- [x] 3.5 Modify `src/Aura.Application/UseCases/IngestionSync/TriggerSyncUseCase.cs` — add `"calendar" => "calendar"` to GetSource mapping
- [x] 3.6 Modify `src/Aura.Workers/ConnectorExecutionWorker.cs` — add `"calendar" => "calendar"` to GetSource mapping
- [x] 3.7 Modify `src/Aura.UI/Pages/Index.razor` — add `<UpcomingMeetingsPanel />` after `<InboxPreviewPanel />`

## Phase 4: Architecture Tests + Cleanup

- [x] 4.1 Modify `tests/Aura.ArchitectureTests/GraphConnectorArchitectureTests.cs` — verify Aura.Domain.Calendar not referenced by Aura.Application.UseCases.IngestionSync; CalendarEvent not stored in WorkItemStore
- [x] 4.2 Run `dotnet test Aura.sln` — verify all unit + integration + architecture tests pass
- [x] 4.3 Add `Calendars.Read` to `GraphConnectorOptions.Scopes[]` in User Secrets (config, not code — manual step for deployer)
