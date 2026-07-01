# Exploration: teams-chat-level — Chat-Level Teams WorkItems

**Change:** `teams-chat-level`
**Date:** 2026-07-01
**Artifact store:** openspec
**Status:** Ready for Proposal

---

## Current State

### What the Teams connector does today

The Teams connector pipeline currently:

```
GraphTeamsSourceProvider.FetchAsync()
  → GET /me/chats?$top=50
  → Maps each Chat → TeamsMessageDto (source: "chats", ExternalId: chat.Id)
    ↓
TeamsConnectorAdapter.ExecuteAsync()
  → TeamsWorkItemMapper.TryMap(dto) → WorkItem (SourceType: TeamsMessage, source: "chats")
  → Buffer.Enqueue(workItem)
    ↓
ExecuteConnectorUseCase.PersistWorkItemsAsync()
  → IWorkItemStore.SaveAsync(item) per item
```

**Key architectural fact: `GraphTeamsSourceProvider` already maps at the CHAT level** — each chat becomes one DTO, one WorkItem. The ExternalId is the ChatId (`chat.Id`, e.g. `19:abc@thread.v2`). It gets the sender/preview from `chat.LastMessagePreview` (a single message preview). Source is set to `"chats"`.

However, the **default fixtures** in `TeamsConnectorAdapter.cs` (lines 98–129) still use message-level data:
- ExternalId = `"teams-msg-1001"` (message IDs, not chat IDs)
- Source = `"messages"` (not `"chats"`)
- No `Sender`, `BodyPreview`, `WebUrl` in fixtures

This means there's a **mismatch** between the Graph provider (chat-level) and the fixtures (message-level).

### What the fixture-only path looks like (no Graph provider)

When `IMessageSourceProvider<TeamsMessageDto>` is null, `TeamsConnectorAdapter` falls back to static fixtures:
- 2 valid items, 1 skipped (null ExternalId)
- Source = `"messages"`, ExternalId = `"teams-msg-1001"` / `"teams-msg-1003"`
- Priority resolved from raw string

### WorkItem domain model — status transitions

```
Pending ──MarkProcessing()──→ Processing ──MarkCompleted()──→ Completed
                                       ──MarkFaulted()──────→ Faulted
```

**Critical constraint:** `MarkCompleted()` (line 76–83 of `WorkItem.cs`) **only allows transition from `Processing`**. There is no path from `Pending` → `Completed`. For auto-dismiss (detect that a chat was read in the origin and automatically complete the WorkItem without user interaction), this transition must be allowed.

### WorkItem stores — dedup comparison

| Store | Key | Dedup |
|-------|-----|-------|
| `SqliteWorkItemStore` | `ExternalId TEXT NOT NULL UNIQUE` | ✅ `ON CONFLICT(ExternalId) DO UPDATE SET ...` |
| `InMemoryWorkItemStore` | `ConcurrentDictionary<Guid, WorkItem>` | ❌ Keys by `item.Id` (new GUID per call) |

SqliteWorkItemStore correctly deduplicates. InMemoryWorkItemStore does NOT — each call to `SaveAsync` overwrites by GUID, so the same ExternalId creates duplicates if the WorkItem is mapped more than once.

### Current store/reader interfaces

```csharp
// IWorkItemStore — line 8
public interface IWorkItemStore {
    Task<WorkItemPersistenceResult> SaveAsync(WorkItem item, CancellationToken ct);
}

// IWorkItemReader — line 17
public interface IWorkItemReader {
    Task<IReadOnlyList<WorkItem>> ReadForWindowAsync(MorningSummaryQuery query, CancellationToken ct);
}
```

- MorningSummaryQuery has only `UserId`, `FromUtc`, `ToUtc` — no status filter
- IWorkItemReader returns ALL items in the time window regardless of status
- Neither interface has a `FindByExternalId` method needed for auto-dismiss detection

### DashboardPreviewReader

- Reads all WorkItems in a 24h window (no status filter)
- Groups by Source string
- For Teams chats: extracts `teams.sender`, `teams.snippet`, `teams.deepLink` from metadata
- No unread count or chat-level grouping
- `InboxItemPreviewDto` has flat title/source/score/suggestedAction + optional sender/snippet/deepLink/priorityHint/syncState

### GraphTeamsSourceProvider — what it fetches today

