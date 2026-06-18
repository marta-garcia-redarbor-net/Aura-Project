# Delta for Plugin Kernel

## MODIFIED Requirements

### Requirement: WorkItem State Encapsulation

The `WorkItem` entity MUST encapsulate its own state and MUST NOT expose public
setters for state mutations. It SHALL reside entirely in the `Domain` layer without
external dependencies. Construction MUST satisfy the full mandatory-field contract
defined in the `work-item-contract` specification.

(Previously: construction only required non-empty `title` and `source`)

#### Scenario: Valid state transition

- GIVEN a `WorkItem` constructed with the full mandatory-field contract
- WHEN a valid domain operation is invoked to transition its state
- THEN the `WorkItem` updates its internal state successfully
- AND exposes the new state via public getters

#### Scenario: Invalid state transition

- GIVEN a `WorkItem` constructed with the full mandatory-field contract
- WHEN an invalid domain operation is attempted
- THEN the `WorkItem` rejects the transition and maintains its current state
