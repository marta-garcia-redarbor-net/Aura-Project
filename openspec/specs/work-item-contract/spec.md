# Work Item Contract Specification

## Purpose

Defines the canonical mandatory-field contract for the `WorkItem` domain entity:
required fields, the `sourceType` closed set, normalization rules for `correlationId`
and `capturedAtUtc`, the fixed `schemaVersion`, and the minimal `Metadata` shape.

## Requirements

### Requirement: Mandatory Field Presence

The `WorkItem` constructor MUST reject construction when any of the following fields
is absent or empty: `externalId`, `sourceType`, `title`, `source`, `priority`, or `Metadata`.

#### Scenario: All mandatory fields provided

- GIVEN a caller supplies all mandatory fields with valid non-empty values
- WHEN the `WorkItem` is constructed
- THEN the entity is created successfully and all fields are accessible

#### Scenario: Missing mandatory field

- GIVEN a caller omits any single mandatory field
- WHEN the `WorkItem` is constructed
- THEN construction is rejected with an argument validation error

---

### Requirement: sourceType Closed-Set Validation

`sourceType` MUST be one of: `teams-message`, `teams-chat`, `slack-message`, `outlook-email`, `calendar-appointment`, `pr-review`, `todo-task`. Any other value MUST be rejected.
(Previously: closed set omitted `teams-chat`)

#### Scenario: Valid existing sourceType

- GIVEN a caller supplies `sourceType = "outlook-email"`
- WHEN the `WorkItem` is constructed
- THEN construction succeeds

#### Scenario: Valid new sourceType teams-chat

- GIVEN a caller supplies `sourceType = "teams-chat"`
- WHEN the `WorkItem` is constructed
- THEN construction succeeds

#### Scenario: Invalid sourceType

- GIVEN a caller supplies `sourceType = "unknown-source"`
- WHEN the `WorkItem` is constructed
- THEN construction is rejected with an argument validation error

---

### Requirement: correlationId Normalization

`correlationId` MUST be present on every `WorkItem`. If the caller does not provide
a non-empty value, the system MUST generate a unique identifier automatically.

#### Scenario: correlationId provided by caller

- GIVEN a caller supplies a non-empty `correlationId`
- WHEN the `WorkItem` is constructed
- THEN `correlationId` equals the supplied value

#### Scenario: correlationId absent — system generates

- GIVEN a caller omits or passes an empty `correlationId`
- WHEN the `WorkItem` is constructed
- THEN `correlationId` is set to a non-empty system-generated value

---

### Requirement: capturedAtUtc Resolution

`capturedAtUtc` MUST reflect the source timestamp when provided by the caller.
When absent, the system MUST substitute the current UTC time at ingestion.

#### Scenario: Source timestamp provided

- GIVEN a caller supplies a valid UTC `capturedAtUtc`
- WHEN the `WorkItem` is constructed
- THEN `capturedAtUtc` equals the supplied timestamp

#### Scenario: Source timestamp absent

- GIVEN a caller omits `capturedAtUtc`
- WHEN the `WorkItem` is constructed
- THEN `capturedAtUtc` is set to the current UTC time

---

### Requirement: Fixed schemaVersion

Every `WorkItem` MUST carry `schemaVersion = "v1"`. This value is fixed by the
domain and SHALL NOT be overridden by the caller.

#### Scenario: schemaVersion on every constructed item

- GIVEN any valid `WorkItem` construction input
- WHEN the `WorkItem` is constructed
- THEN `schemaVersion` equals `"v1"`

---

### Requirement: Metadata Shape

`Metadata` MUST be a non-null dictionary with string keys and string values.
An empty dictionary is valid. A null `Metadata` MUST be rejected.

#### Scenario: Empty metadata accepted

- GIVEN a caller supplies an empty `Metadata` dictionary
- WHEN the `WorkItem` is constructed
- THEN construction succeeds and `Metadata` is an accessible empty collection

#### Scenario: Null metadata rejected

