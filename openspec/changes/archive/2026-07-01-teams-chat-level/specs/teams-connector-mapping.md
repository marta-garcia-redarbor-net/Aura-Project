# Delta for teams-connector-mapping

## MODIFIED Requirements

### Requirement: Teams Field Mapping

The adapter MUST map a valid Teams chat payload to a canonical `WorkItem` with `SourceType = TeamsChat`. The `WorkItem.ExternalId` MUST be set to the chat's `ChatId` (e.g., `19:abc@thread.v2`). The `WorkItem.Source` MUST be `"chats"`. The adapter MUST include `lastMessageAt`, `lastMessageReadAt`, and `unreadCount` in `WorkItem.Metadata`. All required fields MUST be populated from the corresponding payload fields.
(Previously: mapped TeamsMessage source type; ExternalId was per-message GUID; no chat-level metadata)

#### Scenario: Valid Teams chat payload produces canonical WorkItem

- GIVEN a valid Teams chat payload with ChatId, title, message timestamps, and unread count
- WHEN the adapter maps the payload
- THEN a `WorkItem` is returned with `SourceType = TeamsChat`, `Source = "chats"`, and `ExternalId` set to the chat's `ChatId`
- AND `Metadata` contains `lastMessageAt`, `lastMessageReadAt`, and `unreadCount`

#### Scenario: WorkItem SourceType is always TeamsChat

- GIVEN any Teams chat payload that produces a WorkItem
- WHEN the resulting WorkItem is inspected
- THEN `SourceType` equals `TeamsChat`

## ADDED Requirements

### Requirement: Auto-Dismiss on Read Chat

After mapping, BEFORE enqueueing the WorkItem, the adapter MUST check chat read status. If `lastMessageReadAt >= lastMessageAt`, the adapter MUST call `MarkAutoCompleted()` on the WorkItem. If `lastMessageReadAt` is null, the chat SHALL be treated as unread.

#### Scenario: Read chat auto-completes

- GIVEN a chat where `lastMessageReadAt >= lastMessageAt`
- WHEN the adapter processes the mapped WorkItem
- THEN `MarkAutoCompleted()` is called
- AND the WorkItem transitions to Completed status

#### Scenario: Null lastMessageReadAt treated as unread

- GIVEN a chat where `lastMessageReadAt` is null
- WHEN the adapter processes the mapped WorkItem
- THEN `MarkAutoCompleted()` is NOT called
- AND the WorkItem remains in Pending status

#### Scenario: Partially read chat stays pending

- GIVEN a chat where `lastMessageReadAt < lastMessageAt`
- WHEN the adapter processes the mapped WorkItem
- THEN `MarkAutoCompleted()` is NOT called
- AND the WorkItem remains in Pending status
