# Outlook Connector Mapping Specification

## Purpose

Defines mapping rules, partial-payload tolerance, Metadata traceability, and initial
classification rules for the Outlook connector adapter's anti-corruption layer inside
`Aura.Infrastructure`. Outlook email payloads are translated to canonical `WorkItem`
instances; no Outlook or Microsoft Graph types escape this boundary.

Priority MUST be derived from a multi-signal score combining `Importance`, subject cues,
sender address, and body content. No single signal is authoritative; all scoring inputs
are recorded in `WorkItem.Metadata` for explainability.

## Requirements

| Requirement | Constraint | Strength |
|---|---|---|
| Outlook Field Mapping | Valid payload → `WorkItem`; `SourceType = OutlookEmail`; all required fields populated | MUST |
| Unread Inbox Filter | Graph inbox query MUST use `$filter=isRead eq false` and include `isRead` in `$select` | MUST |
| DTO Read State Mapping | `OutlookEmailDto.IsRead` MUST be mapped from Graph `isRead` | MUST |
| Partial Payload Tolerance | Recoverable fields degrade; unrecoverable items skipped; batch MUST NOT abort | MUST |
| Metadata Traceability | Degraded/absent source values AND all classification scoring inputs recorded in `WorkItem.Metadata` | MUST |
| Initial Classification | Priority from multi-signal score (`Importance`, subject, sender, body); all signals traced in Metadata | MUST |
| Clean Architecture Boundary | Outlook/Graph types confined to `Aura.Infrastructure`; adapter output is `Aura.Domain` only | MUST NOT violate |

---

### Requirement: Outlook Field Mapping

The adapter MUST map a valid Outlook email payload to a canonical `WorkItem` with
`SourceType = OutlookEmail`. All required `WorkItem` fields MUST be populated from
corresponding email payload fields.

#### Scenario: Valid email payload produces canonical WorkItem

- GIVEN an Outlook payload with all required fields present
- WHEN the adapter maps the payload
- THEN a `WorkItem` is returned with `SourceType = OutlookEmail` and all required fields populated

#### Scenario: WorkItem SourceType is always OutlookEmail

- GIVEN any Outlook payload that produces a WorkItem
- WHEN the WorkItem is inspected
- THEN `SourceType` equals `OutlookEmail`

---

### Requirement: Unread Inbox Filter

The Graph-backed Outlook source MUST query `/me/mailFolders/inbox/messages` with
`$filter=isRead eq false`. The request MUST include `isRead` in `$select` so the
adapter can preserve read-state semantics explicitly while ingesting only unread
inbox mail.

#### Scenario: Graph query filters unread inbox messages

- GIVEN a user with messages in the Outlook inbox
- WHEN `GraphOutlookSourceProvider` builds the Graph request
- THEN the request URL contains `$filter=isRead eq false`
- AND the endpoint targets `/me/mailFolders/inbox/messages`

#### Scenario: Read email is excluded from unread result set

- GIVEN an Outlook email where `isRead = true`
- WHEN Microsoft Graph returns the filtered inbox result
- THEN that email is not included in the adapter input batch

---

### Requirement: DTO Read State Mapping

`OutlookEmailDto` MUST expose an `IsRead` field and the Graph adapter MUST copy the
Graph `isRead` payload value into that DTO field.

#### Scenario: Graph unread flag maps to DTO false

- GIVEN a Graph payload with `isRead = false`
- WHEN the Outlook adapter maps the message DTO
- THEN `OutlookEmailDto.IsRead` equals `false`

#### Scenario: Graph read flag maps to DTO true

- GIVEN a Graph payload with `isRead = true`
- WHEN the Outlook adapter maps the message DTO
- THEN `OutlookEmailDto.IsRead` equals `true`

---

### Requirement: Partial Payload Tolerance

The adapter MUST produce a `WorkItem` from a payload with missing or invalid optional
fields by applying safe defaults. The adapter MUST NOT abort batch processing for a
single recoverable item. Items lacking required fields (e.g., absent `ExternalId`)
MUST be skipped; the batch MUST continue.

#### Scenario: Missing optional field produces degraded WorkItem

- GIVEN an Outlook payload with a missing optional field
- WHEN the adapter maps the payload
- THEN a `WorkItem` is returned with a safe default for that field

