# Proposal: Login Popup — OIDC Pipeline Redesign

## Intent

The Entra ID popup login is broken at the infrastructure level. Five independent root causes prevent authentication from completing: the HTTP pipeline is killed before Blazor starts, `AcquireTokenInteractive` opens a system browser window (desktop primitive on a server), manual URL construction bypasses OIDC middleware state/nonce, the callback never writes the auth cookie, and `[Authorize]` on `Index.razor` triggers an HTTP-level challenge before the Blazor circuit initialises.

The fix replaces manual OIDC URL construction with the correct challenge-endpoint pattern, delegates all state/nonce ownership to the OIDC middleware, and removes every layer that intercepts auth before Blazor loads. Visual behavior: the blurred dashboard shell renders **immediately** for unauthenticated users — the OIDC flow only starts when the user clicks the login button.

## Scope

### In Scope
1. Remove `[Authorize]` from `Index.razor` — let `AuthorizeRouteView` enforce auth in Blazor layer
2. Add `/login/challenge` minimal API endpoint — OIDC middleware owns state/nonce/correlation cookie
3. Simplify `RestrictedAccessView.razor.cs` — `BuildAuthUrl()` returns `/login/challenge` only
4. Simplify `AuthenticationCallback.razor.cs` — postMessage + window.close(), remove token exchange
5. Replace `MsalTokenAcquisitionService.AcquireTokenAsync` — read token from session (`SaveTokens=true`), not `AcquireTokenInteractive`
6. Remove `OnRedirectToIdentityProvider` suppression that returns 401 and kills the pipeline
7. Remove dead services: `OidcUrlBuilder`, `IPublicClientApplication` (Entra branch), `IConfidentialClientApplication`
8. Delete `RedirectToLogin.razor`
9. Fix `MeetingAlerts` scope using config-driven `ClientId` (not hardcoded)

### Out of Scope
- RestrictedAccessView visual appearance changes
- Dev mode (`UseEntraId=false`) path changes
- Graph API connector changes
- Mobile responsive layout

## Capabilities

### New Capabilities
None

### Modified Capabilities
- `api-authentication`: `MsalTokenAcquisitionService` requirement changes from `AcquireTokenInteractive` (desktop primitive) to reading tokens stored by cookie session (`SaveTokens=true`). Challenge endpoint pattern replaces manual URL construction. `AcquireTokenInteractive` and `IPublicClientApplication` (Entra branch) are removed.
- `restricted-access-view`: `BuildAuthUrl()` now returns `/login/challenge` (single value, no manual OIDC params). Popup flow completion uses postMessage only — no token exchange in the callback.

## Approach

**Challenge endpoint pattern** (all decisions resolved from exploration):

1. User hits `/` → Blazor loads anonymously (no HTTP-level `[Authorize]`)
2. `CascadingAuthenticationState` detects unauthenticated → `AuthorizeRouteView` renders `<NotAuthorized>` → `RestrictedAccessView` with blurred dashboard shell + login card
3. User clicks "Sign in with Microsoft" → `auth-popup.js` opens popup to `/login/challenge`
4. `/login/challenge` calls `ChallengeAsync` — OIDC middleware generates state/nonce/correlation cookie
5. Entra ID redirects popup to `/signin-oidc` → OIDC middleware processes, writes auth cookie, redirects to `/authentication/callback`
6. `AuthenticationCallback`: `postMessage({ type: 'auth-success' })` + `window.close()`
7. Opener: `Navigation.Refresh()` → Blazor re-evaluates auth state → dashboard renders

Cookie auth is the default scheme. `SaveTokens=true` stores the access token in the session cookie for downstream use.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.UI/Components/Pages/Index.razor` | Modified | Remove `[Authorize]` attribute |
| `src/Aura.Api/Program.cs` | Modified | Add `/login/challenge` minimal API endpoint; remove `OnRedirectToIdentityProvider` suppression |
| `src/Aura.UI/Components/Auth/RestrictedAccessView.razor.cs` | Modified | `BuildAuthUrl()` → `/login/challenge`; remove manual OIDC URL construction |
| `src/Aura.UI/Components/Auth/AuthenticationCallback.razor.cs` | Modified | Remove token exchange; postMessage + window.close() only |
| `src/Aura.Infrastructure/Auth/MsalTokenAcquisitionService.cs` | Modified | Replace `AcquireTokenInteractive` with session token read |
| `src/Aura.Infrastructure/Auth/OidcUrlBuilder.cs` | Removed | Dead — challenge endpoint replaces manual URL construction |
| `src/Aura.UI/Components/Auth/RedirectToLogin.razor` | Removed | Dead after `RestrictedAccessView` handles all unauthenticated cases |
| `src/Aura.Api/Program.cs` (DI) | Modified | Remove `IPublicClientApplication` (Entra branch), `IConfidentialClientApplication` |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Correlation cookie not set if `/login/challenge` misconfigures the scheme | Med | Integration test: hit `/login/challenge`, assert 302 to Entra, assert `Set-Cookie: .AspNetCore.Correlation.*` |
| `SaveTokens=true` stores tokens in cookie — size limit (4 KB) | Low | Tokens are short-lived; profile size in staging before production deploy |
| Popup blocked by browser if not opened from direct user gesture | Low | Button click is the direct gesture; do not auto-open on page load |

## Rollback Plan

Revert `Program.cs` (restore `OnRedirectToIdentityProvider`, remove `/login/challenge`), restore `[Authorize]` on `Index.razor`, restore `MsalTokenAcquisitionService` to prior implementation, restore `OidcUrlBuilder` from git. No database or migration changes involved.

## Dependencies

- `SaveTokens=true` must be set in OIDC middleware options (`Program.cs`) — prerequisite for session token read
- Entra ID App Registration redirect URI must include `/signin-oidc` (already registered)

## Success Criteria

- [ ] Unauthenticated user sees blurred dashboard shell immediately — no redirect, no blank page
- [ ] Clicking "Sign in with Microsoft" opens popup; popup closes after successful Entra ID auth
- [ ] Opener page refreshes and shows the authenticated dashboard (no manual reload needed)
- [ ] `dotnet test Aura.sln` passes — all existing auth integration tests green
- [ ] No 401 returned to browser before user clicks login button
- [ ] `OnRedirectToIdentityProvider` suppression removed — pipeline no longer killed at HTTP level
