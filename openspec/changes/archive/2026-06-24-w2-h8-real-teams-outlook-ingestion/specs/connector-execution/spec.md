# Delta for Connector Execution

## ADDED Requirements

### Requirement: Partial Degradation Handling

The execution orchestration MUST support partial degradation across multiple sources. If one connector fails (e.g., Teams), the remaining connectors (e.g., Outlook) MUST continue to execute and sync their data independently.

#### Scenario: One connector fails while others succeed

- GIVEN multiple connectors are orchestrated to run
- WHEN the Teams connector fails to sync
- THEN the Outlook connector continues to run and completes successfully
- AND the system reports the overall status as partially degraded

## MODIFIED Requirements

### Requirement: Canonical Execution Result

The canonical execution result MUST contain: connector identity, item count, status, a
max-processed-item timestamp (the highest timestamp among successfully processed items, or
null if no items were processed successfully), and partial degradation details per source if applicable. Status MUST be one of: success, failure, or
partial-failure. A failure result MUST include a non-empty reason string. Failed items MUST
be excluded from the max-processed-item timestamp computation.
(Previously: Execution result did not explicitly require tracking partial degradation visibility per source.)

#### Scenario: Success result contains required fields

- GIVEN execution completes with 5 items
- WHEN the result is inspected
- THEN connector identity, item count = 5, and status = success are present

#### Scenario: Failure result contains reason

- GIVEN execution fails
- WHEN the result is inspected
- THEN status = failure and a non-empty reason string are present

#### Scenario: Full success with items — all fields present

- GIVEN execution completes with 5 items, each carrying a timestamp
- WHEN the result is inspected
- THEN status = success, item count = 5, and max-processed-at equals the highest of the 5 item timestamps

#### Scenario: Full success with no items — max-processed-at is null

- GIVEN execution completes but the source returns zero items
- WHEN the result is inspected
- THEN status = success, item count = 0, and max-processed-at = null

#### Scenario: Full failure — reason present, max-processed-at is null

- GIVEN execution fails entirely before any item is processed
- WHEN the result is inspected
- THEN status = failure, reason is a non-empty string, and max-processed-at = null

#### Scenario: Partial failure — max-processed-at reflects successful items only

- GIVEN execution processes 3 items successfully and fails on 2 others
- WHEN the result is inspected
- THEN status = partial-failure and max-processed-at equals the highest timestamp among the 3 successful items
- AND the 2 failed items do not contribute to max-processed-at
- AND the partial degradation state is captured in the result