Calls `GET /me/chats` with `$top=50`. Maps from each chat:
- `chat.Id` → ExternalId
- `chat.Topic` → Title (falls back to `"Teams chat {chat.Id}"`)
- `chat.LastUpdatedDateTime` → CapturedAtUtc
- `chat.LastMessagePreview?.From?.User?.DisplayName` → Sender
- `chat.LastMessagePreview?.Body?.Content` → BodyPreview (truncated to 200 chars)
- `chat.WebUrl` → WebUrl

**Not fetched:** `chat.LastMessageReadDateTime`, `chat.CreatedDateTime`, `chat.ChatType`.

---

## Affected Areas

### `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsMessageDto.cs`
- Add `LastMessageReadAt` (DateTimeOffset?) — when the current user last read the chat
- Add `LastMessageAt` (DateTimeOffset?) — timestamp of the most recent message
- Add `UnreadCount` (int?) — number of unread messages (if available)
- The `BodyPreview`/`Sender` stay as `lastMessagePreview` fields
- Consider renaming to `TeamsChatDto` to reflect chat-level semantics (backward-compatible: add alias or new record)

### `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsWorkItemMapper.cs`
- Add `lastMessageReadAt`, `lastMessageAt`, `chatType` to metadata
- Add `chat.topic` or `chat.memberCount` to metadata for priority calculation
- The "auto-dismiss" logic (check `lastMessageReadAt >= lastMessageAt`) can go here, calling a new `MarkCompleted()` variant after construction

### `src/Aura.Domain/WorkItems/WorkItem.cs`
- **Critical:** Add `MarkAutoCompleted()` method that allows `Pending` → `Completed` transition for auto-dismissed items
- Signature: `public void MarkAutoCompleted()` — same as `MarkCompleted()` but accepts `Pending` as the source state

### `src/Aura.Domain/WorkItems/WorkItemSourceType.cs`
- Add `TeamsChat` value (or keep `TeamsMessage` but document it now represents chats). New value is cleaner but requires migration of existing enum data. Given the change name "teams-chat-level", adding `TeamsChat = 14` is clearer.

### `src/Aura.Application/Ports/IWorkItemStore.cs`
- Add `Task<WorkItem?> FindByExternalIdAsync(string externalId, CancellationToken ct)` — needed for auto-dismiss detection and dedup in the connector pipeline

### `src/Aura.Application/Ports/IWorkItemReader.cs`
- Add optional status filter to `ReadForWindowAsync`: either add `WorkItemStatus? statusFilter` parameter, or add a new method `ReadForWindowAsync(MorningSummaryQuery, WorkItemStatus?, CancellationToken)`. Overload is backward-compatible.

### `src/Aura.Infrastructure/Adapters/WorkItems/InMemoryWorkItemStore.cs`
- Fix dedup: change storage from `ConcurrentDictionary<Guid, WorkItem>` to `ConcurrentDictionary<string, WorkItem>` keyed by `ExternalId`
- Update the `SaveAsync` to upsert by ExternalId (match Sqlite behavior)
- Implement `FindByExternalIdAsync` (hash lookup)

### `src/Aura.Infrastructure/Adapters/WorkItems/SqliteWorkItemStore.cs`
- Add `FindByExternalIdAsync` implementation: `SELECT ... WHERE ExternalId = @ExternalId`
- Add optional status filter to `ReadForWindowAsync` (with backward-compatible overload)

### `src/Aura.Application/Services/DashboardPreviewReader.cs`
- Filter out `Completed` and `Faulted` items (only show `Pending`)
- Add `UnreadCount` to the `InboxItemPreviewDto` when building Teams entries
- The snippet should show `lastMessagePreview` (already in metadata as `teams.snippet`)

### `src/Aura.Application/Models/DashboardPreviewDto.cs`
- Add `UnreadCount` (int?) to `InboxItemPreviewDto`
- Add `MessagePreviewCount` (int) for dashboard rendering (how many messages to show before truncation)

### `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphTeamsSourceProvider.cs`
- Add `chat.LastMessageReadDateTime` to the DTO mapping
- Add `chat.CreatedDateTime` to DTO mapping
- **Critical design choice:** filter to only chats with unread messages (`lastMessageReadDateTime < lastUpdatedDateTime`) OR return ALL chats and let auto-dismiss handle it downstream
  - Recommended: fetch ALL chats, let the connector handle auto-dismiss downstream. This keeps the source provider pure (fetch + map).

