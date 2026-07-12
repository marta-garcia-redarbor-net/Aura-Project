# Graph Configuration — Delta Spec

## Purpose

Enable Microsoft Graph connectors to flow live data by configuring credentials, adding required scopes, and extending status derivation to reflect actual Graph API connectivity (not just config presence).

## MODIFIED Requirements

### Requirement: No Real Credentials in `.env`

The `.env` file MUST support real Azure AD credentials for local development when the operator explicitly opts in. Placeholders MUST remain the default; real credentials MUST be documented and optional. The `.env` file MUST continue to be git-ignored.

#### Scenario: Placeholders remain default

- GIVEN the `.env` file exists at the project root
- WHEN its contents are inspected
- THEN all credential values are placeholders (e.g., `YOUR_CLIENT_ID`, `YOUR_TENANT_ID`)

#### Scenario: Real credentials accepted when provided

- GIVEN the operator has set `GraphConnector__TenantId` and `GraphConnector__ClientId` to real GUIDs in `.env`
- WHEN the application starts
- THEN the Graph connector status derives as `ValidConfig`
- AND a structured log at Information level confirms Graph connector is enabled with real credentials

#### Scenario: Enable flag must be true for live data

- GIVEN `GraphConnector__Enabled=false` in `.env`
- WHEN the connector status is evaluated
- THEN the derived state is `Disabled` regardless of other fields

---

### Requirement: Configuration Source v1

The system MUST bind Graph connector settings from backend appsettings files and environment variables. The `GraphClientFactory` default scopes MUST include `Calendars.Read` alongside existing `Mail.Read`, `Chat.Read`, and `User.Read`.

#### Scenario: Settings bound from appsettings

- GIVEN connector settings are declared in the backend appsettings file
- WHEN the application initializes
- THEN the settings are bound without error and are available for status derivation

#### Scenario: Environment variable shadows appsettings

- GIVEN an environment variable matches the connector settings key
- WHEN the application initializes
- THEN the environment variable value is used and the appsettings value is shadowed

#### Scenario: Calendars.Read scope present in defaults

- GIVEN the `GraphClientFactory` is constructed with default scopes
- WHEN the scopes list is inspected
- THEN `Calendars.Read` is present alongside `Mail.Read`, `Chat.Read`, and `User.Read`

---

## ADDED Requirements

### Requirement: Live Data Pipeline Enablement

The system MUST support an end-to-end data flow from Graph API through Infrastructure adapters, Application ports, API endpoints, and into UI consumption when `GraphConnector__Enabled=true` with valid credentials.

#### Scenario: Valid credentials enable full pipeline

- GIVEN `GraphConnector__Enabled=true`, `TenantId` and `ClientId` are present
- WHEN the worker executes a connector
- THEN a real Graph API call is attempted (not a mock or stub)
- AND the result contains real item counts from the Graph response

#### Scenario: Invalid credentials produce structured failure

- GIVEN `GraphConnector__Enabled=true` with valid-format but incorrect TenantId
- WHEN the connector executes
- THEN the result status is `failure`
- AND a structured log entry at Error level contains the Graph HTTP status code and connector name
- AND no exception propagates to the caller

#### Scenario: Disabled flag prevents any Graph call

- GIVEN `GraphConnector__Enabled=false`
- WHEN the worker executes a connector
- THEN no Graph API call is attempted
- AND the result status is `failure` with reason indicating connector is disabled

---

## ADDED Requirements

### Requirement: Production Required Configuration for Real-User Sync

For real-user Graph sync to operate in production alongside demo mode, all of the
following MUST be present and non-empty: `GraphConnector__Enabled=true`,
`GraphConnector__TenantId` (valid GUID format), `GraphConnector__ClientId` (valid GUID
format), and the Azure app registration MUST include delegated scopes `Mail.Read` and
`User.Read`. When all fields are present and valid, the connector status MUST be
`ValidConfig` and real Graph token acquisition MUST be permitted.

#### Scenario: All required config present enables real sync

- GIVEN `GraphConnector__Enabled=true`, a valid `TenantId`, and a valid `ClientId` are configured
- WHEN connector status is derived
- THEN the status is `ValidConfig`
- AND real Graph token acquisition is permitted

#### Scenario: Demo mode and real sync coexist in one deployment

- GIVEN demo authentication is enabled and `GraphConnector__Enabled=true` with valid credentials
- WHEN the application starts
- THEN demo sessions use the demo auth pipeline
- AND real authenticated sessions use the real Graph pipeline
- AND neither pipeline interferes with the other

#### Scenario: Missing Mail.Read scope causes permission failure on sync

- GIVEN the Azure app registration does not include `Mail.Read`
- WHEN the worker attempts Outlook sync for a real user
- THEN the Graph call returns a permission error
- AND the result status is `failure` with reason referencing scope absence
- AND no mail items are returned

---

### Requirement: Configuration Gap Safe Status

When any required Graph configuration field is absent or empty in production, the system
MUST derive connector status as `Disabled`. The system MUST NOT throw an unhandled
exception or enter a re-authentication loop. A structured Warning log identifying the
specific missing field MUST be emitted when the gap is detected.

#### Scenario: Missing TenantId yields Disabled status with warning

- GIVEN `GraphConnector__TenantId` is absent or empty
- WHEN connector status is derived
- THEN the status is `Disabled`
- AND a Warning log identifies `TenantId` as the missing field
- AND no exception propagates to the caller

#### Scenario: Missing ClientId yields Disabled status with warning

- GIVEN `GraphConnector__ClientId` is absent or empty
- WHEN connector status is derived
- THEN the status is `Disabled`
- AND a Warning log identifies `ClientId` as the missing field

#### Scenario: Explicit Enabled=false yields Disabled without a gap warning

- GIVEN `GraphConnector__Enabled=false` with otherwise valid TenantId and ClientId
- WHEN connector status is derived
- THEN the status is `Disabled`
- AND no Warning log for missing configuration fields is emitted

#### Scenario: Valid config emits no gap warning

- GIVEN all required fields are present and non-empty with Enabled=true
- WHEN connector status is derived
- THEN no Warning log for missing configuration is emitted
- AND the status is `ValidConfig`
