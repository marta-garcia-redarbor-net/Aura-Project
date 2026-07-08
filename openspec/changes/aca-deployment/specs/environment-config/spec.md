# Delta for Environment Config

## ADDED

### Req: ACA + Azure SQL Vars

Must add: `ConnectionStrings__AzureSql` (required), `AcaUi__BaseUrl`/`AcaApi__BaseUrl` (required), `ContainerRegistry__Host` (default ghcr.io), `ContainerRegistry__Username`/`__Password` (required), `DemoMode__Enabled` (default false).

#### Scenario: Missing AzureSql fails

- GIVEN `ConnectionStrings__AzureSql` absent
- WHEN API starts in ACA mode
- THEN startup fails

#### Scenario: Existing vars preserved

- GIVEN existing env vars unchanged
- WHEN app starts
- THEN all existing contract variables behave same
