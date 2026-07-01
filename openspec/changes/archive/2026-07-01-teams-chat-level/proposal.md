# Proposal: Chat-Level Teams WorkItems

## Intent

Each unread Teams chat creates one WorkItem. Users see chats needing attention, not individual messages. The current message-level model floods the dashboard; the chat-level model maps to how users actually triage — "should I open this chat or not?"

## Scope

### In Scope

- WorkItem = 1 chat/channel with unread messages, keyed by ExternalId = ChatId (`19:abc@thread.v2`)
- Auto-dismiss: WorkItem auto-completes when `lastMessageReadAt >= lastMessageAt`
- Dashboard shows only `Status = Pending` items
- Source identifier `"chats"` used consistently
- `InMemoryWorkItemStore` dedup by ExternalId
- `IWorkItemReader` status filter overload
- `IWorkItemStore.FindByExternalIdAsync` for auto-dismiss detection
- `WorkItem.MarkAutoCompleted()` (Pending→Completed transition)
- `WorkItemSourceType.TeamsChat` enum value
- Fixture data swapped to chat-level

### Out of Scope

- Multi-message preview (up to 3 + "..." badge) — deferred follow-up
- ChatType, member count, or topic metadata in priority calculation (follow-up)
- `TeamsMessageDto` rename to `TeamsChatDto` (additive fields only)

## Capabilities

### New Capabilities

None. All changes modify existing specs.

### Modified Capabilities

| Capability | Change |
|---|---|
| `teams-connector-mapping` | Chat-level field mapping, auto-dismiss requirement, `WorkItemSourceType.TeamsChat` |
| `work-item-contract` | `MarkAutoCompleted()` method, `TeamsChat` in `sourceType` closed set |
| `work-item-persistence` | `FindByExternalIdAsync` on `IWorkItemStore`, status filter on `IWorkItemReader`, InMemory dedup fix |
| `dashboard-inbox-preview` | Pending-only filter, `UnreadCount` on `InboxItemPreviewDto` |
| `graph-connector-status` | `lastMessageReadDateTime` mapping in `GraphTeamsSourceProvider` |

## Approach

`GraphTeamsSourceProvider` already maps chats → DTOs. Extend mapping to include `lastMessageReadDateTime`. `TeamsWorkItemMapper` adds `lastMessageReadAt`/`lastMessageAt` to Metadata. After mapping, `TeamsConnectorAdapter` checks: if `lastMessageReadAt >= lastMessageAt` → call `MarkAutoCompleted()` before enqueueing.

`IWorkItemStore` gains `FindByExternalIdAsync` for dedup detection. `InMemoryWorkItemStore` switches from `Guid` key to `ExternalId` key. `IWorkItemReader` adds status-filter overload. `DashboardPreviewReader` filters to `Pending` items only, surfaces `UnreadCount` from metadata.

## Affected Areas

| Area | Impact | Key Changes |
|---|---|---|
| `WorkItem.cs` | Modified | `MarkAutoCompleted()` — Pending→Completed |
| `WorkItemSourceType.cs` | Modified | Add `TeamsChat = 14` |
| `IWorkItemStore.cs` | Modified | Add `FindByExternalIdAsync` |
| `IWorkItemReader.cs` | Modified | Add status-filter overload |
| `TeamsMessageDto.cs` | Modified | Add `LastMessageReadAt`, `LastMessageAt`, `UnreadCount` |
| `TeamsWorkItemMapper.cs` | Modified | Map new metadata fields |
| `TeamsConnectorAdapter.cs` | Modified | Auto-dismiss logic, chat-level fixtures |
| `GraphTeamsSourceProvider.cs` | Modified | Map `lastMessageReadDateTime` |
| `InMemoryWorkItemStore.cs` | Modified | Key by ExternalId, dedup |
| `SqliteWorkItemStore.cs` | Modified | `FindByExternalIdAsync`, status filter |
| `DashboardPreviewReader.cs` | Modified | Pending-only filter, unread count |
| `DashboardPreviewDto.cs` | Modified | `UnreadCount` init-only property |
| `tests/...Teams*Tests.cs` | Modified | Chat-level fixtures, auto-dismiss tests |
| `tests/...InMemory*Tests.cs` | Modified | Dedup tests |

## Work Item Status Transition Decision

**Problem:** `MarkCompleted()` requires `Processing` state. Auto-dismiss needs `Pending→Completed` without the intermediate step.

**Decision:** Add `MarkAutoCompleted()` to `WorkItem`:

```
Pending ──MarkProcessing()──→ Processing ──MarkCompleted()──→ Completed
  └────────MarkAutoCompleted()──────────────────────────────→ Completed
```

Only `Pending` source is allowed — other states throw `InvalidOperationException`. The existing `Processing→Completed` path is untouched. This keeps the domain explicit about *why* the transition happened (origin-side completion vs user-triggered).

## Risks

| Risk | Likelihood | Mitigation |
|---|---|---|
| `lastMessageReadDateTime` null for some chat types | Medium | Treat null as "unread" — safe default that shows the chat |
| InMemory dedup fix breaks existing tests | Medium | Audit test fixtures; the "fail on 'fail'" pattern still works via ExternalId |
| Auto-dismiss race: chat read between fetch and persist | Low | Eventually consistent — next sync catches it |
| `InboxItemPreviewDto` positional record breaking change | Low | Add `UnreadCount` as `{ get; init; }` property, not constructor param |

## Rollback Plan

1. Revert all file changes (this is additive/modifies existing files — no schema migrations)
2. Restore `InMemoryWorkItemStore` to `ConcurrentDictionary<Guid, WorkItem>` keying
3. Existing message-level fixture data still works for other tests — no data migration needed
4. Restore `IWorkItemReader` and `IWorkItemStore` interfaces to current signatures
5. `WorkItem.MarkAutoCompleted()` can stay as dead code or be removed — it's only called from the Teams connector

## Success Criteria

- [ ] Each Teams chat with unread messages produces exactly 1 WorkItem (ExternalId = ChatId)
- [ ] Auto-dismiss: WorkItems for read chats (`lastMessageReadAt >= lastMessageAt`) are persisted as Completed
- [ ] Dashboard only shows Pending WorkItems (Completed/Faulted hidden)
- [ ] `InMemoryWorkItemStore` dedup: same ExternalId, two saves → one entry
- [ ] `FindByExternalIdAsync` returns the correct WorkItem by ExternalId in both stores
- [ ] All unit, integration, and architecture tests pass (`dotnet test Aura.sln`)
- [ ] Architecture tests enforce no Teams/Graph types in Application or Domain
