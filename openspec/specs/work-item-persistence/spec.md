# Work Item Persistence Specification

## Purpose

Defines the provider-neutral Application-layer port and the Infrastructure store contract
for durably persisting a canonical `WorkItem`. The Application layer owns the port interface;
all store-technology dependencies are confined to `Aura.Infrastructure`.

## Requirements

| Requirement | Constraint | Strength |
|---|---|---|
| Work Item Persistence Port | Provider-neutral; port contract MUST use only `Aura.Application` or BCL types | MUST |
| Infrastructure Store Implementation | Implements the port; all store dependencies confined to `Aura.Infrastructure` | MUST |
| Typed Persistence Result | Port returns success or failure; failure MUST include non-empty reason; no exceptions propagate | MUST |

---

### Requirement: Work Item Persistence Port

The Application layer MUST define a provider-neutral port for persisting a canonical
`WorkItem`. The port interface MUST contain only `Aura.Application` or BCL types.
No Infrastructure, SDK, or store-technology type MAY appear in the port contract.

#### Scenario: Port accepts canonical WorkItem and returns success

- GIVEN a valid canonical `WorkItem`
- WHEN the persistence port is invoked
- THEN a success result is returned with no error information

#### Scenario: Port interface contains no Infrastructure references

- GIVEN the port interface type dependencies are enumerated
- WHEN all types are listed
- THEN no `Aura.Infrastructure` or external SDK type is found

---

### Requirement: Infrastructure Store Implementation

The Infrastructure layer MUST provide a concrete implementation of the Work Item persistence
port. All store-technology dependencies MUST be confined to `Aura.Infrastructure`. The
implementation MUST be registerable via the DI container without exposing store-specific
types to any other layer.

#### Scenario: Store persists the WorkItem and returns success

- GIVEN a canonical `WorkItem` is provided to the store implementation
- WHEN the port is invoked through the registered DI binding
- THEN the `WorkItem` is persisted and a success result is returned

#### Scenario: Architecture test rejects store-technology leakage

- GIVEN the store implementation type dependencies are inspected
- WHEN architecture tests run
- THEN no store-specific dependency is found outside `Aura.Infrastructure`

---

### Requirement: Typed Persistence Result

The port MUST return a typed result indicating success or failure. A failure result MUST
include a non-empty reason string. No exception MUST propagate to the caller for expected
persistence failures; errors MUST be captured in the typed result.

#### Scenario: Persistence failure returns typed result with reason

- GIVEN the store encounters an error persisting a `WorkItem`
- WHEN the port is invoked
- THEN a failure result with a non-empty reason string is returned
- AND no exception propagates to the caller

#### Scenario: Success result carries no error information

- GIVEN the store successfully persists a `WorkItem`
- WHEN the result is inspected
- THEN the result status is success and no reason field is populated
