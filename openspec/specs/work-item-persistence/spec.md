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
| Pending ExternalId Snapshot | Store MUST expose pending `ExternalId` values for a given source as an `IReadOnlySet<string>` | MUST |
| Batch Auto-Completion by ExternalId | Store MUST mark provided pending source-scoped `ExternalId` values as `Completed` and ignore missing ids | MUST |

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

### Requirement: Store Implementation

Provide ADO.NET SQLite AND EF Core/Azure SQL. DI-registerable, types in Infra.
(Previously: single ADO.NET)

#### Scenario: Both persist

- GIVEN canonical `WorkItem`
- WHEN stored via either
- THEN item persists, success

#### Scenario: Architecture guard

- GIVEN store deps inspected
- WHEN arch tests run
- THEN no type leaks outside Infra

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

### Requirement: Pending ExternalId Snapshot

`IWorkItemStore` MUST expose `GetPendingExternalIdsAsync(WorkItemSourceType source,
CancellationToken ct)` returning an `IReadOnlySet<string>` containing only the
`ExternalId` values for items in status `"Pending"` for the requested source.
Implementations MUST use the persisted TEXT status values (`"Pending"`, `"Completed"`),
not numeric enum storage assumptions.

#### Scenario: Pending external id snapshot returns only pending items for the source

- GIVEN source-scoped items `A` and `B` in `Pending` and item `C` in `Completed`
- WHEN `GetPendingExternalIdsAsync` is invoked for that source
- THEN the result contains `A` and `B`
- AND the result excludes `C`

#### Scenario: No pending items returns an empty set

- GIVEN no work items for the source are currently `Pending`
- WHEN `GetPendingExternalIdsAsync` is invoked
- THEN an empty `IReadOnlySet<string>` is returned

#### Scenario: SQLite filters by text status and source

- GIVEN the SQLite implementation executes `GetPendingExternalIdsAsync`
- WHEN it queries the backing table
- THEN it filters `Status = 'Pending'`
- AND it also filters by the requested source

#### Scenario: InMemory implementation filters pending items by source

- GIVEN the in-memory store contains mixed statuses and mixed sources
- WHEN `GetPendingExternalIdsAsync` is invoked
- THEN it returns only `Pending` items for the requested source

### Requirement: Batch Auto-Completion by ExternalId

`IWorkItemStore` MUST expose `MarkCompletedAsync(IReadOnlySet<string> externalIds,
WorkItemSourceType source, CancellationToken ct)` to mark matching pending items as
`"Completed"` for the given source. Missing external ids MUST be ignored without error.

#### Scenario: Batch completion updates only requested ids

- GIVEN pending items `A`, `B`, and `C` for the same source
- WHEN `MarkCompletedAsync` is invoked for `A` and `C`
- THEN `A` and `C` transition to `Completed`
- AND `B` remains `Pending`

#### Scenario: Missing external ids are ignored without failure

- GIVEN pending item `A` exists for the source
- WHEN `MarkCompletedAsync` is invoked for `A` and `nonexistent`
- THEN `A` transitions to `Completed`
- AND no error is thrown for `nonexistent`

#### Scenario: Batch completion preserves source isolation

- GIVEN one source has pending item `A` and another source also has an item with a different external id
- WHEN `MarkCompletedAsync` is invoked for the first source only
- THEN only matching items for that source are updated
- AND other sources remain unchanged

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

---

### Requirement: EF Core Store

EF Core `IWorkItemStore` for Azure SQL alongside SQLite. `StoreProvider` selects.

#### Scenario: EF Core persists

- GIVEN toggle `AzureSql`
- WHEN `WorkItem` stored
- THEN persists to Azure SQL

#### Scenario: SQLite toggle

- GIVEN toggle `Sqlite`
- WHEN store resolved
- THEN ADO.NET injected
