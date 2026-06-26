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
| Canonical Execution Result | identity + item count + status + max-processed-item timestamp; failure MUST include reason | MUST |
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

The use case MUST receive a `CheckpointIdentity` that carries an optional `UserOid` field. Before invoking the connector adapter, the use case MUST verify that `UserOid` is populated. If `UserOid` is null, the use case MUST return a failure result with reason "no cached user identity" and MUST NOT invoke the adapter.

#### Scenario: Use case executes with valid oid

- GIVEN a valid connector identity with `UserOid` populated
- WHEN the use case is invoked
- THEN the port's result is returned to the caller unchanged

#### Scenario: Use case skips connector when no oid

- GIVEN a connector identity with `UserOid` = null
- WHEN the use case is invoked
- THEN a failure result is returned with reason "no cached user identity"
- AND the adapter is NOT invoked

#### Scenario: Use case propagates typed failure

- GIVEN the port returns a typed failure
- WHEN the use case processes it
- THEN the typed failure is returned without re-throwing

---

### Requirement: Canonical Execution Result

The canonical execution result MUST contain: connector identity, item count, status, and a
max-processed-item timestamp (the highest timestamp among successfully processed items, or
null if no items were processed successfully). Status MUST be one of: success, failure, or
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

Telemetry MUST additionally include:
- Structured log for `MsalUiRequiredException` with oid correlation
- Structured log for Graph HTTP failures including status code, endpoint URL, and connector name
- Metric `graph.token.acquired` emitted on successful token acquisition
- Metric `graph.token.expired` emitted when `MsalUiRequiredException` is caught
- Metric `graph.http.error` emitted on 4xx/5xx Graph responses, tagged by status code

#### Scenario: Successful run emits correlated telemetry

- GIVEN execution completes
- WHEN telemetry is inspected
- THEN trace span, item-count metric, and log entry share one correlation identifier

#### Scenario: Failed run emits error-level telemetry

- GIVEN execution fails
- WHEN telemetry is inspected
- THEN a trace span, metric (count = 0), and error-level log share the same correlation identifier

#### Scenario: MsalUiRequiredException emits re-auth telemetry

- GIVEN a Graph call fails with `MsalUiRequiredException` for user oid "abc-123"
- WHEN telemetry is inspected
- THEN a structured log entry with level Warning is emitted
- AND the log contains oid = "abc-123" and connector name
- AND metric `graph.token.expired` is incremented

#### Scenario: Graph HTTP 4xx emits error telemetry

- GIVEN a Graph API call returns HTTP 403 Forbidden
- WHEN telemetry is inspected
- THEN a structured log entry with level Warning is emitted
- AND the log contains status code = 403, endpoint URL, and connector name
- AND metric `graph.http.error` is incremented with status_code = 403

#### Scenario: Graph HTTP 5xx emits error telemetry

- GIVEN a Graph API call returns HTTP 503 Service Unavailable
- WHEN telemetry is inspected
- THEN a structured log entry with level Error is emitted
- AND the log contains status code = 503, endpoint URL, and connector name
- AND metric `graph.http.error` is incremented with status_code = 503

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

---

## New requirement: Delegated Token Acquisition

The system MUST acquire Graph tokens exclusively via delegated flows using `IPublicClientApplication`. The factory MUST filter cached accounts by `oid` match on `HomeAccountId.ObjectId` rather than using `FirstOrDefault()`.

#### Scenario: Oid-based account selection returns correct account

- GIVEN two cached accounts with oids "oid-A" and "oid-B"
- WHEN `CreateClientAsync("oid-B", ct)` is called
- THEN the account with oid "oid-B" is selected for token acquisition

#### Scenario: No matching account throws MsalUiRequiredException

- GIVEN no cached account matches the requested oid "oid-unknown"
- WHEN `CreateClientAsync("oid-unknown", ct)` is called
- THEN `MsalUiRequiredException` is thrown (no silent token available)

#### Scenario: Public client application uses no client secret

- GIVEN `IPublicClientApplication` is registered in DI
- WHEN a Graph client is created
- THEN no client secret is used in the token acquisition flow

---

## New requirement: Worker Oid Resolution

Workers MUST resolve the user oid from the persisted token cache before invoking the connector use case. If no account is cached (user has never logged in via API), the worker MUST log a warning and skip that connector execution.

#### Scenario: Worker resolves oid from token cache

- GIVEN a user account with oid "oid-A" exists in the SQLite token cache
- WHEN the worker prepares to execute a connector for that user
- THEN `CheckpointIdentity.UserOid` is set to "oid-A"

#### Scenario: Worker skips connector when no cached user

- GIVEN the token cache is empty (no accounts)
- WHEN the worker prepares to execute a connector
- THEN a warning log is emitted
- AND the connector execution is skipped
- AND no Graph call is attempted

#### Scenario: Worker propagates oid to all three providers

- GIVEN a connector identity with `UserOid` = "oid-A"
- WHEN Teams, Outlook, or Calendar providers invoke `IGraphClientFactory.CreateClientAsync`
- THEN each receives `oid = "oid-A"` as the first parameter
