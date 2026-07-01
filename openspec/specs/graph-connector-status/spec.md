# Graph Connector Status Specification

## Purpose

Derive and expose the Microsoft Graph connector's configuration readiness as a four-state
read-only status. Configuration is bound from appsettings and environment variables (v1).
No real Graph SDK connection is made. The UI renders the status read-only.

## Requirements

### Requirement: Config Status Derivation

The system MUST derive connector status from configuration presence using these ordered rules
(first match wins):

| Priority | State | Rule |
|----------|-------|------|
| 1 | Disabled | Enable flag = false — overrides all other checks |
| 2 | MissingConfig | Enabled + TenantId absent + ClientId absent |
| 3 | PartialConfig | Enabled + some but not all of (TenantId, ClientId) present |
| 4 | ValidConfig | Enabled + TenantId + ClientId present |

#### Scenario: Disabled takes precedence

- GIVEN the enable flag is false, regardless of other fields being present or absent
- WHEN the connector status is evaluated
- THEN the derived state is Disabled

#### Scenario: MissingConfig when no identifiers present

- GIVEN the enable flag is true, TenantId is absent, and ClientId is absent
- WHEN the connector status is evaluated
- THEN the derived state is MissingConfig

#### Scenario: PartialConfig when some required fields are present

- GIVEN the enable flag is true AND exactly one of (TenantId, ClientId) is present
- WHEN the connector status is evaluated
- THEN the derived state is PartialConfig

#### Scenario: ValidConfig with delegated auth fields only

- GIVEN the enable flag is true, TenantId is present, and ClientId is present
- WHEN the connector status is evaluated
- THEN the derived state is ValidConfig
- AND no ClientSecret or credentials block is required

#### Scenario: ClientSecret presence is ignored

- GIVEN the enable flag is true, TenantId is present, ClientId is present, and ClientSecret is also present
- WHEN the connector status is evaluated
- THEN the derived state is ValidConfig
- AND the ClientSecret value does not affect the status derivation

---

### Requirement: Configuration Source v1

The system MUST bind Graph connector settings exclusively from backend appsettings files and
environment variables. Secret stores, databases, and external services MUST NOT be used as
configuration sources in v1.

#### Scenario: Settings bound from appsettings

- GIVEN connector settings are declared in the backend appsettings file
- WHEN the application initializes
- THEN the settings are bound without error and are available for status derivation

#### Scenario: Environment variable shadows appsettings

- GIVEN an environment variable matches the connector settings key
- WHEN the application initializes
- THEN the environment variable value is used and the appsettings value is shadowed

---

### Requirement: Status API Endpoint

The API MUST expose a GET-only endpoint returning a DTO with the current connector state.
Write verbs (POST, PUT, PATCH, DELETE) MUST NOT be accepted.

#### Scenario: GET returns current state

- GIVEN any valid configuration scenario
- WHEN a client sends GET to the connector-status endpoint
- THEN the response is HTTP 200 with a DTO containing the derived state value

#### Scenario: Write verbs rejected

- GIVEN the connector-status endpoint is available
- WHEN a client sends POST, PUT, PATCH, or DELETE to the endpoint
- THEN the response is HTTP 405 Method Not Allowed

---

### Requirement: Read-Only Status UI Panel

The UI MUST render a read-only panel displaying the current connector state. Each of the four
states (Disabled, MissingConfig, PartialConfig, ValidConfig) MUST have a distinct visual
representation. The panel MUST NOT expose any edit or submit affordance.

#### Scenario: Disabled state renders correctly

- GIVEN the API returns state = Disabled
- WHEN the status panel loads
- THEN the Disabled indicator is shown and no edit controls are present

#### Scenario: MissingConfig state renders correctly

- GIVEN the API returns state = MissingConfig
- WHEN the status panel loads
- THEN the MissingConfig indicator is shown and no edit controls are present

#### Scenario: PartialConfig state renders correctly

- GIVEN the API returns state = PartialConfig
- WHEN the status panel loads
- THEN the PartialConfig indicator is shown and no edit controls are present

#### Scenario: ValidConfig state renders correctly

- GIVEN the API returns state = ValidConfig
- WHEN the status panel loads
- THEN the ValidConfig indicator is shown and no edit controls are present

---

### Requirement: Architecture Isolation

No Graph SDK or provider-specific type SHALL appear in the Application or UI layers.
The Infrastructure layer MUST be the sole layer that references SDK or provider types.

#### Scenario: Architecture tests confirm isolation

- GIVEN the graph-connector-status capability is fully implemented
- WHEN the architecture tests run
- THEN no Graph SDK type is found in the Application or UI project namespaces

---

### Requirement: lastMessageReadDateTime Mapping

`GraphTeamsSourceProvider` MUST map `lastMessageReadDateTime` from the Graph chat response to `TeamsMessageDto.LastMessageReadAt`. When `lastMessageReadDateTime` is null (e.g., chat never opened), the DTO field SHALL be null. `TeamsMessageDto` MUST also include `LastMessageAt` (mapped from `lastMessageDateTime`) and `UnreadCount` (mapped from `unreadCount`).

#### Scenario: lastMessageReadDateTime present maps to DTO

- GIVEN a Graph chat response with `lastMessageReadDateTime = "2026-06-30T14:00:00Z"`
- WHEN `GraphTeamsSourceProvider` maps the response
- THEN `TeamsMessageDto.LastMessageReadAt` is set to `2026-06-30T14:00:00Z`

#### Scenario: null lastMessageReadDateTime maps to null

- GIVEN a Graph chat response where `lastMessageReadDateTime` is null
- WHEN `GraphTeamsSourceProvider` maps the response
- THEN `TeamsMessageDto.LastMessageReadAt` is null

#### Scenario: lastMessageDateTime maps to LastMessageAt

- GIVEN a Graph chat response with `lastMessageDateTime = "2026-06-30T15:00:00Z"`
- WHEN `GraphTeamsSourceProvider` maps the response
- THEN `TeamsMessageDto.LastMessageAt` is set to `2026-06-30T15:00:00Z`

#### Scenario: unreadCount maps to UnreadCount

- GIVEN a Graph chat response with `unreadCount = 3`
- WHEN `GraphTeamsSourceProvider` maps the response
- THEN `TeamsMessageDto.UnreadCount` equals `3`
