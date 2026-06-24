# Delta for API Authentication

## ADDED Requirements

### Requirement: MSAL Token Acquisition

The system MUST provide an `MsalTokenAcquisitionService` implementing `ITokenAcquisitionService` that acquires access tokens using MSAL.Net with interactive browser flow (`AcquireTokenInteractive`). The service MUST use the same Entra ID App Registration as the Graph API connector with an additional `MeetingAlerts` scope. When MSAL configuration (ClientId, TenantId) is absent from `Program.cs`, the system SHOULD fall back to mock JWT token generation for local development.

#### Scenario: MSAL token acquired with config present

- GIVEN MSAL configuration (ClientId, TenantId) is present in `Program.cs`
- WHEN `MsalTokenAcquisitionService.AcquireTokenAsync` is called
- THEN a valid access token for the `MeetingAlerts` scope is returned

#### Scenario: Fallback when MSAL config absent

- GIVEN MSAL configuration is absent from `Program.cs`
- WHEN the token acquisition service is resolved
- THEN a mock JWT token is returned for development use
- AND a warning is logged indicating dev-only fallback mode

### Requirement: SignalR Hub Authentication

The system MUST authenticate the SignalR hub connection using the token acquired by `ITokenAcquisitionService`. The hub connection MUST include the access token in the negotiation request. The same Entra ID App Registration used for Graph MUST be reused with the `MeetingAlerts` scope.

#### Scenario: SignalR connection authenticated via MSAL

- GIVEN MSAL token acquisition is configured
- WHEN the Blazor UI establishes a SignalR connection to `MeetingAlertHub`
- THEN the access token is included in the hub negotiation
- AND the hub authorizes the connection

#### Scenario: SignalR connection uses mock JWT in dev

- GIVEN MSAL configuration is absent
- WHEN the Blazor UI establishes a SignalR connection
- THEN a mock JWT is used for hub authentication
- AND the connection succeeds in development mode
