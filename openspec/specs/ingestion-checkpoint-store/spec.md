# Ingestion Checkpoint Store Specification

## Purpose

Provider-neutral Application-layer contract for persisting and retrieving ingestion
checkpoints, enabling repeated incremental sync runs without re-pulling previously
processed data.

## Requirements

| Requirement | Strength |
|---|---|
| Checkpoint Identity | MUST |
| Checkpoint Value Shape | MUST |
| Checkpoint Read-Write Operations | MUST |
| First-Run Bounded Initial Window | MUST |
| Provider Isolation | MUST NOT (leak SDK types) |

---

### Requirement: Checkpoint Identity

A checkpoint MUST be uniquely identified by the composite key (connector, source, tenant).
All three components MUST be non-null, non-empty strings. No two checkpoints with the same
(connector, source, tenant) triple MAY coexist; a save on an existing identity replaces
the previous value.

#### Scenario: Independent checkpoints per distinct identity

- GIVEN checkpoints exist for (teams, messages, acme) and (teams, calendar, acme)
- WHEN each is retrieved by its own identity
- THEN each returns its own independent value without cross-contamination

#### Scenario: Save replaces checkpoint on same identity

- GIVEN a checkpoint exists for (github, pull-requests, acme) with cursor="delta-v1"
- WHEN a new checkpoint is saved with the same identity and cursor="delta-v2"
- THEN retrieval returns cursor="delta-v2" and the previous value is no longer accessible

---

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

---

### Requirement: Checkpoint Read-Write Operations

The contract MUST expose:
- A **save** operation that accepts a checkpoint identity and value, writing or replacing the checkpoint.
- A **get** operation that accepts an identity and returns the stored checkpoint value, or **null** if no checkpoint exists for that identity.

#### Scenario: Get returns stored checkpoint

- GIVEN a checkpoint was saved for (outlook, inbox, acme)
- WHEN Get is called with the same identity
- THEN the stored checkpoint value is returned

#### Scenario: Get returns null for unknown identity

- GIVEN no checkpoint has been saved for (outlook, inbox, acme)
- WHEN Get is called with that identity
- THEN null is returned

---

### Requirement: First-Run Bounded Initial Window

When Get returns null (no prior checkpoint), callers MUST interpret the absence as a
signal to bound the initial fetch to **UTC today only**: start = UTC today 00:00:00,
end = UTC now. This bound is a caller responsibility enforced at the call site and
MUST NOT be stored as a field in the checkpoint value.

#### Scenario: No checkpoint → caller applies today-only window

- GIVEN no checkpoint exists for a given identity
- WHEN an ingestion caller receives null from Get
- THEN the caller constrains the data pull to the UTC-today window, not all historical data

#### Scenario: Existing checkpoint → today-only window is not applied

- GIVEN Get returns a checkpoint with a non-null cursor for a given identity
- WHEN the caller processes the result
- THEN the caller uses the stored cursor/timestamp and does not apply the today-only bound

---

### Requirement: Provider Isolation

The port interface and checkpoint model MUST NOT expose any type from an external SDK,
provider-specific library, or Infrastructure-layer namespace. All types in the contract
MUST belong to Aura.Application or .NET BCL.

#### Scenario: Contract references only Application or BCL types

- GIVEN the checkpoint-store port interface and its checkpoint model are inspected
- WHEN all parameter types, return types, and property types are enumerated
- THEN every type belongs to Aura.Application namespaces or .NET BCL (string, DateTimeOffset, etc.)
