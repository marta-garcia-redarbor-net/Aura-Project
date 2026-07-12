# Delta for Graph Configuration

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
