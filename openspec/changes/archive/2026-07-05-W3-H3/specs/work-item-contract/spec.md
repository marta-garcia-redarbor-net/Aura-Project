# Delta for work-item-contract

## ADDED Requirements

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