#### Scenario: All scoring signals absent produces Medium priority

- GIVEN a payload where `Importance`, subject, sender, and body carry no recognizable priority signal
- WHEN the adapter maps the payload
- THEN `Priority = Medium`

#### Scenario: Missing required field skips item without aborting batch

- GIVEN a payload missing a required field (e.g., `ExternalId`)
- WHEN the adapter processes a batch containing that payload
- THEN that item is skipped AND remaining items are mapped normally

---

### Requirement: Metadata Traceability

`WorkItem.Metadata` MUST record: (1) degraded, defaulted, or absent source field values
— each entry identifies the field name and original raw value where available;
(2) all classification scoring inputs: `Importance` value or its absence, subject cues
matched, sender weight applied, and body cues matched.

#### Scenario: Defaulted source field recorded in Metadata

- GIVEN a payload where a field was defaulted
- WHEN the resulting WorkItem is inspected
- THEN `Metadata` contains an entry with the field name and original source value

#### Scenario: Absent source field recorded in Metadata

- GIVEN a payload where a non-required field is absent
- WHEN the resulting WorkItem is inspected
- THEN `Metadata` contains an entry indicating the field was absent

#### Scenario: Classification scoring inputs recorded in Metadata

- GIVEN an Outlook payload that produces a classified `WorkItem`
- WHEN `WorkItem.Metadata` is inspected
- THEN entries are present for: `Importance` value or absence, subject cues matched, sender weight, and body cues matched

---

### Requirement: Initial Classification

| Signal | Source Field | Role |
|--------|-------------|------|
| Importance flag | `Importance` | Priority weight when present |
| Subject cues | `Subject` | Priority and deadline signals |
| Sender weight | `SenderAddress` | Priority weight |
| Body cues | `BodyPreview` | Priority signals; deadline fallback |

Priority MUST combine all four signals. No single signal SHALL be the sole determinant.
When `Importance` is absent or unrecognized, remaining signals MUST still produce a
defensible priority; the adapter MUST NOT fall back to `Medium` when sender or body
signals carry evidence. Deadline cues MUST be derived from subject or body patterns.
Source MUST always be `OutlookEmail`.

#### Scenario: High-importance email maps to High priority

- GIVEN an Outlook payload with `Importance = High`
- WHEN the adapter maps the payload
- THEN `Priority = High`

#### Scenario: Absent Importance with strong sender signal produces elevated priority

- GIVEN a payload where `Importance` is absent and `SenderAddress` matches a high-weight sender rule
- WHEN the adapter maps the payload
- THEN the resulting `Priority` reflects the sender signal and is not `Medium`

#### Scenario: Absent Importance with body cue produces elevated priority

- GIVEN a payload where `Importance` is absent and `BodyPreview` contains a recognized high-priority cue
- WHEN the adapter maps the payload
- THEN `Priority` is elevated above `Medium` by the body signal

#### Scenario: Deadline keyword in subject sets deadline cue in Metadata

- GIVEN a payload with a subject containing a deadline pattern (e.g., "due", "by EOD", date reference)
- WHEN the adapter maps the payload
- THEN `WorkItem.Metadata` contains a deadline cue entry derived from the subject

#### Scenario: No deadline indicator produces no Metadata deadline entry

- GIVEN a payload with no deadline indicators in subject or body
- WHEN the adapter maps the payload
- THEN `WorkItem.Metadata` contains no deadline cue entry

---

### Requirement: Clean Architecture Boundary

Outlook-specific DTOs, payload types, and fixture data MUST remain within `Aura.Infrastructure`.
No Outlook or Microsoft Graph SDK type MAY appear in `Aura.Application` or `Aura.Domain`.
Adapter output MUST consist exclusively of `Aura.Domain` types.

#### Scenario: Architecture test rejects Outlook type leakage

- GIVEN an Outlook-specific type is referenced in `Aura.Application`
- WHEN architecture tests run
- THEN at least one test fails identifying the offending type

#### Scenario: Adapter returns only canonical domain types

- GIVEN an Outlook payload is processed by the adapter
- WHEN the adapter output is inspected
- THEN only `Aura.Domain` types are present in the returned value
