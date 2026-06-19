# Delta for ingestion-checkpoint-store

## MODIFIED Requirements

### Requirement: Checkpoint Value Shape

A checkpoint value MUST contain exactly three fields: a nullable cursor/deltaToken (string),
a nullable max-processed-at timestamp (DateTimeOffset — the highest timestamp among
successfully processed items in the last run), and a nullable execution-finished-at timestamp
(DateTimeOffset — the wall-clock time at which the last run fully completed). All three
fields MAY be null simultaneously. The two timestamps MUST advance independently based on
run outcome and MUST NOT be conflated. No other fields are part of the checkpoint value
contract.
(Previously: value contained exactly two fields — cursor and a single processed-at timestamp
with no distinction between max-item-timestamp and execution-finished-at.)

#### Scenario: Both timestamps stored and returned unchanged

- GIVEN cursor="delta-abc", maxProcessedAt=2026-06-18T10:00:00Z, executionFinishedAt=2026-06-18T10:05:00Z
- WHEN saved and then retrieved by identity
- THEN all three fields are returned with the exact same values

#### Scenario: Null fields are preserved independently

- GIVEN a checkpoint with cursor=null, maxProcessedAt=2026-06-18T10:00:00Z, executionFinishedAt=null
- WHEN saved and retrieved
- THEN cursor=null, maxProcessedAt=2026-06-18T10:00:00Z, and executionFinishedAt=null are returned exactly

#### Scenario: Both timestamps null with non-null cursor

- GIVEN a checkpoint with cursor="delta-v1", maxProcessedAt=null, executionFinishedAt=null
- WHEN saved and retrieved
- THEN cursor="delta-v1", maxProcessedAt=null, and executionFinishedAt=null are returned
