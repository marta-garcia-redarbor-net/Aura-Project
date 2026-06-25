# API Authentication Specification

## Purpose

Defines how users authenticate with the Aura API and how their identity context is established across layers without coupling to external identity providers during local development.

## Requirements

### Requirement: Mock Login Generation

The system MUST provide an endpoint to generate a local, symmetric JWT for development purposes.

#### Scenario: Successful Mock Login

- GIVEN the application is running in a development environment
- WHEN a client sends a POST request to `/api/auth/mock-login`
- THEN the API returns a valid JWT token containing basic mock user claims

### Requirement: API Authorization Enforcement

The system MUST enforce authentication on protected endpoints using JWT Bearer token validation.

#### Scenario: Access without token

- GIVEN a protected API endpoint
- WHEN a client sends a request without an Authorization header
- THEN the API rejects the request with a 401 Unauthorized status

#### Scenario: Access with valid mock token

- GIVEN a valid JWT token obtained from the mock login endpoint
- WHEN a client sends a request to a protected API endpoint with the token as a Bearer token
- THEN the API accepts the request and processes it successfully

### Requirement: Identity Decoupling

The system MUST represent the current authenticated user inside the Application layer using pure domain models, completely decoupled from infrastructure SDKs or Microsoft Entra ID.

#### Scenario: Retrieving current user context

- GIVEN a valid authenticated request
- WHEN an Application layer service requests the current user via `ICurrentUserService`
- THEN it receives an `AuraUser` model containing only domain-relevant identity information

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