### `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsConnectorAdapter.cs`
- Update default fixtures to represent chats (ExternalId = chat-like IDs like `"19:abc@thread.v2"`, Source = `"chats"`, include `Sender`, `BodyPreview`, `WebUrl`)
- Add fixture `LastMessageReadAt` fields to demonstrate auto-dismiss scenarios
- The adapter should handle auto-dismiss: after mapping, if `lastMessageReadAt >= lastMessageAt`, mark the WorkItem as auto-completed

### `src/Aura.Infrastructure/Adapters/Connectors/DependencyInjection.cs`
- No changes expected (the Teams connector is already registered)

### `tests/Aura.UnitTests/Ingestion/Teams/TeamsWorkItemMapperTests.cs`
- Add tests for chat-level DTO mapping (ExternalId = ChatId format)
- Add tests for `lastMessageReadAt` / `lastMessageAt` metadata
- Add tests for auto-dismiss when read timestamp >= last message timestamp

### `tests/Aura.UnitTests/Ingestion/Teams/TeamsConnectorAdapterTests.cs`
- Update fixture data to chat-level
- Add test for auto-dismiss flow (fixture with `lastMessageReadAt >= lastMessageAt` → WorkItem saved as Completed)

### `tests/Aura.UnitTests/GraphConnector/GraphTeamsSourceProviderTests.cs`
- Update fake chat data to include `lastMessageReadDateTime`
- Add test verifying `lastMessageReadDateTime` is mapped into DTO

### `tests/Aura.UnitTests/WorkItems/InMemoryWorkItemStoreTests.cs`
- Add test for `FindByExternalIdAsync`
- Add test for ExternalId dedup (same ExternalId, two SaveAsync calls → one entry retained)
- Modify existing tests where the fail-on-"fail" behavior could interact with ExternalId keying

### `tests/Aura.UnitTests/WorkItems/SqliteWorkItemStoreTests.cs`
- Add test for `FindByExternalIdAsync` (round-trip)
- Add test for status filter on `ReadForWindowAsync`

### `openspec/changes/teams-chat-level/` (new)
- This `exploration.md`
- Future: `specs/`, `design.md`, `tasks.md` as the SDD progresses

---

## Approaches

### 1. Chat-Level Model with Auto-Dismiss in Connector (RECOMMENDED)

Extend the existing Teams data model to represent chats, add auto-dismiss logic in the connector adapter, and add store/reader capabilities for finding by ExternalId and filtering by status.

**Implementation sequence:**

1. **Domain layer:** Add `WorkItemSourceType.TeamsChat`, add `MarkAutoCompleted()` to `WorkItem`
2. **Application layer:** Extend `IWorkItemStore` (add `FindByExternalIdAsync`), extend `IWorkItemReader` (add status filter overload)
3. **Infrastructure DTO:** Extend `TeamsMessageDto` with `LastMessageReadAt`, `LastMessageAt`, `UnreadCount`; consider renaming to `TeamsChatDto`
4. **Infrastructure mapper:** Update `TeamsWorkItemMapper` to add new metadata fields, handle auto-dismiss logic
5. **Infrastructure store:** Fix `InMemoryWorkItemStore` dedup, implement `FindByExternalIdAsync` in both stores
6. **Infrastructure Graph provider:** Add `lastMessageReadDateTime` to `GraphTeamsSourceProvider` mapping
7. **Infrastructure connector:** Update `TeamsConnectorAdapter` fixtures to chat-level data
8. **Application dashboard:** Filter out non-Pending items in `DashboardPreviewReader`, surface unread count
9. **Tests:** All layers (mapper, adapter, store, Graph provider, dashboard reader)

**Pros:**
- Clear separation of concerns — each layer changes only what it owns
- Auto-dismiss happens at the right level (connector knows when a chat is "read")
- InMemoryWorkItemStore dedup fix aligns it with Sqlite behavior
- Dashboard naturally filters to only actionable items

**Cons:**
- Requires changes across all Clean Architecture layers
- TeamsMessageDto rename could break existing tests if not aliased

**Effort:** Medium (10–15 files, ~400–600 changed lines)

### 2. Minimal — Just Chat Source Provider Changes, No Auto-Dismiss

Only change `GraphTeamsSourceProvider` and the mapper to properly represent chat-level data. Keep the existing WorkItem status model (no `MarkAutoCompleted`). Don't change store/reader interfaces. Auto-dismiss is handled as a separate change.

**Pros:**
- Smallest surface area
- Forward-compatible with later auto-dismiss
- No changes to domain model or store interfaces

