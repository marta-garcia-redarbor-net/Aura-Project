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

`sourceType` MUST be one of: `teams-message`, `slack-message`, `outlook-email`,
`calendar-appointment`, `pr-review`, `todo-task`. Any other value MUST be rejected.

#### Scenario: Valid sourceType

- GIVEN a caller supplies `sourceType = "outlook-email"`
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
