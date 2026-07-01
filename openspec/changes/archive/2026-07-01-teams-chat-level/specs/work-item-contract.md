# Delta for work-item-contract

## MODIFIED Requirements

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

## ADDED Requirements

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
