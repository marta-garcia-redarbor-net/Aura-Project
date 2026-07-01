# Delta for work-item-persistence

## ADDED Requirements

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
