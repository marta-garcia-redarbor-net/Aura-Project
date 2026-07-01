# Teams Connector Mapping Specification

## Purpose

Defines mapping rules, partial-payload tolerance, and Metadata traceability for the Teams
connector adapter's anti-corruption layer inside `Aura.Infrastructure`.
Teams payloads are translated to canonical `WorkItem` instances; no Teams or
Graph types escape this boundary.

## Requirements

| Requirement | Constraint | Strength |
|---|---|---|
| Teams Field Mapping | Valid payload produces `WorkItem` with all fields populated from source | MUST |
| Partial Payload Tolerance | Recoverable missing/invalid fields produce degraded item; batch MUST NOT abort | MUST |
| Metadata Traceability | Degraded, defaulted, or absent source values recorded in `WorkItem.Metadata` | MUST |
| Clean Architecture Boundary | Teams/Graph types confined to `Aura.Infrastructure`; adapter output is `Aura.Domain` only | MUST NOT violate |

---

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

---

### Requirement: Partial Payload Tolerance

The adapter MUST produce a `WorkItem` from a Teams payload with missing or invalid
optional fields by applying safe defaults. The adapter MUST NOT abort batch processing
for a single item with recoverable issues. Items for which a valid `WorkItem` cannot be
constructed (e.g., absent `ExternalId`) MUST be skipped; the batch MUST continue.

#### Scenario: Missing optional field produces degraded WorkItem

- GIVEN a Teams payload with a missing optional field
- WHEN the adapter maps the payload
- THEN a `WorkItem` is returned with a safe default for that field
- AND all other items in the batch are mapped normally

#### Scenario: Unrecognized priority value defaults to Medium

- GIVEN a Teams payload with an unrecognized or absent priority value
- WHEN the adapter maps the payload
- THEN the `WorkItem` is produced with `Priority = Medium`

#### Scenario: Missing required field skips item without aborting batch

- GIVEN a Teams payload missing a field required for a valid `WorkItem` (e.g., `ExternalId`)
- WHEN the adapter processes a batch containing that payload
- THEN that item is skipped
- AND remaining items are mapped and returned normally

---

### Requirement: Metadata Traceability

The adapter MUST record degraded, defaulted, or absent source field values in
`WorkItem.Metadata`. Each entry MUST identify the field name and, where available,
the raw source value that was discarded or replaced.

#### Scenario: Defaulted field recorded in Metadata

- GIVEN a Teams payload where a field was defaulted (e.g., priority resolved to Medium)
- WHEN the resulting WorkItem is inspected
- THEN `Metadata` contains an entry identifying the defaulted field and the original source value

#### Scenario: Absent field recorded in Metadata

- GIVEN a Teams payload where a non-required field is absent
- WHEN the resulting WorkItem is inspected
- THEN `Metadata` contains an entry indicating the field was absent

---

### Requirement: Clean Architecture Boundary

Teams-specific DTOs, payload types, and fixture data MUST remain within `Aura.Infrastructure`.
No Teams or Microsoft Graph SDK type MAY appear in `Aura.Application` or `Aura.Domain`.
The adapter output MUST consist exclusively of `Aura.Domain` types.

#### Scenario: Architecture test rejects Teams type leakage

- GIVEN a Teams-specific type is referenced in `Aura.Application`
- WHEN architecture tests run
- THEN at least one test fails identifying the offending type

#### Scenario: Adapter returns only canonical domain types

- GIVEN a Teams payload is processed by the adapter
- WHEN the adapter output is inspected
- THEN only `Aura.Domain` types are present in the returned value

---

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
