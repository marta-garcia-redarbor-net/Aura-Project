# Delta for Morning Summary Scheduling

## ADDED Requirements

### Requirement: Composition After Emission

When the scheduler determines a Morning Summary is due and the emission guard is passed, the worker MUST invoke `IMorningSummaryComposer.ComposeAsync()` after `MarkEmittedAsync()` completes successfully. Composition failure MUST NOT break the worker loop or prevent future scheduling cycles.

#### Scenario: Composer called when due

- GIVEN the scheduler determines `isDue = true` for a user
- WHEN `MarkEmittedAsync()` completes successfully
- THEN `ComposeAsync()` is invoked for that user
- AND composition success or failure is logged

#### Scenario: Composition failure does not break loop

- GIVEN the scheduler determines `isDue = true` for a user
- WHEN `ComposeAsync()` throws an exception
- THEN the exception is caught and logged at Error level
- AND the worker loop continues to the next polling cycle

#### Scenario: Composer not called when not due

- GIVEN the scheduler determines `isDue = false`
- WHEN the worker completes the evaluation
- THEN `ComposeAsync()` is NOT invoked
- AND no composition log is emitted

### Requirement: Composer Dependency Injection

The worker MUST accept `IMorningSummaryComposer` as a constructor dependency. The dependency MUST be injected alongside the existing `IMorningSummaryScheduler` and `IMorningSummaryEmissionStore`.

#### Scenario: Worker resolves with composer

- GIVEN the DI container registers `IMorningSummaryScheduler`, `IMorningSummaryEmissionStore`, and `IMorningSummaryComposer`
- WHEN the worker is resolved from the container
- THEN all three dependencies are injected successfully
