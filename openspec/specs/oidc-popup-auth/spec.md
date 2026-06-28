# Delta for OIDC Popup Auth

## REMOVED Requirements

### Requirement: Popup Window Launch

(Reason: The popup now opens `/login/challenge` — a server-side challenge endpoint — not a
manually constructed Entra authorization URL. State, nonce, and correlation cookie are owned
by the OIDC middleware. Popup launch behavior is now specified in `restricted-access-view`
under "Popup Auth Flow".)
(Migration: Tests asserting the popup URL contains Entra ID domain or manual OIDC params must
be updated to assert the popup URL is `/login/challenge`.)

### Requirement: Correct OIDC Parameters

(Reason: Manual OIDC parameter construction (`client_id`, `redirect_uri`, `state`, `nonce`)
is fully delegated to the OIDC middleware via `ChallengeAsync`. The AADSTS90013 error this
requirement addressed is resolved by the challenge endpoint pattern — the server generates
correct state and nonce automatically.)
(Migration: Remove any test that validates OIDC URL parameter construction in client code.
Replace with integration test on `/login/challenge` asserting the 302 + correlation cookie.)

### Requirement: Popup-to-Main Communication

(Reason: Replaced and tightened. The popup sends exactly `{ type: 'auth-success' }` after
`/authentication/callback` loads. The failure-message scenario is removed — popup failure
causes the popup to close without a message; the main page detects this by popup-closed event.)
(Migration: Tests for failure postMessage path should be removed. Callback-page postMessage
tests move to `api-authentication` spec under "Authentication Callback Page".)

### Requirement: Auth State Update After Popup Login

(Reason: `Navigation.Refresh()` (force-load) replaces the no-reload pattern. The SignalR
circuit is intentionally terminated on refresh — this is correct behaviour for the cookie-based
auth model. The "preserve SignalR circuit" constraint is removed.)
(Migration: Tests asserting `Navigation.Refresh(forceLoad: false)` must be updated to assert
`Navigation.Refresh()` with force-load. Circuit-preservation assertions must be removed.)

### Requirement: SignalR Circuit Preservation

(Reason: The cookie-based OIDC flow requires a full page reload (`Navigation.Refresh()`) to
pick up the new auth cookie. Circuit preservation after login is not achievable with this
model and is no longer a requirement.)
(Migration: Remove all tests asserting circuit continuity across the login event.)
