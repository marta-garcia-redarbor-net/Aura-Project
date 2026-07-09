# Demo Auth Specification

## Purpose

Minimal API endpoint that creates a fake authentication session for demo purposes. Allows visitors to explore the dashboard without Microsoft Entra ID login. The demo identity is established via an `aura_demo_mode` claim on the auth cookie.

## Requirements

### Requirement: Demo Login Endpoint

The system MUST expose a `GET /login/demo` minimal API endpoint decorated with `[AllowAnonymous]`. The endpoint MUST create an authentication cookie containing demo claims and redirect to `/dashboard`.

#### Scenario: Demo login creates auth cookie and redirects

- GIVEN the application is running
- WHEN an anonymous GET request is sent to `/login/demo`
- THEN the response sets an authentication cookie
- AND the cookie contains claims: name ("Demo User"), email ("demo@aura.local"), role ("Demo")
- AND the cookie contains an `aura_demo_mode` claim with value `"true"`
- AND the response is a 302 redirect to `/dashboard`

#### Scenario: Demo user accesses dashboard

- GIVEN the demo login cookie is set
- WHEN the user navigates to `/dashboard`
- THEN the dashboard renders without authentication challenge
- AND the user identity shows "Demo User"

### Requirement: Demo Claim Isolation

The `aura_demo_mode` claim MUST be present only on cookies issued by `/login/demo`. Real Entra ID authentication MUST NOT include this claim. The system MUST NOT allow the `aura_demo_mode` claim to grant access to production data or non-demo endpoints.

#### Scenario: Demo claim absent on real auth

- GIVEN a user authenticates via Microsoft Entra ID popup flow
- WHEN the authentication cookie is inspected
- THEN the `aura_demo_mode` claim is NOT present

#### Scenario: Demo claim not elevated

- GIVEN a user has the `aura_demo_mode` claim
- WHEN the user attempts to access a non-demo protected endpoint
- THEN the request is processed based on the "Demo" role
- AND no admin or production privileges are granted

### Requirement: Demo Session Independence

Demo authentication MUST be independent of `UseEntraId` configuration. The `/login/demo` endpoint MUST be available regardless of whether `UseEntraId` is `true` or `false`. Demo claims MUST persist for the session duration (cookie lifetime).

#### Scenario: Demo works with UseEntraId true

- GIVEN `UseEntraId=true` in configuration
- WHEN a user navigates to `/login/demo`
- THEN the demo cookie is set successfully
- AND the user is redirected to `/dashboard`

#### Scenario: Demo works with UseEntraId false

- GIVEN `UseEntraId=false` in configuration
- WHEN a user navigates to `/login/demo`
- THEN the demo cookie is set successfully
- AND the user is redirected to `/dashboard`
