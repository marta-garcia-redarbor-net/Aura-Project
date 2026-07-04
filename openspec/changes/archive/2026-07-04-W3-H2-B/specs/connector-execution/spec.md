# Delta for Connector Execution

## ADDED Requirements

### Requirement: Full Verdict Persistence in EvaluateAndEnqueueAsync

When `EvaluateAndEnqueueAsync` enqueues a notification for an `InterruptNow` decision, it MUST persist the full `InterruptionVerdict` — `Decision`, `Explanation`, `TargetUserId`, `TriggerRule`, and the JSON-serialized `EvaluationReport` — into the `NotificationOutboxEntry` via the verdict-aware constructor overload. This extends the current behavior that persists only `TriggerRule`.

#### Scenario: InterruptNow persists full verdict

- GIVEN an `InterruptionVerdict` with Decision=InterruptNow, Explanation="Urgent action required", TargetUserId="user-abc", and a Report with 2 rule results
- WHEN `EvaluateAndEnqueueAsync` creates and enqueues the outbox entry
- THEN the `NotificationOutboxEntry` is created with all verdict fields populated
- AND `RuleResults` stores the serialized JSON of the `EvaluationReport`

#### Scenario: Queue decision does not create outbox entry

- GIVEN an `InterruptionVerdict` with Decision=Queue
- WHEN `EvaluateAndEnqueueAsync` processes it
- THEN no outbox entry is enqueued
- AND no verdict data is persisted

#### Scenario: Defer decision does not create outbox entry

- GIVEN an `InterruptionVerdict` with Decision=Defer
- WHEN `EvaluateAndEnqueueAsync` processes it
- THEN no outbox entry is enqueued
- AND no verdict data is persisted

#### Scenario: Evaluation failure does not block ingestion

- GIVEN the engine throws an exception during evaluation
- WHEN `EvaluateAndEnqueueAsync` catches it
- THEN the exception is swallowed and logged
- AND ingestion continues for the next item