- GIVEN a caller passes `null` for `Metadata`
- WHEN the `WorkItem` is constructed
- THEN construction is rejected with an argument validation error

---

### Requirement: Mandatory Field Whitespace Rejection

The `WorkItem` constructor MUST treat whitespace-only strings for `externalId`, `title`,
and `source` as absent and MUST reject construction.

(Supplements: Mandatory Field Presence — whitespace boundary coverage)

#### Scenario: externalId whitespace-only rejected

- GIVEN a caller supplies `externalId` as a whitespace-only string (e.g. `"   "`)
- WHEN the `WorkItem` is constructed
- THEN construction is rejected with an argument validation error

#### Scenario: title whitespace-only rejected

- GIVEN a caller supplies `title` as a whitespace-only string
- WHEN the `WorkItem` is constructed
- THEN construction is rejected with an argument validation error

#### Scenario: source whitespace-only rejected

- GIVEN a caller supplies `source` as a whitespace-only string
- WHEN the `WorkItem` is constructed
- THEN construction is rejected with an argument validation error

---

### Requirement: correlationId Whitespace Auto-Generation

A whitespace-only `correlationId` MUST be treated as absent; the system MUST
auto-generate a unique identifier.

(Supplements: correlationId Normalization — whitespace boundary coverage)

#### Scenario: correlationId whitespace-only triggers auto-generation

- GIVEN a caller supplies `correlationId` as a whitespace-only string (e.g. `"   "`)
- WHEN the `WorkItem` is constructed
- THEN `correlationId` is set to a non-empty system-generated value

---

### Requirement: capturedAtUtc Boundary Inputs

When `capturedAtUtc` is supplied as `DateTimeOffset.MinValue`, the system MUST
treat it as absent and substitute the current UTC time. When a caller supplies a
non-zero-offset `DateTimeOffset`, the system SHOULD preserve the supplied value
without rejection.

(Supplements: capturedAtUtc Resolution — `DateTimeOffset?` boundary coverage)

#### Scenario: DateTimeOffset.MinValue treated as absent

- GIVEN a caller supplies `capturedAtUtc = DateTimeOffset.MinValue`
- WHEN the `WorkItem` is constructed
- THEN `capturedAtUtc` is set to the current UTC time, not `DateTimeOffset.MinValue`

#### Scenario: Local-offset DateTimeOffset preserved without rejection

- GIVEN a caller supplies `capturedAtUtc` as a `DateTimeOffset` with a non-zero local offset
- WHEN the `WorkItem` is constructed
- THEN `capturedAtUtc` is accepted and set to the supplied value

---

### Requirement: Metadata Populated Dictionary Accepted

A non-null `Metadata` dictionary with one or more string key-value entries MUST be
accepted and all entries MUST be accessible on the constructed `WorkItem`.

(Supplements: Metadata Shape — populated dictionary positive coverage)

#### Scenario: Populated metadata accepted and preserved

- GIVEN a caller supplies a `Metadata` dictionary with one or more entries (e.g. `{ "key": "value" }`)
- WHEN the `WorkItem` is constructed
- THEN construction succeeds and all supplied entries are accessible via `Metadata`

---

### Requirement: MarkAutoCompleted State Transition

`WorkItem` MUST expose a `MarkAutoCompleted()` method that transitions `Status` from `Pending` to `Completed`. The method MUST throw `InvalidOperationException` when called from `Processing`, `Faulted`, or `Completed` status. This method SHALL NOT affect the existing `MarkCompleted()` path.

#### Scenario: Pending transitions to Completed

- GIVEN a `WorkItem` with `Status = Pending`
- WHEN `MarkAutoCompleted()` is called
- THEN `Status` equals `Completed`

#### Scenario: Processing throws

- GIVEN a `WorkItem` with `Status = Processing`
- WHEN `MarkAutoCompleted()` is called
- THEN `InvalidOperationException` is thrown

#### Scenario: Completed throws

- GIVEN a `WorkItem` with `Status = Completed`
- WHEN `MarkAutoCompleted()` is called
- THEN `InvalidOperationException` is thrown

