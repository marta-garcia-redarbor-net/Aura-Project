# Delta for API Authentication

## MODIFIED Requirements

### Requirement: Authentication Callback Page

The system MUST provide a Blazor page at `/authentication/callback` decorated with `[AllowAnonymous]`. On page load, if `window.opener` exists, the page MUST post `{ type: 'auth-success' }` to the opener and close itself. If no opener exists, the page MUST redirect to `/dashboard`.
(Previously: Redirected to `/` when no opener existed.)

#### Scenario: Callback in popup context posts message and closes

- GIVEN the OIDC middleware has processed `/signin-oidc` and redirected to `/authentication/callback`
- WHEN `/authentication/callback` loads inside a popup window with `window.opener` set
- THEN the page calls `postMessage({ type: 'auth-success' }, opener origin)`
- AND calls `window.close()`

#### Scenario: Callback without opener redirects to dashboard

- GIVEN `/authentication/callback` is navigated to directly (no popup)
- WHEN the page loads and `window.opener` is null
- THEN the page redirects to `/dashboard`
- AND does not attempt `postMessage`
