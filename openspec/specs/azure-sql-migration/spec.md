# Azure SQL Migration

SQLite ADO.NET → EF Core + Azure SQL, store-by-store, ports unchanged.

| Req | Str |
|---|---|
| Each store migrated independently under TDD | MUST |
| SQLite stays as fallback | MUST |
| EF Core maps to existing table schema | MUST |
| Port interfaces unchanged | MUST |

#### Scenario: Toggle selects provider

- GIVEN `StoreProvider:AzureSql`
- WHEN port invoked
- THEN EF Core responds

#### Scenario: Schema preserved

- GIVEN EF Core mappings
- WHEN compared to SQLite
- THEN columns match
