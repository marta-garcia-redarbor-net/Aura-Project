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
| Post-Persistence Diff Lifecycle | Successful Outlook runs MUST diff pending persisted ids against the current batch and auto-complete absent Outlook items | MUST |
| Application-Owned Pending Filter | Pending/completed filtering MUST live in Application/store layers; UI MUST NOT own unread-state filtering | MUST |
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

### Requirement: Post-Persistence Diff Lifecycle

After a successful Outlook connector run persists the current batch, the use case MUST
retrieve the pending persisted Outlook `ExternalId` values, compare them against the
current batch ids, and mark absent Outlook ids as `Completed`. The diff MUST run only
after persistence succeeds and MUST NOT run when the Graph execution returns a failure.

#### Scenario: Read Outlook email is auto-completed on the next successful poll

- GIVEN pending Outlook items with external ids `A`, `B`, and `C`
- WHEN the persisted batch contains only `A` and `B`
- THEN the use case invokes `MarkCompletedAsync` for `C`
- AND item `C` transitions to `Completed`

#### Scenario: Graph failure skips the diff lifecycle

- GIVEN the Outlook connector returns a failure from Graph
- WHEN the use case handles that result
- THEN the post-persistence diff does not execute
- AND no persisted work item state changes to `Completed`

#### Scenario: Inbox zero completes all pending Outlook items

- GIVEN persisted Outlook items exist in `Pending`
- WHEN a successful Graph poll returns zero unread Outlook emails
- THEN every pending Outlook item absent from the empty batch is marked `Completed`

#### Scenario: First successful empty sync does not invoke completion for a blank store

- GIVEN no Outlook work items are currently persisted in `Pending`
- WHEN a successful Outlook poll returns zero unread emails
- THEN `GetPendingExternalIdsAsync` returns an empty set
- AND `MarkCompletedAsync` is not invoked

#### Scenario: Non-Outlook pending items are not affected by the Outlook diff

- GIVEN pending Teams items and pending Outlook items exist together
- WHEN the Outlook batch omits one Outlook external id
- THEN only the missing Outlook external id is passed to `MarkCompletedAsync`
- AND non-Outlook items remain unchanged

---

### Requirement: Application-Owned Pending Filter

Pending/completed filtering for Outlook unread behavior MUST be applied in the
Application and persistence layers. UI consumers MUST receive already-filtered data and
MUST NOT implement additional read-state filtering to hide completed Outlook items.

#### Scenario: Application requests only pending ids for the diff

- GIVEN persisted Outlook items with mixed statuses
- WHEN the diff lifecycle prepares its comparison set
- THEN it reads only `Pending` Outlook ids from the store

#### Scenario: UI receives already-filtered pending state

- GIVEN an Outlook item was auto-completed by the Application diff lifecycle
- WHEN downstream UI readers request inbox data
- THEN completed Outlook items are excluded by Application/store filtering
- AND the UI applies no extra read-state logic

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

---

### Requirement: Continuous Polling Execution

The ConnectorExecutionWorker MUST run as a continuous background polling service. The worker MUST NOT stop the application host after a single execution cycle. The loop MUST exit only when a cancellation is requested via the stopping token.

#### Scenario: Worker polls at configured interval

- GIVEN the worker is started with a `PollingInterval` of 5 minutes
- WHEN the worker completes one execution cycle
- THEN the worker waits `PollingInterval` before starting the next cycle
- AND the application host remains running

#### Scenario: Worker stops gracefully on cancellation

- GIVEN the worker is in a delay between polling cycles
- WHEN a cancellation is requested via the stopping token
- THEN the delay completes or is cancelled immediately
- AND the worker exits without calling `StopApplication`

#### Scenario: Worker continues after adapter failure

- GIVEN one adapter throws an exception during execution
- WHEN the execution cycle completes (success or failure)
- THEN the worker logs the error
- AND the worker continues to the next polling cycle without stopping

---

### Requirement: Fresh DI Scope Per Iteration

The worker MUST create a new `IServiceScope` at the start of each polling iteration. All dependencies resolved within the iteration MUST come from that scope. The scope MUST be disposed when the iteration ends, whether it succeeds or fails.

#### Scenario: Each iteration resolves fresh dependencies

- GIVEN the worker starts a new polling iteration
- WHEN services are resolved for the iteration
- THEN they come from a new DI scope
- AND the scope is disposed after the iteration completes

#### Scenario: Scope disposal on exception

- GIVEN the worker is in the middle of an iteration and an unhandled exception occurs
- WHEN the catch block executes
- THEN the scope is disposed in a finally block
- AND the worker continues to the next polling cycle

---

### Requirement: Configurable Polling Interval

The worker MUST support a configurable polling interval with a default of 5 minutes. The interval SHOULD be read from application configuration. A `PollingInterval` property with a fallback default MUST be present.

#### Scenario: Default interval applied when unconfigured

- GIVEN no custom polling interval is configured
- WHEN the worker starts
- THEN the polling interval defaults to 5 minutes

#### Scenario: Custom interval from configuration

- GIVEN configuration specifies a polling interval of 10 minutes
- WHEN the worker starts
- THEN the worker polls every 10 minutes

---

### Requirement: Application Lifetime Independence

The worker MUST NOT depend on `IHostApplicationLifetime`. The host lifecycle MUST be managed by the runtime, not by any worker. Any previous `IHostApplicationLifetime` dependency MUST be removed.

#### Scenario: Worker does not inject application lifetime

- GIVEN the worker class is instantiated
- WHEN its constructor parameters are inspected
- THEN `IHostApplicationLifetime` is not among them
