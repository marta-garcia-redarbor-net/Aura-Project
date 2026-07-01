# Design: Chat-Level Teams WorkItems

## Technical Approach

Extend the existing message-level pipeline to model 1 chat → 1 WorkItem (keyed by `ChatId` as `ExternalId`). Add `MarkAutoCompleted()` for origin-side dismissal when chat is fully read. Persistence stores dedup by `ExternalId` with stable priority (preserve original). Dashboard filters to `Pending` only. All changes are additive — no existing contracts break.

## Architecture Decisions

| Decision | Choice | Alternatives | Rationale |
|---|---|---|---|
| Stable priority on re-sync | Store retains original Priority | Recompute every sync | Chat priority shouldn't fluctuate; user intent is first-sync anchor |
| `MarkAutoCompleted()` semantic | Pending→Completed, throws otherwise | Reuse `MarkCompleted()` (requires Processing) | Auto-dismiss is origin-side; the two methods have different invariants |
| `UnreadCount` on DTO | `{ get; init; }` property | Positional record param | Avoids breaking existing record constructor calls |
| Source identifier | `"chats"` for source field | Reuse `"messages"` | Enables adapter to distinguish chat vs message mapping |
| Null `lastMessageReadAt` | Treated as unread | Error vs skip | Safe default: show chat rather than hide it |
| InMemory store key | `ConcurrentDictionary<string, WorkItem>` by ExternalId | Keep Guid key + secondary index | Single source of truth for dedup; simpler get/upsert |

## Data Flow

```
Graph /me/chats
     │
     ▼
GraphTeamsSourceProvider.FetchAsync()
  → maps lastMessageReadDateTime, lastMessageDateTime, unreadCount
     │
     ▼
TeamsMessageDto { ExternalId=ChatId, LastMessageReadAt, LastMessageAt, UnreadCount, Source="chats" }
     │
     ▼
TeamsWorkItemMapper.TryMap()
  → SourceType=TeamsChat, Source="chats", Metadata[lastMessageAt/ReadAt/unreadCount]
     │
     ▼
TeamsConnectorAdapter (auto-dismiss gate)
  ├── if lastMessageReadAt >= lastMessageAt → MarkAutoCompleted()
  └── Enqueue to buffer
     │
     ▼
IWorkItemStore.SaveAsync()
  ├── InMemory: _workItems[item.ExternalId] = item (keep Priority)
  └── Sqlite: ON CONFLICT(ExternalId) DO UPDATE (exclude Priority)
     │
     ▼
DashboardPreviewReader.GetAsync()
  → status filter pending
  → project InboxItemPreviewDto.UnreadCount from Metadata["unreadCount"]
```

## Type Changes

### WorkItemSourceType (Domain)
```
Add: TeamsChat = 14
Keep: TeamsMessage = 0
```

### WorkItem.MarkAutoCompleted() (Domain)
```csharp
/// Pending→Completed. Throws InvalidOperationException from Processing/Faulted/Completed.
public void MarkAutoCompleted()
{
    if (Status != WorkItemStatus.Pending)
        throw new InvalidOperationException(
            $"Cannot auto-complete from {Status}. Expected Pending.");
    Status = WorkItemStatus.Completed;
}
```

### TeamsMessageDto (Infrastructure)
```
Add: DateTimeOffset? LastMessageReadAt { get; init; }
Add: DateTimeOffset? LastMessageAt { get; init; }
Add: int UnreadCount { get; init; }
```

### IWorkItemStore (Application)
```
Add: Task<WorkItem?> FindByExternalIdAsync(string externalId, CancellationToken ct)
```

### IWorkItemReader (Application)
```
Add: Task<IReadOnlyList<WorkItem>> ReadForWindowAsync(
         MorningSummaryQuery query, WorkItemStatus? statusFilter, CancellationToken ct)
```

### InboxItemPreviewDto (Application)
```
Add: public int UnreadCount { get; init; }
```

## File Changes

