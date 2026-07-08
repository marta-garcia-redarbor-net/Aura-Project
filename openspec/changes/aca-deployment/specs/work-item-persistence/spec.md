# Delta for Work Item Persistence

## ADDED

### Req: EF Core Store

EF Core `IWorkItemStore` for Azure SQL alongside SQLite. `StoreProvider` selects.

#### Scenario: EF Core persists

- GIVEN toggle `AzureSql`
- WHEN `WorkItem` stored
- THEN persists to Azure SQL

#### Scenario: SQLite toggle

- GIVEN toggle `Sqlite`
- WHEN store resolved
- THEN ADO.NET injected

## MODIFIED

### Req: Store Implementation

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
