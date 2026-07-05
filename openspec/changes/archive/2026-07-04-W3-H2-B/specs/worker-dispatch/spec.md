# Worker and Dispatch Specification

## Purpose

Define the contract for the notification worker and SignalR dispatcher to propagate persisted audit information from the outbox to the frontend without breaking existing consumers.

## Requirements

| Requirement | Constraint | Strength |
|---|---|---|
| Worker Deserializes Persisted Verdict | Stop synthesizing fake verdicts; fall back for null fields | MUST |
| Backward-Compatible Dispatch | Add audit fields to payload; existing fields unchanged | MUST |

---

### Requirement: Worker Deserializes Persisted Verdict

The `WorkItemNotificationWorker` MUST deserialize the persisted verdict fields from each `NotificationOutboxEntry` and construct an `InterruptionVerdict` using real persisted data. When verdict fields are null (pre-migration rows), the worker MUST fall back to synthesizing a default verdict with the `TriggerRule`.

#### Scenario: Persisted verdict used when available

- GIVEN a `NotificationOutboxEntry` with non-null `Decision`, `Explanation`, `TargetUserId`, and `RuleResults`
- WHEN the worker processes the entry
- THEN the `InterruptionVerdict` passed to `DispatchAsync` contains the persisted values
- AND `Decision` matches the persisted value (not hardcoded InterruptNow)

#### Scenario: Fallback to synthesized verdict for old rows

- GIVEN a `NotificationOutboxEntry` with null verdict fields
- WHEN the worker processes the entry
- THEN the worker constructs a default `InterruptionVerdict` using `TriggerRule`
- AND no deserialization error occurs

#### Scenario: Null RuleResults produces empty report

- GIVEN a `NotificationOutboxEntry` with `RuleResults = null`
- WHEN the worker deserializes the verdict
- THEN the `EvaluationReport` in the verdict is an empty report (zero results)
- AND the worker does not fail

---

### Requirement: Backward-Compatible Dispatch

The `SignalRWorkItemNotificationDispatcher` MUST add audit fields (`Explanation`, `Decision`, `TargetUserId`, `RuleResults`) to the `UrgentWorkItem` SignalR event payload. Existing fields (`Id`, `Title`, `SourceType`, `Priority`, `TriggerRule`, `Reason`) MUST remain in the same shape. New fields MUST be optional — existing frontend consumers MUST continue to work unchanged.

#### Scenario: Dispatch includes audit fields

- GIVEN a notification entry with all verdict fields populated
- WHEN `DispatchAsync` sends the `UrgentWorkItem` event
- THEN the payload contains `Explanation`, `Decision`, `TargetUserId`, `RuleResults`
- AND all existing payload fields (`Id`, `Title`, `SourceType`, `Priority`, `TriggerRule`, `Reason`) are present with their current values

#### Scenario: Dispatch without audit fields

- GIVEN a notification entry with null verdict fields
- WHEN `DispatchAsync` sends the `UrgentWorkItem` event
- THEN the new audit fields are absent or null in the payload
- AND existing fields remain unchanged
- AND any frontend consumer that does not know about audit fields receives the same shape as before
