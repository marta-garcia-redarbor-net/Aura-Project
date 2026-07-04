# Notification Outbox Audit Specification

## Purpose

Define the durable audit trail that persists structured `InterruptionVerdict` data through the notification outbox so downstream consumers can reconstruct why a notification was sent.

## Requirements

| Requirement | Constraint | Strength |
|---|---|---|
| Domain Entity Verdict Fields | All new fields nullable on `NotificationOutboxEntry` | MUST |
| SQLite Schema | Nullable TEXT columns, backward-compatible reads | MUST |
| RuleResult JSON Round-Trip | Serialize `EvaluationReport` to JSON on write, deserialize on read | MUST |

---

### Requirement: Domain Entity Verdict Fields

`NotificationOutboxEntry` MUST carry nullable audit fields: `Explanation` (string?), `Decision` (string?), `TargetUserId` (string?), and `RuleResults` (string? — JSON-serialized `EvaluationReport`). A constructor overload accepting these fields alongside existing parameters MUST exist. Entries without verdict data (pre-migration or alternative paths) MUST remain valid.

#### Scenario: Entry created with full verdict

- GIVEN an `InterruptionVerdict` with Decision, Explanation, TargetUserId, and a Report containing 3 rule results
- WHEN the verdict-aware constructor creates the entry
- THEN all verdict fields are populated on the entity

#### Scenario: Entry created without verdict fields

- GIVEN the original constructor is used (no verdict data available)
- WHEN the entry is constructed
- THEN `Explanation`, `Decision`, `TargetUserId`, and `RuleResults` are all null
- AND the existing behavior (TriggerRule only) is preserved

---

### Requirement: SQLite Schema

`InitializeSchema` MUST add nullable TEXT columns: `Explanation`, `Decision`, `TargetUserId`, `RuleResults`. `EnqueueAsync` MUST write these columns with DBNull when verdict data is absent. `ReadEntryFromReader` MUST read them and return null when the column is NULL. Pre-migration rows with NULL in audit columns MUST NOT cause read failures.

#### Scenario: New row persists full verdict

- GIVEN a `NotificationOutboxEntry` with all verdict fields populated
- WHEN `EnqueueAsync` executes the INSERT
- THEN each audit column stores non-null text content

#### Scenario: Pre-migration row reads safely

- GIVEN a row persisted before the schema migration (all audit columns are NULL)
- WHEN `GetPendingAsync` reads that row
- THEN the returned `NotificationOutboxEntry` has null verdict fields
- AND no exception or column-missing error is thrown

#### Scenario: New row without verdict writes NULL

- GIVEN an entry created via the original constructor (verdict fields are null)
- WHEN `EnqueueAsync` executes the INSERT
- THEN the audit columns are written as DBNull

---

### Requirement: RuleResult JSON Round-Trip

The `RuleResults` field MUST serialize `EvaluationReport` to a JSON string on write and deserialize back to the report structure on read. Every `RuleResult` in the report — `RuleName`, `Matched`, `Score`, `Confidence`, `Reason` — MUST survive the round-trip with identical values.

#### Scenario: Single rule result round-trips correctly

- GIVEN an `EvaluationReport` with one `RuleResult` (RuleName="priority_check", Matched=true, Score=8.0, Confidence=0.9, Reason="High urgency")
- WHEN the report is serialized to JSON, stored, read back, and deserialized
- THEN the recovered `RuleResult` has identical field values

#### Scenario: Empty report round-trips to empty list

- GIVEN an `EvaluationReport` with zero results
- WHEN serialized and deserialized
- THEN the recovered report has an empty `Results` list

#### Scenario: Null RuleResults field is handled gracefully

- GIVEN a `NotificationOutboxEntry` with `RuleResults = null`
- WHEN the worker reads the entry
- THEN no deserialization is attempted
- AND the report defaults to an empty `EvaluationReport`
