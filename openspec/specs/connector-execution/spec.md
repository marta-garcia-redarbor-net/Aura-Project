# Connector Execution Specification

## Purpose

Provider-neutral Application-layer contract and use case for executing a single ingestion
connector and returning a canonical result with correlated telemetry.
Teams is the first connector in this slice.

## Scope Boundaries

| Item | Status | Tracked In |
|------|--------|------------|
| Teams field mapping to canonical model | Out of scope | W2-H3 |
| Checkpoint persistence, delta sync, idempotency | Out of scope | W2-H2-T3 |
| Outlook, Calendar, GitHub connectors | Out of scope | Separate backlog |
| Scheduling, retry, resilience | Out of scope | Separate backlog |

## Requirements

| Requirement | Constraint | Strength |
|---|---|---|
| Connector Execution Port | Provider-neutral; all types MUST be Aura.Application or BCL | MUST |
| Connector Execution Use Case | Invokes the port; no Infrastructure or SDK type references | MUST |
| Canonical Execution Result | identity + item count + status + max-processed-item timestamp + partial degradation details; failure MUST include reason | MUST |
| Partial Degradation Handling | If one connector fails, remaining connectors MUST continue executing independently | MUST |
| Telemetry Emission | Trace span + item-count metric + log; share one correlation ID | MUST |
| Clean Architecture Boundary | No SDK types above Infrastructure; enforced by arch tests | MUST NOT violate |
| Checkpoint Read-Only Integration | Read checkpoint to bound fetch window; read-then-persist with four-outcome policy | MUST |

---

### Requirement: Connector Execution Port

#### Scenario: Valid identity returns canonical result

- GIVEN a connector identity is passed to the port
- WHEN the port is invoked
- THEN a result with identity, item count, and status is returned

#### Scenario: Unregistered connector returns typed failure

- GIVEN a connector identity with no registered adapter
- WHEN the port is invoked
- THEN a typed failure is returned and no exception propagates

---

### Requirement: Connector Execution Use Case

#### Scenario: Use case executes and returns result

- GIVEN a valid connector identity is provided
- WHEN the use case is invoked
- THEN the port's result is returned to the caller unchanged

#### Scenario: Use case propagates typed failure

- GIVEN the port returns a typed failure
- WHEN the use case processes it
- THEN the typed failure is returned without re-throwing

---

### Requirement: Partial Degradation Handling

The execution orchestration MUST support partial degradation across multiple sources. If one connector fails (e.g., Teams), the remaining connectors (e.g., Outlook) MUST continue to execute and sync their data independently.

#### Scenario: One connector fails while others succeed

- GIVEN multiple connectors are orchestrated to run
- WHEN the Teams connector fails to sync
- THEN the Outlook connector continues to run and completes successfully
- AND the system reports the overall status as partially degraded

---

### Requirement: Canonical Execution Result

The canonical execution result MUST contain: connector identity, item count, status, a
max-processed-item timestamp (the highest timestamp among successfully processed items, or
null if no items were processed successfully), and partial degradation details per source if applicable. Status MUST be one of: success, failure, or
partial-failure. A failure result MUST include a non-empty reason string. Failed items MUST
be excluded from the max-processed-item timestamp computation.

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

---

### Requirement: Telemetry Emission

#### Scenario: Successful run emits correlated telemetry

- GIVEN execution completes
- WHEN telemetry is inspected
- THEN trace span, item-count metric, and log entry share one correlation identifier

#### Scenario: Failed run emits error-level telemetry

- GIVEN execution fails
- WHEN telemetry is inspected
- THEN a trace span, metric (count = 0), and error-level log share the same correlation identifier

---

### Requirement: Clean Architecture Boundary

#### Scenario: Architecture test rejects SDK leakage

- GIVEN an external SDK type is referenced in Aura.Application
- WHEN architecture tests run
- THEN at least one test fails identifying the offending type

#### Scenario: Use case has no Infrastructure references

- GIVEN the use case type dependencies are enumerated
- WHEN all types are listed
- THEN no Aura.Infrastructure or external SDK type is found

---

### Requirement: Checkpoint Read-Only Integration

The use case MUST read the checkpoint before execution to bound the fetch window and MUST
persist the updated checkpoint after execution according to the following policy:

| Run Outcome | max-processed-at | execution-finished-at |
|---|---|---|
| Full success + new items | Advance to highest successful item timestamp | Advance to now |
| Full success + no items | Unchanged | Advance to now |
| Full failure | Unchanged | Unchanged |
| Partial/mixed failure | Advance to highest successful item timestamp | Unchanged |

The use case MUST NOT advance execution-finished-at on any outcome other than full success.
Max-processed-at MUST be derived from successfully processed items only; failed items MUST
be excluded. The use case MUST NOT expose or depend on provider SDK types when applying this
policy.

#### Scenario: Existing checkpoint bounds fetch window

- GIVEN a checkpoint exists for the connector identity
- WHEN the use case executes
- THEN the fetch window is bounded by the stored checkpoint timestamps

#### Scenario: Absent checkpoint applies today-only window

- GIVEN no checkpoint exists for the connector identity
- WHEN the use case executes
- THEN the fetch window defaults to UTC today and execution proceeds normally

#### Scenario: Full success with new items advances both timestamps

- GIVEN the run processes at least one item successfully and zero items fail
- WHEN execution completes
- THEN the checkpoint is persisted with max-processed-at = highest item timestamp AND execution-finished-at = current time

#### Scenario: Full success with no items advances execution-finished-at only

- GIVEN the run completes successfully and the source returns zero items
- WHEN execution completes
- THEN execution-finished-at is advanced and max-processed-at remains unchanged

#### Scenario: Full failure advances neither timestamp

- GIVEN all items fail to process (or the connector itself fails)
- WHEN execution completes
- THEN no checkpoint is written and both timestamps retain their previous values

#### Scenario: Partial failure advances max-processed-at only

- GIVEN some items are processed successfully and others fail
- WHEN execution completes
- THEN max-processed-at is advanced to the highest successful item timestamp
- AND execution-finished-at remains unchanged

#### Scenario: Repeated run over same window does not regress checkpoint

- GIVEN a checkpoint exists with max-processed-at = T1 and a re-run fetches the identical window
- WHEN the second run completes successfully with the same items
- THEN max-processed-at is not set to a value earlier than T1
