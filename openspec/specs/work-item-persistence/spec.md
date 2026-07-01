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

---

### Requirement: FindByExternalIdAsync on IWorkItemStore

`IWorkItemStore` MUST expose a `FindByExternalIdAsync(string externalId, CancellationToken ct)` method that returns the `WorkItem` matching the given `ExternalId`, or `null` if no match exists. Both `InMemoryWorkItemStore` and `SqliteWorkItemStore` MUST implement this method.

#### Scenario: Existing ExternalId returns WorkItem

- GIVEN a `WorkItem` with `ExternalId = "19:abc@thread.v2"` is persisted
- WHEN `FindByExternalIdAsync("19:abc@thread.v2")` is called
- THEN the matching `WorkItem` is returned

#### Scenario: Non-existent ExternalId returns null

- GIVEN no `WorkItem` with `ExternalId = "nonexistent"` exists
- WHEN `FindByExternalIdAsync("nonexistent")` is called
- THEN `null` is returned

### Requirement: IWorkItemReader Status Filter Overload

`IWorkItemReader` MUST expose an overload that accepts a `WorkItemStatus` filter and returns only items matching that status. The existing unfiltered overload SHALL remain unchanged.

#### Scenario: Status filter returns matching items

- GIVEN WorkItems with mixed statuses (Pending, Completed)
- WHEN the reader is queried with `Status = Pending`
- THEN only items with `Status = Pending` are returned

#### Scenario: Status filter with no matches returns empty

- GIVEN no WorkItems with `Status = Processing`
- WHEN the reader is queried with `Status = Processing`
- THEN an empty collection is returned

### Requirement: InMemoryWorkItemStore Dedup by ExternalId

`InMemoryWorkItemStore` MUST key its internal store by `string ExternalId` (not `Guid Id`). When persisting a WorkItem whose `ExternalId` already exists, the store MUST update metadata fields (title, `Metadata` dictionary, `capturedAtUtc`) but MUST retain the original `Priority`. The store MUST NOT create duplicate entries for the same `ExternalId`.

#### Scenario: First save creates entry

- GIVEN no WorkItem with `ExternalId = "19:abc@thread.v2"` exists
- WHEN the store persists a WorkItem with that ExternalId
- THEN a new entry is created and success is returned

#### Scenario: Re-save updates metadata, keeps priority

- GIVEN a WorkItem with `ExternalId = "19:abc@thread.v2"` exists with `Priority = High` and `Metadata["title"] = "old"`
- WHEN the store persists a WorkItem with the same ExternalId, `Priority = Low`, and `Metadata["title"] = "new"`
- THEN the stored entry retains `Priority = High`
- AND `Metadata["title"]` is updated to `"new"`

#### Scenario: Different ExternalId creates separate entry

- GIVEN one WorkItem with `ExternalId = "19:abc@thread.v2"` exists
- WHEN the store persists another with `ExternalId = "19:def@thread.v2"`
- THEN both entries exist independently