**Cons:**
- Chat WorkItems can never auto-dismiss — they stay Pending forever
- Must still fix InMemoryWorkItemStore dedup (it's a correctness bug regardless)
- Not actually meeting the requirement

**Effort:** Low (4–6 files, ~150–250 lines)

### 3. Full Chat with Multi-Message Preview

In addition to chat-level WorkItems and auto-dismiss, implement the full "3 unread message preview" requirement: `GraphTeamsSourceProvider` calls `GET /me/chats/{chatId}/messages?$top=3&$filter=createdDateTime gt {lastMessageReadAt}` per chat to get the actual unread messages for the preview.

**Pros:**
- Delivers the complete UX described in requirements
- Users see actual unread message content, not just the last message

**Cons:**
- N+1 Graph API calls per sync (50 chats = 51 API calls)
- Rate limiting risk
- Significantly higher complexity for the first slice
- Can be deferred to a follow-up without breaking the core model

**Effort:** High (15–20 files, ~600–800 lines + Graph API pagination)

---

## Graph API Endpoints

| Endpoint | Purpose | Currently Used | Required Scope |
|----------|---------|---------------|----------------|
| `GET /me/chats` | List all chats the user is in | ✅ Yes | `Chat.ReadBasic`, `Chat.Read` |
| `GET /me/chats/{chatId}/messages?$top=3&$filter=...` | Get recent unread messages for preview | ❌ No | `Chat.Read` |
| `chat.lastMessageReadDateTime` | When user last read the chat | ❌ Not mapped | Returned with `Chat.Read` |
| `chat.lastUpdatedDateTime` | When last update occurred | ✅ Mapped as CapturedAtUtc | Returned with `Chat.ReadBasic` |

For the first slice: fetch `lastMessageReadDateTime` directly from the `/me/chats` response (it's a property of the `chat` resource). Do NOT call the per-chat messages endpoint until the multi-message preview is needed.

---

## Fixture/Seed Data Design

New fixture data should represent chats, not individual messages:

```csharp
private static IReadOnlyList<TeamsMessageDto> LoadDefaultFixtures() =>
[
    // Chat 1: Unread messages (active)
    new TeamsMessageDto
    {
        ExternalId = "19:active-chat@thread.v2",
        Title = "Sprint Planning",
        Source = "chats",
        Priority = "high",
        Sender = "Alice",
        BodyPreview = "Can everyone review the sprint goals?",
        WebUrl = "https://teams.microsoft.com/l/chat/19:active-chat@thread.v2",
        LastMessageAt = DateTimeOffset.UtcNow.AddMinutes(-15),
        LastMessageReadAt = DateTimeOffset.UtcNow.AddHours(-2),
        CapturedAtUtc = DateTimeOffset.UtcNow,
        CorrelationId = "corr-chat-1"
    },
    // Chat 2: Already read (should auto-dismiss)
    new TeamsMessageDto
    {
        ExternalId = "19:read-chat@thread.v2",
        Title = "Lunch Plans",
        Source = "chats",
        Priority = "low",
        Sender = "Bob",
        BodyPreview = "Pizza at 1pm?",
        WebUrl = "https://teams.microsoft.com/l/chat/19:read-chat@thread.v2",
        LastMessageAt = DateTimeOffset.UtcNow.AddHours(-1),
        LastMessageReadAt = DateTimeOffset.UtcNow.AddMinutes(-30), // read AFTER last message
        CapturedAtUtc = DateTimeOffset.UtcNow,
        CorrelationId = "corr-chat-2"
    },
    // Chat 3: No read timestamp (never read) — unread
    new TeamsMessageDto
    {
        ExternalId = "19:unread-chat@thread.v2",
        Title = "Production Incident",
        Source = "chats",
        Priority = "critical",
        Sender = "Carol",
        BodyPreview = "PagerDuty alert: CPU at 95%",
        WebUrl = "https://teams.microsoft.com/l/chat/19:unread-chat@thread.v2",
        LastMessageAt = DateTimeOffset.UtcNow.AddMinutes(-5),
        LastMessageReadAt = null, // never read
        CapturedAtUtc = DateTimeOffset.UtcNow,
        CorrelationId = "corr-chat-3"
    },
    // Chat 4: Missing ExternalId — should be skipped (existing pattern)
    new TeamsMessageDto
    {
        ExternalId = null,
        Title = "Missing id should be skipped",
        Source = "chats",
        Priority = "low"
    }
];
```

---

## WorkItem Status Transition Issue

**Current constraint:** `MarkCompleted()` requires `Processing` status.

**Need:** Allow `Pending` → `Completed` for auto-dismiss when `lastMessageReadAt >= lastMessageAt`.

**Solution:** Add a new method to `WorkItem`:

```csharp
/// <summary>
/// Transition from <see cref="WorkItemStatus.Pending"/> to <see cref="WorkItemStatus.Completed"/>.
/// Used when the origin system indicates the work is already done (e.g., chat read in Teams).
/// </summary>
public void MarkAutoCompleted()
{
    if (Status != WorkItemStatus.Pending)
        throw new InvalidOperationException(
            $"Cannot transition to AutoCompleted from {Status}. Expected Pending.");

    Status = WorkItemStatus.Completed;
}
```

This keeps the existing `Processing → Completed` path intact while adding a bypass for origin-side completion.

**Placement of the call:** After `TryMap` creates the WorkItem, in `TeamsConnectorAdapter.ExecuteAsync`:

```csharp
if (_mapper.TryMap(payload, out var workItem) && workItem is not null)
{
    // Auto-dismiss if the chat was already read
    if (payload.LastMessageReadAt is not null &&
        payload.LastMessageAt is not null &&
        payload.LastMessageReadAt >= payload.LastMessageAt)
    {
        workItem.MarkAutoCompleted();
    }
    _buffer.Enqueue(workItem);
    mappedCount++;
    continue;
}
```

---

## Store/Reader Changes

### IWorkItemStore addition

```csharp
public interface IWorkItemStore
{
    Task<WorkItemPersistenceResult> SaveAsync(WorkItem item, CancellationToken ct);
    
    /// <summary>
    /// Finds a stored WorkItem by its ExternalId, or null if not found.
    /// Used by connectors to check if an item already exists and for auto-dismiss.
    /// </summary>
    Task<WorkItem?> FindByExternalIdAsync(string externalId, CancellationToken ct);
}
```

### InMemoryWorkItemStore dedup fix

**Current (broken):**
```csharp
private readonly ConcurrentDictionary<Guid, WorkItem> _workItems = new();
// ...
_workItems[item.Id] = item; // Keys by new GUID — no dedup
```

**Fixed:**
```csharp
private readonly ConcurrentDictionary<string, WorkItem> _workItems = new(StringComparer.OrdinalIgnoreCase);
// ...
_workItems[item.ExternalId] = item; // Keys by ExternalId — natural dedup
```

And implement `FindByExternalIdAsync`:
```csharp
public Task<WorkItem?> FindByExternalIdAsync(string externalId, CancellationToken ct)
{
    ct.ThrowIfCancellationRequested();
    _workItems.TryGetValue(externalId, out var item);
    return Task.FromResult(item);
}
```

**Note:** The existing "fail on 'fail' in ExternalId" test behavior would still work since `ExternalId` containing "fail" would still be caught before the upsert.

### IWorkItemReader status filter

**Current:**
```csharp
Task<IReadOnlyList<WorkItem>> ReadForWindowAsync(MorningSummaryQuery query, CancellationToken ct);
```

**New (backward-compatible overload):**
```csharp
Task<IReadOnlyList<WorkItem>> ReadForWindowAsync(MorningSummaryQuery query, CancellationToken ct);
Task<IReadOnlyList<WorkItem>> ReadForWindowAsync(MorningSummaryQuery query, WorkItemStatus? statusFilter, CancellationToken ct);
```

Or add the parameter with a default value to avoid a new method — but an overload is cleaner for backward compatibility with existing callers.

---

## DashboardPreviewReader Changes

The reader needs to:
1. **Filter out non-Pending items** — only show WorkItems the user actually needs to act on

```csharp
// Current: all items returned
var items = await _workItemReader.ReadForWindowAsync(query, cancellationToken);

// New: only pending items (completed/faulted are filtered out)
var items = await _workItemReader.ReadForWindowAsync(query, WorkItemStatus.Pending, cancellationToken);
```

2. **Surface unread count** for Teams chats — the count should be derived from metadata or Graph API. For the first slice, default `UnreadCount` to a sensible value (at least 1 if the chat has unread messages, or track it explicitly in metadata). Add `UnreadCount` to `InboxItemPreviewDto`:

```csharp
// In InboxItemPreviewDto
public int? UnreadCount { get; init; }
```

The `InboxSourceGroupDto` grouping already works by Source name — Teams chats with Source = `"chats"` will group naturally.

---

## Risks

1. **Graph `lastMessageReadDateTime` availability** — This property may not be populated for all chat types (e.g., meeting chats). The adapter must handle null gracefully and treat the chat as "unread" when the value is null.

2. **InMemoryWorkItemStore dedup fix breaks existing tests** — The keying change from `Guid` to `string` (ExternalId) requires updating the existing InMemoryWorkItemStore tests. The "fail on ExternalId containing 'fail'" behavior is preserved but it now checks the ExternalId not the Id.

3. **Auto-dismiss timing** — If a chat is read in Teams between the sync's fetch and persist, the WorkItem could be saved as Pending even though it should be Completed. This is eventually consistent and acceptable — the next sync will catch it.

4. **TeamsMessageDto rename risk** — Renaming to `TeamsChatDto` is clearer but breaks references across 6+ files (mapper, connector, Graph provider, tests, DI). Safer approach for first slice: add new fields to the existing DTO and add a documentation comment that it represents chat-level data. Rename can be a follow-up.

5. **InboxItemPreviewDto constructor breaking change** — Adding `UnreadCount` as a positional constructor parameter to `InboxItemPreviewDto` (line 20, a positional record) would be a **source-breaking change** for all call sites. Must add as `{ get; init; }` property, not a constructor parameter:

```csharp
public sealed record InboxItemPreviewDto(
    string Title,
    string Source,
    string RelativeTimestamp,
    double Score,
    string SuggestedAction)
{
    // Existing init-only properties
    public string? Sender { get; init; }
    public string? Snippet { get; init; }
    public string? DeepLink { get; init; }
    public string? PriorityHint { get; init; }
    public string? SyncState { get; init; }
    
    // New
    public int? UnreadCount { get; init; }
}
```

6. **Confusion between `Source` field and `WorkItemSourceType` enum** — Currently, `WorkItem.Source` is a string (`"messages"` or `"chats"`) while `SourceType` is the enum (`TeamsMessage`). For chat-level work, the Source should consistently be `"chats"` (not `"messages"`). The Graph provider already sets it to `"chats"` — but the mapper defaults to `"messages"` when Source is null. This must be consistent in fixtures and real data.

---

## Estimated Complexity

| Layer | Files Changed | Changed Lines |
|-------|--------------|---------------|
| Domain | 2 (`WorkItem.cs`, `WorkItemSourceType.cs`) | ~20 |
| Application (Ports) | 2 (`IWorkItemStore.cs`, `IWorkItemReader.cs`) | ~15 |
| Application (Models) | 1 (`DashboardPreviewDto.cs`) | ~5 |
| Application (Services) | 1 (`DashboardPreviewReader.cs`) | ~5 |
| Infrastructure (DTO) | 1 (`TeamsMessageDto.cs`) | ~10 |
| Infrastructure (Mapper) | 1 (`TeamsWorkItemMapper.cs`) | ~25 |
| Infrastructure (Connector) | 1 (`TeamsConnectorAdapter.cs`) | ~25 |
| Infrastructure (Graph Provider) | 1 (`GraphTeamsSourceProvider.cs`) | ~10 |
| Infrastructure (Store) | 2 (`InMemoryWorkItemStore.cs`, `SqliteWorkItemStore.cs`) | ~40 |
| Tests | 5 files | ~200 |
| **Total** | **~17 files** | **~355 lines** |

This excludes OpenSpec spec/delta files for the formal SDD (specs, design, tasks).

---

## Openspec Capability Analysis

| Capability | Action |
|------------|--------|
| `teams-connector-mapping` | **MODIFY** — add chat-level field mapping, auto-dismiss requirement |
| `work-item-contract` | **MODIFY** — add `MarkAutoCompleted`, `TeamsChat` SourceType |
| `work-item-store` | **MODIFY** — add `FindByExternalIdAsync`, status filter |
| `dashboard-inbox-preview` | **MODIFY** — add `UnreadCount`, Pending-only filtering |
| `graph-ingestion-teams` | **MODIFY** — add `lastMessageReadDateTime` mapping |
| `connector-execution` | **No change** — the use case is unchanged |

---

## Ready for Proposal

**Yes.** All architectural questions have been resolved. The change touches 17 files across all Clean Architecture layers but each change is bounded and testable.

Key clarifications to carry into the proposal:
- Confirm `TeamsMessageDto` stays as-is (no rename) vs becomes `TeamsChatDto`
- Confirm whether `WorkItemSourceType` gets a new `TeamsChat` value or reuses `TeamsMessage`
- Confirm whether the "3 unread message preview" is in-scope for this slice or deferred
- Confirm fixture update strategy: update default fixtures immediately or keep old ones and add new
