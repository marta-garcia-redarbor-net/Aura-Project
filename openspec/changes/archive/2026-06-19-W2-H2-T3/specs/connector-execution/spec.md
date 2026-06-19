# Delta for connector-execution

## MODIFIED Requirements

### Requirement: Canonical Execution Result

The canonical execution result MUST contain: connector identity, item count, status, and a
max-processed-item timestamp (the highest timestamp among successfully processed items, or
null if no items were processed successfully). Status MUST be one of: success, failure, or
partial-failure. A failure result MUST include a non-empty reason string. Failed items MUST
be excluded from the max-processed-item timestamp computation.
(Previously: result contained identity, item count, status, and reason only; no
max-processed-item timestamp; no partial-failure status.)

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
(Previously: read-only; MUST NOT write; checkpoint write was explicitly deferred to W2-H2-T3.
Now read-then-persist with four-outcome policy.)

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
