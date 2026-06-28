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

The system MUST provide an `MsalTokenAcquisitionService` implementing `ITokenAcquisitionService`
that returns access tokens by reading the `access_token` stored in the HTTP session cookie by the
OIDC middleware (`SaveTokens=true`). `AcquireTokenInteractive` MUST NOT be called — it is a
desktop primitive incompatible with server-side Blazor. The `MeetingAlerts` scope MUST use
`api://{AzureAd:ClientId}/MeetingAlerts` with `ClientId` sourced from configuration. When OIDC
configuration (ClientId, TenantId) is absent, the system SHOULD fall back to mock JWT for local
development.

#### Scenario: Token read from session cookie

- GIVEN `SaveTokens=true` is set in OIDC middleware options
- AND the user is authenticated via the popup flow
- WHEN `MsalTokenAcquisitionService.AcquireTokenAsync` is called
- THEN the service reads `access_token` from `HttpContext` session
- AND returns it without calling `AcquireTokenInteractive`

#### Scenario: MeetingAlerts scope uses config-driven ClientId

- GIVEN `AzureAd:ClientId` is set in configuration
- WHEN the `MeetingAlerts` scope is constructed
- THEN it equals `api://{AzureAd:ClientId}/MeetingAlerts`
- AND does not contain any hardcoded GUID

#### Scenario: Fallback when OIDC config absent

- GIVEN OIDC configuration (ClientId, TenantId) is absent
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

### Requirement: OIDC Challenge Endpoint

The system MUST expose a `GET /login/challenge` minimal API endpoint that accepts anonymous
requests and triggers an OIDC challenge via `ChallengeAsync`. The OIDC middleware MUST own
state, nonce, and correlation cookie — no manual construction is permitted.

#### Scenario: GET /login/challenge redirects to Entra

- GIVEN the OIDC middleware is configured with `SaveTokens=true`
- WHEN an anonymous GET request is sent to `/login/challenge`
- THEN the response is a 302 redirect to the Entra ID authorization endpoint
- AND the response sets an `AspNetCore.Correlation.*` cookie (OIDC correlation)
- AND the redirect URL contains server-generated `state` and `nonce` parameters

### Requirement: Authentication Callback Page

The system MUST provide a Blazor page at `/authentication/callback` decorated with
`[AllowAnonymous]`. On page load, if `window.opener` exists, the page MUST post
`{ type: 'auth-success' }` to the opener and close itself. If no opener exists, the page
MUST redirect to `/`.

#### Scenario: Callback in popup context posts message and closes

- GIVEN the OIDC middleware has processed `/signin-oidc` and redirected to `/authentication/callback`
- WHEN `/authentication/callback` loads inside a popup window with `window.opener` set
- THEN the page calls `postMessage({ type: 'auth-success' }, opener origin)`
- AND calls `window.close()`

#### Scenario: Callback without opener redirects to root

- GIVEN `/authentication/callback` is navigated to directly (no popup)
- WHEN the page loads and `window.opener` is null
- THEN the page redirects to `/`
- AND does not attempt `postMessage`

### Requirement: CORS for Mock Login

The API server (port 5180) SHALL include a CORS policy allowing the UI origin (port 5190) for
the mock-login endpoint, including `Access-Control-Allow-Credentials: true`.

#### Scenario: Cross-origin mock-login request succeeds

- GIVEN the UI is on port 5190 and the API on port 5180
- WHEN the popup sends a mock-login request to the API
- THEN the API responds with `Access-Control-Allow-Origin: http://localhost:5190`
- AND `Access-Control-Allow-Credentials: true`

---

### Requirement: Auth Middleware Integration Tests

The system MUST include integration tests that validate the JWT Bearer authentication middleware enforces access control on protected endpoints. Tests MUST cover the 401 Unauthorized path for unauthenticated requests and the 200 OK path for authenticated requests. Tests SHOULD use the existing mock-login endpoint to obtain a valid token.

#### Scenario: Unauthenticated request returns 401

- GIVEN a protected API endpoint exists
- WHEN a GET request is sent without an Authorization header
- THEN the response status is 401 Unauthorized
- AND the response body does not contain protected resource data

#### Scenario: Authenticated request returns 200

- GIVEN a valid JWT token is obtained from the mock-login endpoint
- WHEN a GET request is sent to a protected endpoint with `Authorization: Bearer {token}`
- THEN the response status is 200 OK
- AND the response body contains the expected resource data

#### Scenario: Invalid token returns 401

- GIVEN a malformed or expired JWT token
- WHEN a GET request is sent to a protected endpoint with that token
- THEN the response status is 401 Unauthorized
