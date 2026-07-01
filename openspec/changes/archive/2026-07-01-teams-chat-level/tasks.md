# Tasks: Chat-Level Teams WorkItems

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~600–700 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 → PR 2 → PR 3 |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Domain + Contracts + DTO | PR 1 | base=main; SourceType, MarkAutoCompleted, interfaces, TeamsMessageDto fields, InboxItemPreviewDto.UnreadCount |
| 2 | Store implementations | PR 2 | base=main; InMemory (ExternalId key, dedup), Sqlite (FindByExternalId, status filter, ON CONFLICT) |
| 3 | Mapping + Adapter + Dashboard | PR 3 | base=main; Mapper chat routing, Graph field mapping, auto-dismiss, Pending filter, integration + arch tests |

## Phase 1: Domain Contracts (TDD)

- [x] 1.1 RED: `WorkItemTests.cs` — 4 scenarios for `MarkAutoCompleted()` (Pending→ok, Processing/Faulted/Completed→throws)
- [x] 1.2 RED: assert `WorkItemSourceType.TeamsChat` value equals 14
- [x] 1.3 GREEN: add `MarkAutoCompleted()` to `src/Aura.Domain/WorkItems/WorkItem.cs`
- [x] 1.4 GREEN: add `TeamsChat = 14` to `src/Aura.Domain/WorkItems/WorkItemSourceType.cs`

## Phase 2: Persistence Contracts (TDD)

- [x] 2.1 RED: test `FindByExternalIdAsync` on `IWorkItemStore` (found + null cases)
- [x] 2.2 RED: test `IWorkItemReader` status-filter overload (filter + empty cases)
- [x] 2.3 GREEN: add `FindByExternalIdAsync` to `src/Aura.Application/Ports/IWorkItemStore.cs`
- [x] 2.4 GREEN: add `Task<IReadOnlyList<WorkItem>> ReadForWindowAsync(MorningSummaryQuery, WorkItemStatus?, CancellationToken)` to `src/Aura.Application/Ports/IWorkItemReader.cs`

## Phase 3: DTO Changes

- [x] 3.1 RED: test `TeamsMessageDto` — `LastMessageReadAt`, `LastMessageAt`, `UnreadCount` init-only properties
- [x] 3.2 RED: test `InboxItemPreviewDto.UnreadCount` init-only property
- [x] 3.3 GREEN: add 3 fields to `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsMessageDto.cs`
- [x] 3.4 GREEN: add `public int UnreadCount { get; init; }` to `src/Aura.Application/Models/DashboardPreviewDto.cs` (InboxItemPreviewDto)

## Phase 4: Graph Provider Mapping (TDD)

- [x] 4.1 RED: `GraphTeamsSourceProviderTests.cs` — 4 scenarios: `lastMessageReadDateTime` mapped, null mapped to null, `lastMessageDateTime` mapped, `unreadCount` mapped
- [x] 4.2 GREEN: map `chat.LastMessageReadDateTime` → `LastMessageReadAt`, `chat.LastMessageDateTime` → `LastMessageAt`, `chat.UnreadCount` → `UnreadCount` in `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphTeamsSourceProvider.cs`

## Phase 5: Mapper Chat Routing (TDD)

- [x] 5.1 RED: `TeamsWorkItemMapperTests.cs` — when `Source=="chats"`, assert `SourceType==TeamsChat`, Metadata contains `lastMessageAt`/`lastMessageReadAt`/`unreadCount`
- [x] 5.2 GREEN: update `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsWorkItemMapper.cs` to map chat DTO fields and use `TeamsChat` source type

## Phase 6: InMemory Store Implementation (TDD)

- [x] 6.1 RED: `InMemoryWorkItemStoreTests.cs` — dedup by ExternalId (first save, re-save retains Priority, different ExternalId separate)
- [x] 6.2 RED: `FindByExternalIdAsync` (found + null) for InMemory
- [x] 6.3 GREEN: switch `src/Aura.Infrastructure/Adapters/WorkItems/InMemoryWorkItemStore.cs` to `ConcurrentDictionary<string, WorkItem>` keyed by ExternalId; implement dedup with stable Priority; implement `FindByExternalIdAsync`

## Phase 7: Sqlite Store Implementation (TDD)

- [x] 7.1 RED: `SqliteWorkItemStoreTests.cs` — `FindByExternalIdAsync` (found + null round-trip)
- [x] 7.2 RED: status filter returns only Pending, empty when no match
- [x] 7.3 GREEN: add `FindByExternalIdAsync` + status-filter WHERE clause to `src/Aura.Infrastructure/Adapters/WorkItems/SqliteWorkItemStore.cs`
- [x] 7.4 GREEN: update upsert to `ON CONFLICT(ExternalId) DO UPDATE SET` excluding Priority from SET clause

## Phase 8: Connector Adapter Auto-Dismiss (TDD)

- [x] 8.1 RED: `TeamsConnectorAdapterTests.cs` — read chat (`>=`) calls `MarkAutoCompleted()`, null `LastMessageReadAt` treated as unread, partially read stays Pending
- [x] 8.2 RED: test chat-level fixtures use `Source="chats"` and ChatId ExternalId pattern
- [x] 8.3 GREEN: implement auto-dismiss gate in `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsConnectorAdapter.cs`; update fixture data to chat-level

## Phase 9: Dashboard Filter (TDD)

- [x] 9.1 RED: `DashboardPreviewReaderTests.cs` — status filter returns only Pending items; `UnreadCount` projected from Metadata; absent metadata defaults to 0
- [x] 9.2 GREEN: pass `WorkItemStatus.Pending` filter to `ReadForWindowAsync` in `src/Aura.Application/Services/DashboardPreviewReader.cs`; project `UnreadCount` from Metadata (null when absent)

## Phase 10: Integration + Architecture Tests

- [x] 10.1 RED: integration test — Sqlite round-trip with chat WorkItem (save, FindByExternalId, ReadForWindow with status filter)
- [x] 10.2 RED: architecture test — no Teams/Graph types leak into Application or Domain layers
- [x] 10.3 GREEN: verify all tests pass (`dotnet test Aura.sln`)