| File | Action | Description |
|---|---|---|
| `src/.../WorkItemSourceType.cs` | Modify | Add `TeamsChat = 14` |
| `src/.../WorkItem.cs` | Modify | Add `MarkAutoCompleted()` method |
| `src/.../TeamsMessageDto.cs` | Modify | Add `LastMessageReadAt`, `LastMessageAt`, `UnreadCount` |
| `src/.../TeamsWorkItemMapper.cs` | Modify | Map new fields to Metadata; use `TeamsChat` when Source=="chats" |
| `src/.../TeamsConnectorAdapter.cs` | Modify | Auto-dismiss logic, chat-level fixtures |
| `src/.../GraphTeamsSourceProvider.cs` | Modify | Map `lastMessageReadDateTime`, `lastMessageDateTime`, `unreadCount` |
| `src/.../IWorkItemStore.cs` | Modify | Add `FindByExternalIdAsync` |
| `src/.../IWorkItemReader.cs` | Modify | Add status-filter overload |
| `src/.../InMemoryWorkItemStore.cs` | Modify | Key by `string ExternalId`, dedup, keep original Priority |
| `src/.../SqliteWorkItemStore.cs` | Modify | `FindByExternalIdAsync`, status filter, remove Priority from upsert SET |
| `src/.../DashboardPreviewReader.cs` | Modify | Filter Pending items, project `UnreadCount` |
| `src/.../DashboardPreviewDto.cs` | Modify | Add `UnreadCount { get; init; }` to `InboxItemPreviewDto` |
| tests/* | Modify | Update fixtures to chat-level; add auto-dismiss/dedup/status-filter tests |

## Interfaces / Contracts

```csharp
// IWorkItemStore — new method
Task<WorkItem?> FindByExternalIdAsync(string externalId, CancellationToken ct);

// IWorkItemReader — new overload (original remains unchanged)
Task<IReadOnlyList<WorkItem>> ReadForWindowAsync(
    MorningSummaryQuery query,
    WorkItemStatus? statusFilter,
    CancellationToken cancellationToken);
```

**InMemoryWorkItemStore dedup contract (`SaveAsync`):**
- Key by `item.ExternalId` in `ConcurrentDictionary<string, WorkItem>`
- If ExternalId exists: update Title, Metadata, CapturedAtUtc, Status, FaultReason
- Retain original Priority from stored entry
- "fail" in ExternalId → `Failure` result (existing test contract)

**SqliteWorkItemStore upsert change:**
```sql
ON CONFLICT(ExternalId) DO UPDATE SET
    Title = excluded.Title,
    Source = excluded.Source,
    SourceType = excluded.SourceType,
    -- Priority intentionally excluded — stable priority
    MetadataJson = excluded.MetadataJson,
    CapturedAtUtc = excluded.CapturedAtUtc,
    Status = excluded.Status,
    FaultReason = excluded.FaultReason;
```

**Auto-dismiss contract:**
```
if (item.Source == "chats"
    && item.Metadata.TryGetValue("teams.lastMessageReadAt", out var readAt)
    && item.Metadata.TryGetValue("teams.lastMessageAt", out var msgAt)
    && DateTimeOffset.Parse(readAt) >= DateTimeOffset.Parse(msgAt))
{
    item.MarkAutoCompleted();
}
```
Null `lastMessageReadAt` → no `TryGetValue` match → auto-dismiss skipped.

## Testing Strategy

| Layer | What | Approach |
|---|---|---|
| Unit — Domain | `MarkAutoCompleted()` state transitions | Fact-per-scenario (Pending→ok, Processing/Faulted/Completed→throws) |
| Unit — Domain | `WorkItemSourceType.TeamsChat` value | Assert enum value = 14 |
| Unit — Store | InMemory dedup by ExternalId | Save twice same ExternalId → 1 entry, keep original Priority |
| Unit — Store | InMemory `FindByExternalIdAsync` | Found returns item, not-found returns null |
| Unit — Store | Sqlite `FindByExternalIdAsync` | Same as InMemory + verify SQL round-trip |
| Unit — Store | Sqlite status filter | Filter by Pending returns only pending items |
| Unit — Mapping | `TeamsWorkItemMapper` with `Source="chats"` | SourceType == TeamsChat, Metadata has chat fields |
| Unit — Mapping | `TeamsWorkItemMapper` with chat metadata | Metadata contains `lastMessageAt`, `lastMessageReadAt`, `unreadCount` |
| Unit — Graph | `GraphTeamsSourceProvider` new fields | DTO has LastMessageReadAt, LastMessageAt, UnreadCount |
| Unit — Graph | Null `lastMessageReadDateTime` | DTO.LastMessageReadAt is null |
| Unit — Adapter | Auto-dismiss when fully read | `MarkAutoCompleted()` called |
| Unit — Adapter | Null LastMessageReadAt — treat as unread | Not auto-completed |
| Unit — Adapter | Chat-level fixtures | Default fixtures use chat ExternalId pattern |
| Unit — Dashboard | Pending filter | Only pending items in result |
| Unit — Dashboard | UnreadCount projection | DTO.UnreadCount from Metadata["unreadCount"] |
| Integration | SQLite round-trip with chat WorkItem | Save + FindByExternalId + ReadForWindow with status filter |
| Architecture | Layer enforcement | ArchTest: no Teams/Graph types leak into Application/Domain |

## Migration / Rollout

No data migration required. Existing message-level WorkItems have different `ExternalId` values (per-message vs ChatId) — the two models coexist via distinct source values (`"messages"` vs `"chats"`). Existing tests use `WorkItemSourceType.TeamsMessage` and continue passing.

## Sequencing

| Step | Capability | Rationale |
|---|---|---|
| 1 | Domain: WorkItemSourceType + MarkAutoCompleted | Foundation — no infra deps |
| 2 | Ports: IWorkItemStore + IWorkItemReader | Contracts before implementations |
| 3 | DTO: TeamsMessageDto new fields | Data shape before mapping |
| 4 | Mapping: TeamsWorkItemMapper | Consumes new DTO fields |
| 5 | Store: InMemoryWorkItemStore | Simplest impl, validates dedup contract |
| 6 | Store: SqliteWorkItemStore | Real impl with SQL |
| 7 | Graph: GraphTeamsSourceProvider | New field mapping |
| 8 | Adapter: TeamsConnectorAdapter | Auto-dismiss + fixtures |
| 9 | Dashboard: DTO + Reader | UnreadCount + Pending filter |
| 10 | Tests: full suite | Validate end-to-end |

## Open Questions

- None — all decisions resolved in proposal + specs.
