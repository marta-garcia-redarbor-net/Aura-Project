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
| Canonical Execution Result | identity + item count + status; failure MUST include reason | MUST |
| Telemetry Emission | Trace span + item-count metric + log; share one correlation ID | MUST |
| Clean Architecture Boundary | No SDK types above Infrastructure; enforced by arch tests | MUST NOT violate |
| Checkpoint Read-Only Integration | Read checkpoint to bound fetch window; MUST NOT write (deferred W2-H2-T3) | SHOULD |

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

### Requirement: Canonical Execution Result

#### Scenario: Success result contains required fields

- GIVEN execution completes with 5 items
- WHEN the result is inspected
- THEN connector identity, item count = 5, and status = success are present

#### Scenario: Failure result contains reason

- GIVEN execution fails
- WHEN the result is inspected
- THEN status = failure and a non-empty reason string are present

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

#### Scenario: Existing checkpoint bounds fetch window

- GIVEN a checkpoint exists for the connector identity
- WHEN the use case executes
- THEN the checkpoint bounds the fetch window and no write or mutation occurs

#### Scenario: Absent checkpoint → today-only window, no write

- GIVEN no checkpoint exists for the connector identity
- WHEN the use case executes
- THEN the fetch window defaults to UTC today and no checkpoint is written
