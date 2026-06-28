# Delta for API Authentication

## ADDED Requirements

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

## MODIFIED Requirements

### Requirement: MSAL Token Acquisition

The system MUST provide an `MsalTokenAcquisitionService` implementing `ITokenAcquisitionService`
that returns access tokens by reading the `access_token` stored in the HTTP session cookie by the
OIDC middleware (`SaveTokens=true`). `AcquireTokenInteractive` MUST NOT be called — it is a
desktop primitive incompatible with server-side Blazor. The `MeetingAlerts` scope MUST use
`api://{AzureAd:ClientId}/MeetingAlerts` with `ClientId` sourced from configuration. When OIDC
configuration (ClientId, TenantId) is absent, the system SHOULD fall back to mock JWT for local
development.
(Previously: Required `AcquireTokenInteractive` with interactive browser flow; `MeetingAlerts` scope was hardcoded.)

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

## REMOVED Requirements

### Requirement: Mock Login Popup Compatibility

(Reason: Superseded by the OIDC challenge endpoint pattern. The popup now opens
`/login/challenge` — not the mock-login endpoint. Token forwarding via postMessage from
mock-login is no longer the flow.)
(Migration: Tests targeting mock-login popup token forwarding must be replaced with tests
for the `/login/challenge` → `/signin-oidc` → `/authentication/callback` pipeline.)