#### Scenario: Faulted throws

- GIVEN a `WorkItem` with `Status = Faulted`
- WHEN `MarkAutoCompleted()` is called
- THEN `InvalidOperationException` is thrown

### Requirement: TeamsChat sourceType Enum Value

`WorkItemSourceType` MUST define a `TeamsChat` member with integer value `14`. The existing `TeamsMessage` member SHALL remain unchanged.

#### Scenario: TeamsChat has correct value

- GIVEN the `WorkItemSourceType` enum
- WHEN the `TeamsChat` member is inspected
- THEN its integer value equals `14`

---

### Requirement: PriorityScore Field

`WorkItem` MUST expose a nullable `int? PriorityScore` property. The field SHALL be optional in the constructor (defaults to `null`). `PriorityScore` is assigned during triage evaluation — it MUST NOT be computed on-the-fly.

#### Scenario: Constructor accepts optional PriorityScore

- GIVEN a caller does not supply `PriorityScore`
- WHEN a `WorkItem` is constructed
- THEN `PriorityScore` is `null`

#### Scenario: Constructor accepts explicit PriorityScore

- GIVEN a caller supplies `PriorityScore = 85`
- WHEN a `WorkItem` is constructed
- THEN `PriorityScore` equals `85`

#### Scenario: PriorityScore is persisted and stable

- GIVEN a `WorkItem` with `PriorityScore = 72`
- WHEN the item is retrieved from the store
- THEN `PriorityScore` remains `72` across the round-trip

---

### Requirement: PriorityScore Default Derivation

When `PriorityScore` is `null`, the system SHALL derive an effective order value from `WorkItemPriority` for sorting: `Critical → 100`, `High → 75`, `Normal → 50`, `Low → 25`. The derivation MUST NOT mutate the stored `null` value on the entity.

#### Scenario: Critical priority defaults to 100

- GIVEN a `WorkItem` with `Priority = Critical` and `PriorityScore = null`
- WHEN the item is included in a sorted query
- THEN its effective sort value is `100`

#### Scenario: Low priority defaults to 25

- GIVEN a `WorkItem` with `Priority = Low` and `PriorityScore = null`
- WHEN the item is included in a sorted query
- THEN its effective sort value is `25`

#### Scenario: Explicit PriorityScore overrides default

- GIVEN a `WorkItem` with `Priority = Low` and `PriorityScore = 90`
- WHEN the item is included in a sorted query
- THEN its sort value is `90` (the explicit score, not `25`)

---

### Requirement: PriorityScore on DTO

All `WorkItem` DTOs (`WorkItemDetailDto`, `WorkItemSummaryDto`, etc.) MUST carry a nullable `PriorityScore` property matching the domain entity.

#### Scenario: DTO carries PriorityScore

- GIVEN a `WorkItem` with `PriorityScore = 80`
- WHEN it is mapped to a DTO
- THEN the DTO's `PriorityScore` equals `80`

#### Scenario: Null PriorityScore on DTO

- GIVEN a `WorkItem` with `PriorityScore = null`
- WHEN it is mapped to a DTO
- THEN the DTO's `PriorityScore` is `null`

---

### Requirement: Priority Ordering on API Queries

`GET /api/workitems` and all detail views MUST return items sorted by `PriorityScore` DESCENDING (highest first). Items with `null` PriorityScore SHALL be ordered by their derived default value for comparison. Items with equal effective score SHALL be sub-ordered by `capturedAtUtc` DESC.

#### Scenario: Sorted by PriorityScore DESC

- GIVEN items with `PriorityScore = 90`, `50`, and `null` (Critical → 100)
- WHEN `GET /api/workitems` is called
- THEN the returned order is: Critical-default (100), 90, 50

#### Scenario: Equal scores sub-ordered by recency

- GIVEN two items with `PriorityScore = 80` captured at different times
- WHEN `GET /api/workitems` is called
- THEN the more recent item appears first
